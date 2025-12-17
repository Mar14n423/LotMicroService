using Lots.Domain.Entities; // Asegúrate de tener el namespace correcto
using Lots.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lots.Infrastructure.Messaging
{
    public class RabbitConsumer : BackgroundService
    {
        private readonly IConnection _conn;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider; // Para crear scopes
        private readonly ILogger<RabbitConsumer> _log;
        private readonly string _queueName = "lots.stock.queue"; // Cola exclusiva de Lots

        public RabbitConsumer(IServiceProvider serviceProvider, ILogger<RabbitConsumer> log)
        {
            _serviceProvider = serviceProvider;
            _log = log;

            var factory = new ConnectionFactory { HostName = "localhost" }; // Ajusta según tu config
            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();

            // Declarar el Exchange (mismo nombre que en Sales)
            _channel.ExchangeDeclare("saga.exchange", ExchangeType.Topic, durable: true);
            // Declarar la Cola
            _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
            // Bind: Escuchar cuando se crea una venta
            _channel.QueueBind(_queueName, "saga.exchange", "sale.header.created");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    await ProcessSaleCreated(message);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error procesando mensaje de venta");
                    // Si falla, podrías hacer Nack para reintentar o mandarlo a una cola de errores
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessSaleCreated(string jsonMessage)
        {
            using var scope = _serviceProvider.CreateScope();
            var lotRepo = scope.ServiceProvider.GetRequiredService<ILotRepository>();
            // Aquí necesitaríamos un Publisher para responder a Sales (stock.reserved o failed)
            // Por ahora, solo haremos la lógica de descuento.

            var data = JsonSerializer.Deserialize<JsonElement>(jsonMessage);
            var saleId = data.GetProperty("sale_id").GetString();
            var items = data.GetProperty("items").EnumerateArray();

            decimal totalCost = 0;
            bool success = true;
            string failReason = "";

            // Lógica FEFO (First Expired, First Out)
            foreach (var item in items)
            {
                int medId = int.Parse(item.GetProperty("MedId").GetString()!); // Asumiendo que MedId viene como string
                int qtyRequested = item.GetProperty("Quantity").GetInt32();

                // 1. Buscar lotes disponibles para esta medicina
                var lots = await lotRepo.GetByMedicineIdWithStockAsync(medId);

                int qtyRemaining = qtyRequested;

                foreach (var lot in lots)
                {
                    if (qtyRemaining <= 0) break;

                    int toTake = Math.Min(qtyRemaining, lot.quantity);

                    // Descontar
                    lot.quantity -= toTake;
                    qtyRemaining -= toTake;
                    totalCost += (lot.unit_cost * toTake); // Calculamos costo real (opcional)

                    // Actualizar en BD
                    await lotRepo.UpdateAsync(lot);
                }

                if (qtyRemaining > 0)
                {
                    success = false;
                    failReason = $"Stock insuficiente para medicina ID {medId}";
                    // OJO: Aquí deberíamos hacer Rollback de lo que descontamos antes si falla uno.
                    // Para hacerlo bien, deberías usar TransactionScope o UnitOfWork en Lots también.
                    break;
                }
            }

            if (success)
            {
                _log.LogInformation($"Stock reservado para Venta {saleId}. Costo total: {totalCost}");
                // TODO: Publicar evento "stock.reserved" con el total calculado
            }
            else
            {
                _log.LogWarning($"Fallo al reservar stock para Venta {saleId}: {failReason}");
                // TODO: Publicar evento "stock.reservation_failed"
                // TODO: Ejecutar compensación (devolver stock si se descontó parcialmente)
            }
        }
    }
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Lots.Domain.Entities;
using Lots.Domain.Ports; // Necesario para IUnitOfWork e IIdempotencyStore

namespace Lots.Infrastructure.Messaging
{
    public class RabbitConsumer : BackgroundService
    {
        private readonly IConnection _conn;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitConsumer> _log;
        private readonly string _queueName = "lots.stock.queue";

        public RabbitConsumer(IServiceProvider serviceProvider, ILogger<RabbitConsumer> log, Microsoft.Extensions.Configuration.IConfiguration cfg)
        {
            _serviceProvider = serviceProvider;
            _log = log;

            var factory = new ConnectionFactory
            {
                HostName = cfg["RabbitMQ:Host"] ?? "localhost",
                UserName = cfg["RabbitMQ:User"] ?? "guest",
                Password = cfg["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();

            // Configuración del Exchange y Cola
            _channel.ExchangeDeclare("saga.exchange", ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);

            // Escuchamos el evento de Venta Creada
            _channel.QueueBind(_queueName, "saga.exchange", "sale.header.created");
            // También escuchamos si la venta se cancela para devolver stock (Compensación)
            _channel.QueueBind(_queueName, "saga.exchange", "sale.cancelled");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnReceived;
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task OnReceived(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var idempotency = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

                // 1. Parsear Mensaje
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Obtener MessageId para idempotencia
                string messageId = root.TryGetProperty("MessageId", out var midProp) ? midProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                // 2. Verificar Idempotencia (¿Ya procesé esto antes?)
                if (await idempotency.HasProcessedAsync(messageId))
                {
                    _log.LogInformation($"Mensaje duplicado ignorado: {messageId}");
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                // 3. Procesar según el evento
                if (routingKey == "sale.header.created")
                {
                    await ProcessReservation(scope, root, messageId, routingKey);
                }
                else if (routingKey == "sale.cancelled")
                {
                    await ProcessCompensation(scope, root, messageId, routingKey);
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Error procesando mensaje {routingKey}");
                // Nack con requeue false para no bloquear la cola infinitamente (o true si quieres reintentar)
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }

        private async Task ProcessReservation(IServiceScope scope, JsonElement root, string msgId, string rk)
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var idempotency = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

            var saleId = root.GetProperty("sale_id").GetString();
            var items = root.GetProperty("items").EnumerateArray();

            decimal totalCalculated = 0;
            bool success = true;
            string failReason = "";

            await uow.BeginTransactionAsync(); // <--- INICIO TRANSACCIÓN GLOBAL

            try
            {
                foreach (var item in items)
                {
                    // Asumimos que Sale envía "MedId" (o medId) y "Quantity"
                    // Ajusta las mayúsculas según lo que envíe Sales.Api
                    var medIdProp = item.TryGetProperty("MedId", out var p1) ? p1 : item.GetProperty("medId");
                    var qtyProp = item.TryGetProperty("Quantity", out var p2) ? p2 : item.GetProperty("quantity");

                    int medId = int.Parse(medIdProp.ToString());
                    int qtyRequested = qtyProp.GetInt32();

                    // Lógica FEFO en Repo
                    var lots = await uow.LotRepository.GetByMedicineIdWithStockAsync(medId);
                    int qtyRemaining = qtyRequested;

                    foreach (var lot in lots)
                    {
                        if (qtyRemaining <= 0) break;
                        int toTake = Math.Min(qtyRemaining, lot.quantity);

                        lot.quantity -= toTake;
                        qtyRemaining -= toTake;
                        totalCalculated += (lot.unit_cost * toTake);

                        await uow.LotRepository.UpdateAsync(lot); // Update dentro de la transacción
                    }

                    if (qtyRemaining > 0)
                    {
                        success = false;
                        failReason = $"Stock insuficiente para Med ID {medId}";
                        break; // Romper el ciclo de items, la venta falla completa
                    }
                }

                // Preparar respuesta para Sales
                OutboxMessage responseMsg;
                if (success)
                {
                    var payload = new { sale_id = saleId, total_calculated = totalCalculated };
                    responseMsg = new OutboxMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        AggregateId = saleId,
                        RoutingKey = "stock.reserved", // <--- ESTO ES LO QUE ESPERA SALES
                        Payload = JsonSerializer.Serialize(payload),
                        Status = "PENDING"
                    };
                    _log.LogInformation($"Stock reservado para Venta {saleId}. Total: {totalCalculated}");
                }
                else
                {
                    // Si falló, hacemos Rollback manual de lo que hayamos descontado antes
                    // Pero como estamos en una transacción de BD, basta con NO hacer Commit
                    // Sin embargo, queremos guardar el mensaje de fallo en la Outbox.
                    // ESTRATEGIA: Hacemos Rollback de la transacción actual y abrimos una nueva SOLO para el mensaje de error.
                    await uow.RollbackAsync();

                    await uow.BeginTransactionAsync(); // Nueva transacción solo para registrar el fallo

                    var payload = new { sale_id = saleId, reason = failReason };
                    responseMsg = new OutboxMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        AggregateId = saleId,
                        RoutingKey = "stock.reservation_failed", // <--- ESTO ESPERA SALES
                        Payload = JsonSerializer.Serialize(payload),
                        Status = "PENDING"
                    };
                    _log.LogWarning($"Fallo reserva Venta {saleId}: {failReason}");
                }

                await uow.OutboxRepository.AddAsync(responseMsg);
                await idempotency.MarkProcessedAsync(msgId, rk); // Guardar historial en la misma TX

                await uow.CommitAsync(); // <--- COMMIT FINAL (Stock + Mensaje + Historial)
            }
            catch
            {
                await uow.RollbackAsync();
                throw;
            }
        }

        private async Task ProcessCompensation(IServiceScope scope, JsonElement root, string msgId, string rk)
        {
            // Aquí iría la lógica para DEVOLVER el stock si la venta se cancela después de reservar.
            // Por simplicidad, solo marcamos como procesado por ahora.
            var idempotency = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
            await idempotency.MarkProcessedAsync(msgId, rk);
        }
    }
}
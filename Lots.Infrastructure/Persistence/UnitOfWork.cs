using Lots.Domain.Interfaces;
using Lots.Domain.Ports;
using Lots.Infrastructure.Data;
using Lots.Infrastructure.Data; 
using Lots.Infrastructure.Repository;
using MySql.Data.MySqlClient;
using Lots.Infraestructure.Persistence;
using System.Threading.Tasks;

namespace Lots.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MySqlConnection _connection;
        private MySqlTransaction? _transaction;

        private ILotRepository? _lotRepository;
        private IOutboxRepository? _outboxRepository;

        public UnitOfWork()
        {
            // Usamos tu singleton existente
            _connection = DatabaseConnection.Instance.GetConnection();
        }

        public ILotRepository LotRepository
        {
            get
            {
                // Aquí está la magia: Pasamos la transacción al repositorio
                return _lotRepository ??= new LotRepository(_connection, _transaction);
            }
        }

        public IOutboxRepository OutboxRepository
        {
            get
            {
                // Pasamos la transacción al repositorio de Outbox también
                return _outboxRepository ??= new OutboxRepository(_connection, _transaction);
            }
        }

        public async Task BeginTransactionAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();

            _transaction = await _connection.BeginTransactionAsync();

            // Limpiamos las instancias para que se recreen con la nueva transacción
            _lotRepository = null;
            _outboxRepository = null;
        }

        public async Task CommitAsync()
        {
            try
            {
                await _transaction!.CommitAsync();
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
            finally
            {
                await DisposeTransaction();
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();

            await DisposeTransaction();
        }

        public async Task EnsureConnectionOpenAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
        }

        private async Task DisposeTransaction()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }
}
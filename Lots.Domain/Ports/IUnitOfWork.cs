using System;
using System.Threading.Tasks;
using Lots.Domain.Interfaces; 

namespace Lots.Domain.Ports
{
    public interface IUnitOfWork : IDisposable
    {
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task EnsureConnectionOpenAsync();
        ILotRepository LotRepository { get; }

        IOutboxRepository OutboxRepository { get; }
    }
}
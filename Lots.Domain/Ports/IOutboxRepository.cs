using System.Collections.Generic;
using System.Threading.Tasks;
using Lots.Domain.Entities; // Asegúrate de haber copiado la entidad OutboxMessage también

namespace Lots.Domain.Ports
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage message);
        Task<IEnumerable<OutboxMessage>> GetPendingAsync(int limit = 100);
        Task MarkSentAsync(string id);
        Task IncrementAttemptAsync(string id);
    }
}
using System.Threading.Tasks;

namespace Lots.Domain.Ports
{
    public interface IEventPublisher
    {
        Task PublishAsync(string routingKey, object @event);
    }
}

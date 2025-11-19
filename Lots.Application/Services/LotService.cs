using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using Lots.Domain.Services;

namespace Lots.Application.Services
{
    public class LotService : ILotService
    {
        private readonly ILotRepository _repository;

        public LotService(ILotRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Lot>> GetLotsAsync()
            => _repository.GetAllAsync();

        public Task<Lot?> GetLotAsync(int id)
            => _repository.GetByIdAsync(id);

        public Task<int> CreateLotAsync(Lot lot)
        {
            lot.created_at = DateTime.Now;
            return _repository.CreateAsync(lot);
        }

        public Task UpdateLotAsync(Lot lot)
        {
            lot.updated_at = DateTime.Now;
            return _repository.UpdateAsync(lot);
        }

        public Task DeleteLotAsync(int id)
            => _repository.DeleteAsync(id);
    }
}

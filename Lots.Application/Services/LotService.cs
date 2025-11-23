using Lots.Domain.Entities;
using Lots.Domain.Exceptions;
using Lots.Domain.Interfaces;
using Lots.Domain.Services;
using Lots.Domain.Validators;

namespace Lots.Application.Services
{
    public class LotService : ILotService
    {
        private readonly ILotRepository _repository;
        private readonly IValidator<Lot> _validator;

        public LotService(ILotRepository repository, IValidator<Lot> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public Task<List<Lot>> GetLotsAsync()
            => _repository.GetAllAsync();

        public Task<Lot?> GetLotAsync(int id)
            => _repository.GetByIdAsync(id);

        public async Task<int> CreateLotAsync(Lot lot)
        {
            lot.created_at = DateTime.Now;
            lot.is_deleted = false;

            var result = _validator.Validate(lot);
            if (!result.IsSuccess)
                throw new ValidationException(result.Errors.ToDictionary());

            return await _repository.CreateAsync(lot);
        }

        public async Task UpdateLotAsync(Lot lot)
        {
            lot.updated_at = DateTime.Now;

            var result = _validator.Validate(lot);
            if (!result.IsSuccess)
                throw new ValidationException(result.Errors.ToDictionary());

            await _repository.UpdateAsync(lot);
        }

        public Task DeleteLotAsync(int id)
            => _repository.DeleteAsync(id);
    }
}

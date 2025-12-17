using Lots.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Lots.Domain.Interfaces
{
    public interface ILotRepository
    {
        Task<List<Lot>> GetAllAsync();
        Task<Lot?> GetByIdAsync(int id);

        Task<List<Lot>> GetByIdsAsync(List<int> ids);

        Task<int> CreateAsync(Lot lot);
        Task UpdateAsync(Lot lot);
        Task DeleteAsync(int id);

        Task<List<Lot>> GetByMedicineIdWithStockAsync(int medicineId);

    }
}

using Lots.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lots.Domain.Services
{
    public interface ILotService
    {
        Task<List<Lot>> GetLotsAsync();
        Task<Lot?> GetLotAsync(int id);
        Task<int> CreateLotAsync(Lot lot);
        Task UpdateLotAsync(Lot lot);
        Task DeleteLotAsync(int id);
        Task<List<Lot>> GetLotsByIdsAsync(List<int> ids);
    }
}

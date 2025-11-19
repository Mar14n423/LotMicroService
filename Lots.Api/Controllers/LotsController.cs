using Lots.Domain.Entities;
using Lots.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lots.Api.Controllers
{
    [ApiController]
    [Route("api/lots")]
    public class LotsController : ControllerBase
    {
        private readonly ILotService _service;

        public LotsController(ILotService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetLotsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var lot = await _service.GetLotAsync(id);
            return lot is null ? NotFound() : Ok(lot);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Lot lot)
        {
            var id = await _service.CreateLotAsync(lot);
            return CreatedAtAction(nameof(Get), new { id }, lot);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Lot lot)
        {
            lot.id = id;
            await _service.UpdateLotAsync(lot);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteLotAsync(id);
            return NoContent();
        }
    }
}

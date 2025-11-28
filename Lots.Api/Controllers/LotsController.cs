using Lots.Domain.Entities;
using Lots.Domain.Exceptions;
using Lots.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lots.Api.Controllers
{
    [ApiController]
    [Route("api/lots")]
    [Authorize]
    public class LotsController : ControllerBase
    {
        private readonly ILotService _service;

        public LotsController(ILotService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var lots = await _service.GetLotsAsync();
                return Ok(lots);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var lot = await _service.GetLotAsync(id);
                return lot is null ? NotFound() : Ok(lot);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Lot lot)
        {
            try
            {
                var id = await _service.CreateLotAsync(lot);
                return CreatedAtAction(nameof(Get), new { id }, lot);
            }
            catch (ValidationException ve)
            {
                return BadRequest(new { message = ve.Message, errors = ve.Errors });
            }
            catch (DomainException de)
            {
                return BadRequest(new { message = de.Message });
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Lot lot)
        {
            lot.id = id;

            try
            {
                await _service.UpdateLotAsync(lot);
                return NoContent();
            }
            catch (ValidationException ve)
            {
                return BadRequest(new { message = ve.Message, errors = ve.Errors });
            }
            catch (DomainException de)
            {
                return BadRequest(new { message = de.Message });
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteLotAsync(id);
                return NoContent();
            }
            catch (DomainException de)
            {
                return BadRequest(new { message = de.Message });
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }
    }
}

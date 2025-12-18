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
            [HttpPost("by-ids")] // Ruta: api/lots/by-ids
            public async Task<IActionResult> GetByIds([FromBody] List<int> ids)
            {
                try
                {
                    // Asumo que agregaste el método al servicio también
                    var lots = await _service.GetLotsByIdsAsync(ids);
                    return Ok(lots);
                }
                catch (Exception)
                {
                    return StatusCode(500);
                }
            }

            // GET api/lots/medicine/{medId}
            [HttpGet("medicine/{medId:int}")]
            public async Task<IActionResult> GetByMedicine(int medId)
            {
                try
                {
                    // OPTION A: If your service has a specific method (Recommended)
                    // var lots = await _service.GetLotsByMedicineIdAsync(medId);

                    // OPTION B: Temporary workaround if you haven't created that method yet
                    // (Fetches all and filters - active only)
                    var allLots = await _service.GetLotsAsync();
                    var lots = allLots.Where(l => l.medicine_id == medId && !l.is_deleted && l.quantity > 0)
                                      .OrderBy(l => l.expiration_date) // FEFO (First Expired First Out) logic usually
                                      .ToList();

                    return Ok(lots);
                }
                catch (Exception)
                {
                    return StatusCode(500);
                }
            }


        }
    }

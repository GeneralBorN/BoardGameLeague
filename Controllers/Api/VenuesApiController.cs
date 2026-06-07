using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers.Api
{
    [ApiController]
    [Authorize]
    [Route("api/venues")]
    public class VenuesApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public VenuesApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VenueDto>>> Get([FromQuery] string? q)
        {
            var query = _context.Venues.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(v => v.Name.Contains(q) || v.City.Contains(q) || v.Country.Contains(q));
            }

            var venues = await query
                .OrderBy(v => v.Name)
                .Select(v => new VenueDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    City = v.City,
                    Country = v.Country,
                    Capacity = v.Capacity,
                    Indoor = v.Indoor
                })
                .ToListAsync();

            return Ok(venues);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<VenueDto>> Get(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return Ok(new VenueDto
            {
                Id = venue.Id,
                Name = venue.Name,
                City = venue.City,
                Country = venue.Country,
                Capacity = venue.Capacity,
                Indoor = venue.Indoor
            });
        }

        [HttpPost]
        public async Task<ActionResult<VenueDto>> Post([FromBody] VenueCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var venue = new Venue
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                City = model.City,
                Country = model.Country,
                Capacity = model.Capacity,
                Indoor = model.Indoor
            };

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = venue.Id }, new VenueDto
            {
                Id = venue.Id,
                Name = venue.Name,
                City = venue.City,
                Country = venue.Country,
                Capacity = venue.Capacity,
                Indoor = venue.Indoor
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<VenueDto>> Put(Guid id, [FromBody] VenueUpdateDto model)
        {
            if (!ModelState.IsValid || id != model.Id)
            {
                return BadRequest(ModelState);
            }

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            venue.Name = model.Name;
            venue.City = model.City;
            venue.Country = model.Country;
            venue.Capacity = model.Capacity;
            venue.Indoor = model.Indoor;

            await _context.SaveChangesAsync();
            return Ok(new VenueDto
            {
                Id = venue.Id,
                Name = venue.Name,
                City = venue.City,
                Country = venue.Country,
                Capacity = venue.Capacity,
                Indoor = venue.Indoor
            });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

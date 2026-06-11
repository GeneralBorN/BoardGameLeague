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
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/venues")]
    public class VenuesApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public VenuesApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VenueDto>>> Get([FromQuery] VenueQueryParameters queryParameters)
        {
            var query = _context.Venues.AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(queryParameters.Name))
            {
                query = query.Where(v => v.Name.Contains(queryParameters.Name));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.City))
            {
                query = query.Where(v => v.City.Contains(queryParameters.City));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.Country))
            {
                query = query.Where(v => v.Country.Contains(queryParameters.Country));
            }

            if (queryParameters.MinCapacity.HasValue)
            {
                query = query.Where(v => v.Capacity >= queryParameters.MinCapacity.Value);
            }

            if (queryParameters.MaxCapacity.HasValue)
            {
                query = query.Where(v => v.Capacity <= queryParameters.MaxCapacity.Value);
            }

            if (queryParameters.Indoor.HasValue)
            {
                query = query.Where(v => v.Indoor == queryParameters.Indoor.Value);
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            {
                query = queryParameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(v => EF.Property<object>(v, queryParameters.SortBy))
                    : query.OrderBy(v => EF.Property<object>(v, queryParameters.SortBy));
            }
            else
            {
                query = query.OrderBy(v => v.Name);
            }

            // Pagination
            var totalCount = await query.CountAsync();
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Size", queryParameters.PageSize.ToString());
            Response.Headers.Append("X-Page-Number", queryParameters.PageNumber.ToString());
            Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling((double)totalCount / queryParameters.PageSize)).ToString());

            var venues = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(v => ToDto(v))
                .ToListAsync();

            return Ok(venues);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<VenueDto>> Get(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return Ok(ToDto(venue));
        }

        [Authorize(Roles = "Admin,Manager")]
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

            return CreatedAtAction(nameof(Get), new { id = venue.Id }, ToDto(venue));
        }

        [Authorize(Roles = "Admin,Manager")]
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
            return Ok(ToDto(venue));
        }

        [Authorize(Roles = "Admin")]
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

        private static VenueDto ToDto(Venue venue)
        {
            return new VenueDto
            {
                Id = venue.Id,
                Name = venue.Name,
                City = venue.City,
                Country = venue.Country,
                Capacity = venue.Capacity,
                Indoor = venue.Indoor
            };
        }
    }
}

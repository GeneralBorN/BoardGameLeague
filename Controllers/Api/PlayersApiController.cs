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
    [Route("api/v{version:apiVersion}/players")]
    public class PlayersApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public PlayersApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerDto>>> Get([FromQuery] PlayerQueryParameters queryParameters)
        {
            var query = _context.Players.AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(queryParameters.Name))
            {
                query = query.Where(p => p.Name.Contains(queryParameters.Name));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.Country))
            {
                query = query.Where(p => p.Country.Contains(queryParameters.Country));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.Role))
            {
                query = query.Where(p => p.Role.Contains(queryParameters.Role));
            }

            if (queryParameters.MinRating.HasValue)
            {
                query = query.Where(p => p.Rating >= queryParameters.MinRating.Value);
            }

            if (queryParameters.MaxRating.HasValue)
            {
                query = query.Where(p => p.Rating <= queryParameters.MaxRating.Value);
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            {
                query = queryParameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => EF.Property<object>(p, queryParameters.SortBy))
                    : query.OrderBy(p => EF.Property<object>(p, queryParameters.SortBy));
            }
            else
            {
                query = query.OrderBy(p => p.Name);
            }

            // Pagination
            var totalCount = await query.CountAsync();
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Size", queryParameters.PageSize.ToString());
            Response.Headers.Append("X-Page-Number", queryParameters.PageNumber.ToString());
            Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling((double)totalCount / queryParameters.PageSize)).ToString());

            var players = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(p => ToDto(p))
                .ToListAsync();

            return Ok(players);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PlayerDto>> Get(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return Ok(ToDto(player));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<ActionResult<PlayerDto>> Post([FromBody] PlayerCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var player = new Player
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Rating = model.Rating,
                JoinedDate = model.JoinedDate,
                Country = model.Country,
                Role = model.Role
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = player.Id }, ToDto(player));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<PlayerDto>> Put(Guid id, [FromBody] PlayerUpdateDto model)
        {
            if (!ModelState.IsValid || id != model.Id)
            {
                return BadRequest(ModelState);
            }

            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            player.Name = model.Name;
            player.Rating = model.Rating;
            player.JoinedDate = model.JoinedDate;
            player.Country = model.Country;
            player.Role = model.Role;

            await _context.SaveChangesAsync();

            return Ok(ToDto(player));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static PlayerDto ToDto(Player player)
        {
            return new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Rating = player.Rating,
                JoinedDate = player.JoinedDate,
                Country = player.Country,
                Role = player.Role
            };
        }
    }
}

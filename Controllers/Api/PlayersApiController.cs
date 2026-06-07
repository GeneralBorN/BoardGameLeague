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
    [Route("api/players")]
    public class PlayersApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public PlayersApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerDto>>> Get([FromQuery] string? q)
        {
            var query = _context.Players.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Name.Contains(q) || p.Country.Contains(q) || p.Role.Contains(q));
            }

            var players = await query
                .OrderBy(p => p.Name)
                .Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Rating = p.Rating,
                    JoinedDate = p.JoinedDate,
                    Country = p.Country,
                    Role = p.Role
                })
                .ToListAsync();

            return Ok(players);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PlayerDto>> Get(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return Ok(new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Rating = player.Rating,
                JoinedDate = player.JoinedDate,
                Country = player.Country,
                Role = player.Role
            });
        }

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

            return CreatedAtAction(nameof(Get), new { id = player.Id }, new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Rating = player.Rating,
                JoinedDate = player.JoinedDate,
                Country = player.Country,
                Role = player.Role
            });
        }

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

            return Ok(new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Rating = player.Rating,
                JoinedDate = player.JoinedDate,
                Country = player.Country,
                Role = player.Role
            });
        }

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
    }
}

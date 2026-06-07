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
    [Route("api/teams")]
    public class TeamsApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public TeamsApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamDto>>> Get([FromQuery] string? q)
        {
            var query = _context.Teams.Include(t => t.Players).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(t => t.Name.Contains(q) || t.Region.Contains(q));
            }

            var teams = await query
                .OrderBy(t => t.Name)
                .Select(t => ToDto(t))
                .ToListAsync();

            return Ok(teams);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TeamDto>> Get(Guid id)
        {
            var team = await _context.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == id);
            if (team == null)
            {
                return NotFound();
            }

            return Ok(ToDto(team));
        }

        [HttpPost]
        public async Task<ActionResult<TeamDto>> Post([FromBody] TeamCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Region = model.Region,
                FoundedDate = model.FoundedDate,
                IsActive = model.IsActive,
                TotalWins = model.TotalWins,
                TotalLosses = model.TotalLosses
            };

            if (model.PlayerIds.Any())
            {
                var players = await _context.Players.Where(p => model.PlayerIds.Contains(p.Id)).ToListAsync();
                foreach (var player in players)
                {
                    team.Players.Add(player);
                }
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = team.Id }, ToDto(team));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TeamDto>> Put(Guid id, [FromBody] TeamUpdateDto model)
        {
            if (!ModelState.IsValid || id != model.Id)
            {
                return BadRequest(ModelState);
            }

            var team = await _context.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == id);
            if (team == null)
            {
                return NotFound();
            }

            team.Name = model.Name;
            team.Region = model.Region;
            team.FoundedDate = model.FoundedDate;
            team.IsActive = model.IsActive;
            team.TotalWins = model.TotalWins;
            team.TotalLosses = model.TotalLosses;

            team.Players.Clear();
            if (model.PlayerIds.Any())
            {
                var players = await _context.Players.Where(p => model.PlayerIds.Contains(p.Id)).ToListAsync();
                foreach (var player in players)
                {
                    team.Players.Add(player);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(ToDto(team));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static TeamDto ToDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Region = team.Region,
                FoundedDate = team.FoundedDate,
                IsActive = team.IsActive,
                TotalWins = team.TotalWins,
                TotalLosses = team.TotalLosses,
                Players = team.Players.Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Rating = p.Rating,
                    JoinedDate = p.JoinedDate,
                    Country = p.Country,
                    Role = p.Role
                }).ToList()
            };
        }
    }
}

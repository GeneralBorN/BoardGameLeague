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
    [Route("api/v{version:apiVersion}/teams")]
    public class TeamsApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public TeamsApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamDto>>> Get([FromQuery] TeamQueryParameters queryParameters)
        {
            var query = _context.Teams.Include(t => t.Players).AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(queryParameters.Name))
            {
                query = query.Where(t => t.Name.Contains(queryParameters.Name));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.Region))
            {
                query = query.Where(t => t.Region.Contains(queryParameters.Region));
            }

            if (queryParameters.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == queryParameters.IsActive.Value);
            }

            if (queryParameters.MinWins.HasValue)
            {
                query = query.Where(t => t.TotalWins >= queryParameters.MinWins.Value);
            }

            if (queryParameters.MaxWins.HasValue)
            {
                query = query.Where(t => t.TotalWins <= queryParameters.MaxWins.Value);
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            {
                query = queryParameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => EF.Property<object>(t, queryParameters.SortBy))
                    : query.OrderBy(t => EF.Property<object>(t, queryParameters.SortBy));
            }
            else
            {
                query = query.OrderBy(t => t.Name);
            }

            // Pagination
            var totalCount = await query.CountAsync();
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Size", queryParameters.PageSize.ToString());
            Response.Headers.Append("X-Page-Number", queryParameters.PageNumber.ToString());
            Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling((double)totalCount / queryParameters.PageSize)).ToString());

            var teams = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(t => ToDto(t))
                .ToListAsync();

            return Ok(teams);
        }

        [AllowAnonymous]
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

        [Authorize(Roles = "Admin,Manager")]
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

        [Authorize(Roles = "Admin,Manager")]
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

        [Authorize(Roles = "Admin")]
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

        private static PlayerDto ToPlayerDto(Player player)
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

        private TeamDto ToDto(Team team)
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
                Players = team.Players?.Select(p => ToPlayerDto(p)).ToList() ?? new List<PlayerDto>()
            };
        }
    }
}

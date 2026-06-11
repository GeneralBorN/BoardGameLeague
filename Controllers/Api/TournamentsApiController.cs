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
    [Route("api/v{version:apiVersion}/tournaments")]
    public class TournamentsApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public TournamentsApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TournamentDto>>> Get([FromQuery] TournamentQueryParameters queryParameters)
        {
            var query = _context.Tournaments
                .Include(t => t.Venue)
                .Include(t => t.Teams)
                    .ThenInclude(team => team.Players)
                .AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(queryParameters.Name))
            {
                query = query.Where(t => t.Name.Contains(queryParameters.Name));
            }

            if (queryParameters.StartDate.HasValue)
            {
                query = query.Where(t => t.StartDate >= queryParameters.StartDate.Value);
            }

            if (queryParameters.EndDate.HasValue)
            {
                query = query.Where(t => t.EndDate <= queryParameters.EndDate.Value);
            }

            if (queryParameters.IsOpen.HasValue)
            {
                query = query.Where(t => t.IsOpen == queryParameters.IsOpen.Value);
            }

            if (queryParameters.VenueId.HasValue)
            {
                query = query.Where(t => t.VenueId == queryParameters.VenueId.Value);
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

            var tournaments = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(t => ToDto(t))
                .ToListAsync();

            return Ok(tournaments);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TournamentDto>> Get(Guid id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Venue)
                .Include(t => t.Teams)
                    .ThenInclude(team => team.Players)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            return Ok(ToDto(tournament));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<ActionResult<TournamentDto>> Post([FromBody] TournamentCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var venue = await _context.Venues.FindAsync(model.VenueId);
            if (venue == null)
            {
                return BadRequest("Venue not found.");
            }

            var tournament = new Tournament
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                VenueId = model.VenueId,
                IsOpen = model.IsOpen
            };

            if (model.TeamIds.Any())
            {
                var teams = await _context.Teams.Where(t => model.TeamIds.Contains(t.Id)).ToListAsync();
                foreach (var team in teams)
                {
                    tournament.Teams.Add(team);
                }
            }

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = tournament.Id }, ToDto(tournament));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TournamentDto>> Put(Guid id, [FromBody] TournamentUpdateDto model)
        {
            if (!ModelState.IsValid || id != model.Id)
            {
                return BadRequest(ModelState);
            }

            var tournament = await _context.Tournaments
                .Include(t => t.Teams)
                .Include(t => t.Venue)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            var venue = await _context.Venues.FindAsync(model.VenueId);
            if (venue == null)
            {
                return BadRequest("Venue not found.");
            }

            tournament.Name = model.Name;
            tournament.Description = model.Description;
            tournament.StartDate = model.StartDate;
            tournament.EndDate = model.EndDate;
            tournament.VenueId = model.VenueId;
            tournament.IsOpen = model.IsOpen;

            tournament.Teams.Clear();
            if (model.TeamIds.Any())
            {
                var teams = await _context.Teams.Where(t => model.TeamIds.Contains(t.Id)).ToListAsync();
                foreach (var team in teams)
                {
                    tournament.Teams.Add(team);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(ToDto(tournament));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static TournamentDto ToDto(Tournament tournament)
        {
            return new TournamentDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                Description = tournament.Description,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                IsOpen = tournament.IsOpen,
                Venue = tournament.Venue == null ? null : new VenueDto
                {
                    Id = tournament.Venue.Id,
                    Name = tournament.Venue.Name,
                    City = tournament.Venue.City,
                    Country = tournament.Venue.Country,
                    Capacity = tournament.Venue.Capacity,
                    Indoor = tournament.Venue.Indoor
                },
                Teams = tournament.Teams.Select(t => new TeamDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Region = t.Region,
                    FoundedDate = t.FoundedDate,
                    IsActive = t.IsActive,
                    TotalWins = t.TotalWins,
                    TotalLosses = t.TotalLosses,
                    Players = t.Players.Select(p => new PlayerDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Rating = p.Rating,
                        JoinedDate = p.JoinedDate,
                        Country = p.Country,
                        Role = p.Role
                    }).ToList()
                }).ToList()
            };
        }
    }
}

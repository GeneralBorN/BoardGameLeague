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
    [Route("api/matches")]
    public class MatchesApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public MatchesApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MatchDto>>> Get([FromQuery] string? q)
        {
            var query = _context.Matches
                .Include(m => m.Tournament).ThenInclude(t => t.Venue)
                .Include(m => m.TeamA).ThenInclude(t => t.Players)
                .Include(m => m.TeamB).ThenInclude(t => t.Players)
                .Include(m => m.Game)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(m => m.Tournament.Name.Contains(q) || m.TeamA.Name.Contains(q) || m.TeamB.Name.Contains(q) || m.Game.Name.Contains(q));
            }

            var matches = await query
                .OrderBy(m => m.StartTime)
                .Select(m => ToDto(m))
                .ToListAsync();

            return Ok(matches);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MatchDto>> Get(Guid id)
        {
            var match = await _context.Matches
                .Include(m => m.Tournament).ThenInclude(t => t.Venue)
                .Include(m => m.TeamA).ThenInclude(t => t.Players)
                .Include(m => m.TeamB).ThenInclude(t => t.Players)
                .Include(m => m.Game)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound();
            }

            return Ok(ToDto(match));
        }

        [HttpPost]
        public async Task<ActionResult<MatchDto>> Post([FromBody] MatchCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model.TeamAId == model.TeamBId)
            {
                return BadRequest("Team A and Team B must be different.");
            }

            var tournament = await _context.Tournaments.FindAsync(model.TournamentId);
            var teamA = await _context.Teams.FindAsync(model.TeamAId);
            var teamB = await _context.Teams.FindAsync(model.TeamBId);
            var game = await _context.BoardGames.FindAsync(model.GameId);

            if (tournament == null || teamA == null || teamB == null || game == null)
            {
                return BadRequest("Tournament, teams, or game reference is invalid.");
            }

            var match = new Match
            {
                Id = Guid.NewGuid(),
                TournamentId = model.TournamentId,
                TeamAId = model.TeamAId,
                TeamBId = model.TeamBId,
                GameId = model.GameId,
                StartTime = model.StartTime,
                ScoreA = model.ScoreA,
                ScoreB = model.ScoreB,
                IsCompleted = model.IsCompleted
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = match.Id }, ToDto(match));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<MatchDto>> Put(Guid id, [FromBody] MatchUpdateDto model)
        {
            if (!ModelState.IsValid || id != model.Id)
            {
                return BadRequest(ModelState);
            }

            if (model.TeamAId == model.TeamBId)
            {
                return BadRequest("Team A and Team B must be different.");
            }

            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournaments.FindAsync(model.TournamentId);
            var teamA = await _context.Teams.FindAsync(model.TeamAId);
            var teamB = await _context.Teams.FindAsync(model.TeamBId);
            var game = await _context.BoardGames.FindAsync(model.GameId);

            if (tournament == null || teamA == null || teamB == null || game == null)
            {
                return BadRequest("Tournament, teams, or game reference is invalid.");
            }

            match.TournamentId = model.TournamentId;
            match.TeamAId = model.TeamAId;
            match.TeamBId = model.TeamBId;
            match.GameId = model.GameId;
            match.StartTime = model.StartTime;
            match.ScoreA = model.ScoreA;
            match.ScoreB = model.ScoreB;
            match.IsCompleted = model.IsCompleted;

            await _context.SaveChangesAsync();
            return Ok(ToDto(match));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }

            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static MatchDto ToDto(Match match)
        {
            return new MatchDto
            {
                Id = match.Id,
                StartTime = match.StartTime,
                ScoreA = match.ScoreA,
                ScoreB = match.ScoreB,
                IsCompleted = match.IsCompleted,
                Tournament = match.Tournament == null ? null : new TournamentDto
                {
                    Id = match.Tournament.Id,
                    Name = match.Tournament.Name,
                    Description = match.Tournament.Description,
                    StartDate = match.Tournament.StartDate,
                    EndDate = match.Tournament.EndDate,
                    IsOpen = match.Tournament.IsOpen,
                    Venue = match.Tournament.Venue == null ? null : new VenueDto
                    {
                        Id = match.Tournament.Venue.Id,
                        Name = match.Tournament.Venue.Name,
                        City = match.Tournament.Venue.City,
                        Country = match.Tournament.Venue.Country,
                        Capacity = match.Tournament.Venue.Capacity,
                        Indoor = match.Tournament.Venue.Indoor
                    }
                },
                TeamA = match.TeamA == null ? null : new TeamDto
                {
                    Id = match.TeamA.Id,
                    Name = match.TeamA.Name,
                    Region = match.TeamA.Region,
                    FoundedDate = match.TeamA.FoundedDate,
                    IsActive = match.TeamA.IsActive,
                    TotalWins = match.TeamA.TotalWins,
                    TotalLosses = match.TeamA.TotalLosses,
                    Players = match.TeamA.Players.Select(p => new PlayerDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Rating = p.Rating,
                        JoinedDate = p.JoinedDate,
                        Country = p.Country,
                        Role = p.Role
                    }).ToList()
                },
                TeamB = match.TeamB == null ? null : new TeamDto
                {
                    Id = match.TeamB.Id,
                    Name = match.TeamB.Name,
                    Region = match.TeamB.Region,
                    FoundedDate = match.TeamB.FoundedDate,
                    IsActive = match.TeamB.IsActive,
                    TotalWins = match.TeamB.TotalWins,
                    TotalLosses = match.TeamB.TotalLosses,
                    Players = match.TeamB.Players.Select(p => new PlayerDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Rating = p.Rating,
                        JoinedDate = p.JoinedDate,
                        Country = p.Country,
                        Role = p.Role
                    }).ToList()
                },
                Game = match.Game == null ? null : new BoardGameDto
                {
                    Id = match.Game.Id,
                    Name = match.Game.Name,
                    Category = match.Game.Category,
                    MinPlayers = match.Game.MinPlayers,
                    MaxPlayers = match.Game.MaxPlayers,
                    AveragePlayTimeMinutes = (int)match.Game.AveragePlayTime.TotalMinutes,
                    Complexity = match.Game.Complexity
                }
            };
        }
    }
}

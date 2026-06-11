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
    [Route("api/v{version:apiVersion}/boardgames")]
    public class BoardGamesApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public BoardGamesApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoardGameDto>>> Get([FromQuery] BoardGameQueryParameters queryParameters)
        {
            var query = _context.BoardGames.AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(queryParameters.Name))
            {
                query = query.Where(g => g.Name.Contains(queryParameters.Name));
            }

            if (queryParameters.Category.HasValue)
            {
                query = query.Where(g => g.Category == queryParameters.Category.Value);
            }

            if (queryParameters.MinComplexity.HasValue)
            {
                query = query.Where(g => g.Complexity >= queryParameters.MinComplexity.Value);
            }

            if (queryParameters.MaxComplexity.HasValue)
            {
                query = query.Where(g => g.Complexity <= queryParameters.MaxComplexity.Value);
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            {
                query = queryParameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(g => EF.Property<object>(g, queryParameters.SortBy))
                    : query.OrderBy(g => EF.Property<object>(g, queryParameters.SortBy));
            }
            else
            {
                query = query.OrderBy(g => g.Name);
            }

            // Pagination
            var totalCount = await query.CountAsync();
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Size", queryParameters.PageSize.ToString());
            Response.Headers.Append("X-Page-Number", queryParameters.PageNumber.ToString());
            Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling((double)totalCount / queryParameters.PageSize)).ToString());

            var games = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(g => ToDto(g))
                .ToListAsync();

            return Ok(games);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BoardGameDto>> Get(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return Ok(ToDto(game));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<ActionResult<BoardGameDto>> Post([FromBody] BoardGameCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var game = new BoardGame
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Category = model.Category,
                MinPlayers = model.MinPlayers,
                MaxPlayers = model.MaxPlayers,
                AveragePlayTime = TimeSpan.FromMinutes(model.AveragePlayTimeMinutes),
                Complexity = model.Complexity
            };

            _context.BoardGames.Add(game);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = game.Id }, ToDto(game));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<BoardGameDto>> Put(Guid id, [FromBody] BoardGameUpdateDto model)
        {
            if (!ModelState.IsValid || id != model.Id)
            {
                return BadRequest(ModelState);
            }

            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            game.Name = model.Name;
            game.Category = model.Category;
            game.MinPlayers = model.MinPlayers;
            game.MaxPlayers = model.MaxPlayers;
            game.AveragePlayTime = TimeSpan.FromMinutes(model.AveragePlayTimeMinutes);
            game.Complexity = model.Complexity;

            await _context.SaveChangesAsync();
            return Ok(ToDto(game));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            _context.BoardGames.Remove(game);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static BoardGameDto ToDto(BoardGame game)
        {
            return new BoardGameDto
            {
                Id = game.Id,
                Name = game.Name,
                Category = game.Category,
                MinPlayers = game.MinPlayers,
                MaxPlayers = game.MaxPlayers,
                AveragePlayTimeMinutes = (int)game.AveragePlayTime.TotalMinutes,
                Complexity = game.Complexity
            };
        }
    }
}

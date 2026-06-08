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
    [Route("api/boardgames")]
    public class BoardGamesApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;

        public BoardGamesApiController(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoardGameDto>>> Get([FromQuery] string? q)
        {
            var query = _context.BoardGames.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(g => g.Name.Contains(q));
            }

            var games = await query
                .OrderBy(g => g.Name)
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

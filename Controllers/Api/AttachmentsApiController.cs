using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace BoardGameLeague.Controllers.Api
{
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/attachments")]
    public class AttachmentsApiController : ControllerBase
    {
        private readonly BoardGameLeagueDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AttachmentsApiController(BoardGameLeagueDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("{tournamentId:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AttachmentDto>>> GetAttachments(Guid tournamentId)
        {
            var attachments = await _context.Attachments
                .Where(a => a.TournamentId == tournamentId)
                                .OrderByDescending(a => a.CreatedAt)
                .Select(a => ToDto(a))
                .ToListAsync();

            return Ok(attachments);
        }

        [HttpPost("{tournamentId:guid}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<AttachmentDto>> UploadAttachment(Guid tournamentId, IFormFile file)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound("Tournament not found.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file type and size here if needed
            // For simplicity, we'll allow any file for now

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "tournaments", tournamentId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                TournamentId = tournamentId,
                FileName = file.FileName,
                FilePath = Path.Combine("/uploads", "tournaments", tournamentId.ToString(), uniqueFileName).Replace('\\', '/'),
                ContentType = file.ContentType,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttachments), new { tournamentId = tournamentId, id = attachment.Id }, ToDto(attachment));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var attachment = await _context.Attachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            // Delete physical file
            var physicalPath = Path.Combine(_env.WebRootPath, attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static AttachmentDto ToDto(Attachment attachment)
        {
            return new AttachmentDto
            {
                Id = attachment.Id,
                TournamentId = attachment.TournamentId,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                CreatedAt = attachment.CreatedAt
            };
        }
    }
}

using BoardGameLeague.Models;
using BoardGameLeague.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLeague.Controllers.Api
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatAgentService _chatAgentService;

        public ChatApiController(IChatAgentService chatAgentService)
        {
            _chatAgentService = chatAgentService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<ChatResponse>> Send([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty." });
            }

            if (request.Message.Length > 2000)
            {
                return BadRequest(new { error = "Message is too long." });
            }

            // The chat agent decides what the user is allowed to do purely from
            // User (the authenticated principal on this request), never from anything
            // supplied by the client - the client can't grant itself permissions.
            var result = await _chatAgentService.HandleMessageAsync(request, User, cancellationToken);
            return Ok(result);
        }
    }
}

using System.Security.Claims;
using BoardGameLeague.Models;

namespace BoardGameLeague.Services
{
    public interface IChatAgentService
    {
        Task<ChatResponse> HandleMessageAsync(ChatRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }
}

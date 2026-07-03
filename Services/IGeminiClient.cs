using BoardGameLeague.Models;

namespace BoardGameLeague.Services
{
    public interface IGeminiClient
    {
        Task<GeminiCandidate?> GenerateAsync(GeminiRequest request, CancellationToken cancellationToken = default);
    }
}

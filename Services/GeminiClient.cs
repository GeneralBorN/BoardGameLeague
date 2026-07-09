using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BoardGameLeague.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Services
{
    // Thin wrapper around the Gemini "generateContent" REST endpoint. Deliberately raw
    // HttpClient + System.Text.Json rather than a full SDK, to keep the dependency
    // footprint small - the request/response shapes needed for function calling are
    // simple enough to model directly (see Models/ChatModels.cs).
    public class GeminiClient : IGeminiClient
    {
        // Gemini returns 503 ("model is currently experiencing high demand") and 429
        // (rate limited) fairly routinely under load, and both are expected to be
        // retried by the caller rather than treated as hard failures.
        private static readonly HttpStatusCode[] RetryableStatusCodes =
        {
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.TooManyRequests
        };

        private const int MaxAttempts = 3;

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiClient> _logger;

        public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GeminiCandidate?> GenerateAsync(GeminiRequest request, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini:ApiKey is not configured. Set it via 'dotnet user-secrets set \"Gemini:ApiKey\" \"<key>\"' - never commit it to appsettings.json.");
            }

            var model = _configuration["Gemini:Model"] ?? "gemini-3.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken: cancellationToken);

                    if (payload?.PromptFeedback?.BlockReason is string blockReason)
                    {
                        _logger.LogWarning("Gemini blocked the prompt: {Reason}", blockReason);
                        return null;
                    }

                    return payload?.Candidates?.FirstOrDefault();
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var canRetry = attempt < MaxAttempts && RetryableStatusCodes.Contains(response.StatusCode);

                if (canRetry)
                {
                    var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(
                        "Gemini API request failed with {StatusCode} (attempt {Attempt}/{MaxAttempts}), retrying in {DelayMs}ms: {Body}",
                        response.StatusCode, attempt, MaxAttempts, delay.TotalMilliseconds, body);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                _logger.LogWarning("Gemini API request failed with {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Gemini API request failed with status {response.StatusCode}.");
            }

            throw new InvalidOperationException("Gemini API request failed after retries.");
        }
    }
}

using System.Text.Json.Serialization;

namespace BoardGameLeague.Models
{
    // ---- Public request/response contract for the chat widget ----

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatTurnDto> History { get; set; } = new();
    }

    public class ChatTurnDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "model"
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public List<ChatLinkDto> Links { get; set; } = new();
    }

    public class ChatLinkDto
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    // ---- Gemini REST wire format (generateContent, function calling) ----

    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("tools")]
        public List<GeminiTool>? Tools { get; set; }

        [JsonPropertyName("systemInstruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GeminiGenerationConfig
    {
        // Gemini 2.5's "thinking" models spend part of the output token budget on internal
        // reasoning before writing the actual response/function call; with a small or default
        // budget that reasoning can consume the whole thing and leave zero tokens for real
        // output (an empty candidate with no text and no functionCall). Setting this explicitly
        // keeps enough headroom left over regardless of how much thinking a given turn needs.
        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 4096;
    }

    public class GeminiTool
    {
        [JsonPropertyName("functionDeclarations")]
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    public class GeminiFunctionDeclaration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public object? Parameters { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("functionCall")]
        public GeminiFunctionCall? FunctionCall { get; set; }

        [JsonPropertyName("functionResponse")]
        public GeminiFunctionResponse? FunctionResponse { get; set; }

        // Newer "thinking" models return an opaque signature alongside function calls that
        // must be echoed back verbatim in the next turn, or the API rejects the request with
        // "Function call is missing a thought_signature". Round-tripping it through this model
        // (deserialize on the way in, reserialize on the way out) is all that's needed since we
        // replay the same GeminiPart object back as the "model" turn in ChatAgentService.
        [JsonPropertyName("thoughtSignature")]
        public string? ThoughtSignature { get; set; }
    }

    public class GeminiFunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public System.Text.Json.JsonElement Args { get; set; }
    }

    public class GeminiFunctionResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public object Response { get; set; } = new { };
    }

    public class GeminiGenerateResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }

        [JsonPropertyName("promptFeedback")]
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    public class GeminiPromptFeedback
    {
        [JsonPropertyName("blockReason")]
        public string? BlockReason { get; set; }
    }
}

namespace BoardGameLeague.Models
{
    public class CopilotChatWebhook
    {
        public string? EventType { get; set; }
        public string? ConversationId { get; set; }
        public string? UserId { get; set; }
        public string? Text { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class CopilotChatResponse
    {
        public string Status { get; set; } = "ok";
        public string? ConversationId { get; set; }
        public string ResponseText { get; set; } = string.Empty;
        public object? Extra { get; set; }
    }
}
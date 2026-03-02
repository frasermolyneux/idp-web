namespace MX.IDP.Web.Models;

public class ChatApiResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? Agent { get; set; }
    public TokenUsageInfo? Usage { get; set; }
}

public class TokenUsageInfo
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

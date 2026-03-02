namespace MX.IDP.Web.Models;

public class ChatApiResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? Agent { get; set; }
    public TokenUsageInfo? Usage { get; set; }
    public List<TokenLogprobInfo>? Logprobs { get; set; }
    public List<FunctionCallInfo>? FunctionCalls { get; set; }
}

public class TokenUsageInfo
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class TokenLogprobInfo
{
    public string Token { get; set; } = string.Empty;
    public double Logprob { get; set; }
    public List<TokenAlternativeInfo> TopAlternatives { get; set; } = new();
}

public class TokenAlternativeInfo
{
    public string Token { get; set; } = string.Empty;
    public double Logprob { get; set; }
}

public class FunctionCallInfo
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, string>? Arguments { get; set; }
    public string? ResultPreview { get; set; }
    public int Order { get; set; }
}

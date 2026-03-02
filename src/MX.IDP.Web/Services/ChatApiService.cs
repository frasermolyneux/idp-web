using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Web;
using MX.IDP.Web.Models;

namespace MX.IDP.Web.Services;

public class ChatApiService : IChatApiService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatApiService> _logger;

    public ChatApiService(
        HttpClient httpClient,
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration,
        ILogger<ChatApiService> logger)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChatApiResponse> SendMessageAsync(string message, string? conversationId, List<ChatMessage>? history)
    {
        await AttachBearerTokenAsync();

        var payload = new
        {
            message,
            conversationId,
            history = history?.Select(m => new { m.Role, m.Content }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/chat", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            var apiResponse = new ChatApiResponse
            {
                Message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? result : result,
                ConversationId = root.TryGetProperty("conversationId", out var convProp) ? convProp.GetString() : null
            };

            if (root.TryGetProperty("usage", out var usageProp) && usageProp.ValueKind == JsonValueKind.Object)
            {
                apiResponse.Usage = new TokenUsageInfo
                {
                    PromptTokens = usageProp.TryGetProperty("promptTokens", out var pt) ? pt.GetInt32() : 0,
                    CompletionTokens = usageProp.TryGetProperty("completionTokens", out var ct) ? ct.GetInt32() : 0,
                    TotalTokens = usageProp.TryGetProperty("totalTokens", out var tt) ? tt.GetInt32() : 0
                };
            }

            return apiResponse;
        }
        catch (JsonException)
        {
            return new ChatApiResponse { Message = result };
        }
    }

    private async Task AttachBearerTokenAsync()
    {
        var scopes = _configuration.GetValue<string>("IdpAgents:Scopes");
        if (string.IsNullOrEmpty(scopes)) return;

        var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { scopes });
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

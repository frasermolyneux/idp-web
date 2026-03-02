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

    public async Task<string> SendMessageAsync(string message, string? conversationId, List<ChatMessage>? history)
    {
        try
        {
            await AttachBearerTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acquire OBO token. Calling API without authentication.");
        }

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

        // Try to parse as JSON response from idp-agents
        try
        {
            using var doc = JsonDocument.Parse(result);
            if (doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                return messageProp.GetString() ?? result;
            }
        }
        catch (JsonException)
        {
            // Not JSON — return raw string
        }

        return result;
    }

    private async Task AttachBearerTokenAsync()
    {
        var scopes = _configuration.GetValue<string>("IdpAgents:Scopes");
        if (string.IsNullOrEmpty(scopes)) return;

        var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { scopes });
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

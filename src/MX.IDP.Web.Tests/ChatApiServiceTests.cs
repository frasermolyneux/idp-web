using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

using Moq;

using MX.IDP.Web.Models;
using MX.IDP.Web.Services;

using Xunit;

namespace MX.IDP.Web.Tests;

public class ChatApiServiceTests
{
    private readonly Mock<ITokenAcquisition> _mockTokenAcquisition;
    private readonly Mock<ILogger<ChatApiService>> _mockLogger;

    public ChatApiServiceTests()
    {
        _mockTokenAcquisition = new Mock<ITokenAcquisition>();
        _mockLogger = new Mock<ILogger<ChatApiService>>();
    }

    private ChatApiService CreateService(HttpClient httpClient, Dictionary<string, string?>? configValues = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>())
            .Build();

        return new ChatApiService(httpClient, _mockTokenAcquisition.Object, config, _mockLogger.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendMessageAsync_ReturnsMessageFromJsonResponse()
    {
        var responseBody = JsonSerializer.Serialize(new { message = "Hello from agent", conversationId = "conv-1" });
        var handler = new MockHttpMessageHandler(responseBody, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };

        var sut = CreateService(httpClient);

        var result = await sut.SendMessageAsync("Hi", null, null);

        Assert.Equal("Hello from agent", result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendMessageAsync_ReturnsRawString_WhenNotJson()
    {
        var handler = new MockHttpMessageHandler("plain text response", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };

        var sut = CreateService(httpClient);

        var result = await sut.SendMessageAsync("Hi", null, null);

        Assert.Equal("plain text response", result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendMessageAsync_SendsCorrectPayload()
    {
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(new { message = "ok" }),
            HttpStatusCode.OK,
            onRequest: async req => capturedBody = await req.Content!.ReadAsStringAsync());

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };

        var sut = CreateService(httpClient);

        await sut.SendMessageAsync("Test message", "conv-42",
        [
            new ChatMessage { Role = "user", Content = "prior" }
        ]);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody!);
        Assert.Equal("Test message", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("conv-42", doc.RootElement.GetProperty("conversationId").GetString());
        Assert.Single(doc.RootElement.GetProperty("history").EnumerateArray());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendMessageAsync_AttachesBearerToken_WhenScopesConfigured()
    {
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<System.Security.Claims.ClaimsPrincipal?>(),
                It.IsAny<TokenAcquisitionOptions?>()))
            .ReturnsAsync("test-token-123");

        string? authHeader = null;
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(new { message = "ok" }),
            HttpStatusCode.OK,
            onRequest: req =>
            {
                authHeader = req.Headers.Authorization?.ToString();
                return Task.CompletedTask;
            });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };

        var config = new Dictionary<string, string?>
        {
            ["IdpAgents:Scopes"] = "api://test/.default"
        };

        var sut = CreateService(httpClient, config);

        await sut.SendMessageAsync("Hi", null, null);

        Assert.Equal("Bearer test-token-123", authHeader);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendMessageAsync_ContinuesWithoutAuth_WhenTokenAcquisitionFails()
    {
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<System.Security.Claims.ClaimsPrincipal?>(),
                It.IsAny<TokenAcquisitionOptions?>()))
            .ThrowsAsync(new InvalidOperationException("No token"));

        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(new { message = "ok" }),
            HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };

        var config = new Dictionary<string, string?>
        {
            ["IdpAgents:Scopes"] = "api://test/.default"
        };

        var sut = CreateService(httpClient, config);

        var result = await sut.SendMessageAsync("Hi", null, null);

        Assert.Equal("ok", result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendMessageAsync_ThrowsOnHttpError()
    {
        var handler = new MockHttpMessageHandler("error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };

        var sut = CreateService(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.SendMessageAsync("Hi", null, null));
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;
        private readonly Func<HttpRequestMessage, Task>? _onRequest;

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode, Func<HttpRequestMessage, Task>? onRequest = null)
        {
            _response = response;
            _statusCode = statusCode;
            _onRequest = onRequest;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_onRequest != null) await _onRequest(request);
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response)
            };
        }
    }
}

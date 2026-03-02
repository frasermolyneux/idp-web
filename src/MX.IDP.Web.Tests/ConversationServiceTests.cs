using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using MX.IDP.Web.Models;
using MX.IDP.Web.Services;

using Xunit;

namespace MX.IDP.Web.Tests;

public class ConversationServiceTests
{
    private readonly Mock<ILogger<ConversationService>> _mockLogger;

    public ConversationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConversationService>>();
    }

    private ConversationService CreateUnconfiguredService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new ConversationService(_mockLogger.Object, config);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetConversationsAsync_WhenNotConfigured_ReturnsEmptyList()
    {
        var sut = CreateUnconfiguredService();

        var result = await sut.GetConversationsAsync("user-1");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMessagesAsync_WhenNotConfigured_ReturnsEmptyList()
    {
        var sut = CreateUnconfiguredService();

        var result = await sut.GetMessagesAsync("conv-1");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetOrCreateConversationAsync_WhenNotConfigured_ReturnsNewConversation()
    {
        var sut = CreateUnconfiguredService();

        var result = await sut.GetOrCreateConversationAsync("user-1", null);

        Assert.NotNull(result);
        Assert.Equal("user-1", result.UserId);
        Assert.True(Guid.TryParse(result.Id, out _));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetOrCreateConversationAsync_WhenNotConfigured_PreservesConversationId()
    {
        var sut = CreateUnconfiguredService();

        var result = await sut.GetOrCreateConversationAsync("user-1", "existing-conv");

        Assert.Equal("existing-conv", result.Id);
        Assert.Equal("user-1", result.UserId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveMessageAsync_WhenNotConfigured_DoesNotThrow()
    {
        var sut = CreateUnconfiguredService();

        var message = new ChatMessage { Role = "user", Content = "Hello" };

        var exception = await Record.ExceptionAsync(() => sut.SaveMessageAsync("conv-1", message));

        Assert.Null(exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateConversationTitleAsync_WhenNotConfigured_DoesNotThrow()
    {
        var sut = CreateUnconfiguredService();

        var exception = await Record.ExceptionAsync(() => sut.UpdateConversationTitleAsync("conv-1", "New Title"));

        Assert.Null(exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteConversationAsync_WhenNotConfigured_DoesNotThrow()
    {
        var sut = CreateUnconfiguredService();

        var exception = await Record.ExceptionAsync(() => sut.DeleteConversationAsync("user-1", "conv-1"));

        Assert.Null(exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenNotConfigured_LogsWarning()
    {
        CreateUnconfiguredService();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CosmosDB is not configured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

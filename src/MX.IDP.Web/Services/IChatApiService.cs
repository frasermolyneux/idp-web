using MX.IDP.Web.Models;

namespace MX.IDP.Web.Services;

public interface IChatApiService
{
    Task<string> SendMessageAsync(string message, string? conversationId, List<ChatMessage>? history);
}

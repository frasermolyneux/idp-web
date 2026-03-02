using MX.IDP.Web.Models;

namespace MX.IDP.Web.Services;

public interface IChatApiService
{
    Task<ChatApiResponse> SendMessageAsync(string message, string? conversationId, List<ChatMessage>? history);
}

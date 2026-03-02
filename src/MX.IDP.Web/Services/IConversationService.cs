using MX.IDP.Web.Models;

namespace MX.IDP.Web.Services;

public interface IConversationService
{
    Task<List<Conversation>> GetConversationsAsync(string userId);
    Task<Conversation> GetOrCreateConversationAsync(string userId, string? conversationId);
    Task<List<ChatMessage>> GetMessagesAsync(string conversationId);
    Task SaveMessageAsync(string conversationId, ChatMessage message);
    Task UpdateConversationTitleAsync(string conversationId, string title);
    Task DeleteConversationAsync(string userId, string conversationId);
}

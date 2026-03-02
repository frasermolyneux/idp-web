using Microsoft.Azure.Cosmos;
using MX.IDP.Web.Models;

namespace MX.IDP.Web.Services;

public class ConversationService : IConversationService
{
    private readonly CosmosClient? _cosmosClient;
    private readonly string _databaseName;
    private readonly ILogger<ConversationService> _logger;
    private readonly bool _isConfigured;

    public ConversationService(ILogger<ConversationService> logger, IConfiguration configuration, CosmosClient? cosmosClient = null)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "idp";
        _isConfigured = _cosmosClient != null && !string.IsNullOrEmpty(configuration.GetValue<string>("CosmosDb:Endpoint"));

        if (!_isConfigured)
        {
            _logger.LogWarning("CosmosDB is not configured. Conversation persistence is disabled.");
        }
    }

    private Container GetContainer(string containerName) =>
        _cosmosClient!.GetDatabase(_databaseName).GetContainer(containerName);

    public async Task<List<Conversation>> GetConversationsAsync(string userId)
    {
        if (!_isConfigured) return [];

        try
        {
            var container = GetContainer("conversations");
            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId ORDER BY c.updatedAt DESC")
                .WithParameter("@userId", userId);

            var results = new List<Conversation>();
            using var iterator = container.GetItemQueryIterator<Conversation>(query, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userId)
            });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversations for user {UserId}", userId);
            return [];
        }
    }

    public async Task<Conversation> GetOrCreateConversationAsync(string userId, string? conversationId)
    {
        if (!_isConfigured)
        {
            return new Conversation { Id = conversationId ?? Guid.NewGuid().ToString(), UserId = userId };
        }

        try
        {
            var container = GetContainer("conversations");

            if (!string.IsNullOrEmpty(conversationId))
            {
                try
                {
                    var response = await container.ReadItemAsync<Conversation>(conversationId, new PartitionKey(userId));
                    return response.Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Fall through to create
                }
            }

            var conversation = new Conversation
            {
                Id = conversationId ?? Guid.NewGuid().ToString(),
                UserId = userId
            };

            await container.CreateItemAsync(conversation, new PartitionKey(userId));
            return conversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create conversation {ConversationId}", conversationId);
            return new Conversation { Id = conversationId ?? Guid.NewGuid().ToString(), UserId = userId };
        }
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId)
    {
        if (!_isConfigured) return [];

        try
        {
            var container = GetContainer("messages");
            var query = new QueryDefinition("SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.timestamp ASC")
                .WithParameter("@conversationId", conversationId);

            var results = new List<ChatMessage>();
            using var iterator = container.GetItemQueryIterator<ChatMessage>(query, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId)
            });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for conversation {ConversationId}", conversationId);
            return [];
        }
    }

    public async Task SaveMessageAsync(string conversationId, ChatMessage message)
    {
        if (!_isConfigured) return;

        try
        {
            var container = GetContainer("messages");
            var document = new
            {
                id = message.Id,
                conversationId,
                role = message.Role,
                content = message.Content,
                timestamp = message.Timestamp
            };

            await container.CreateItemAsync(document, new PartitionKey(conversationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save message to conversation {ConversationId}", conversationId);
        }
    }

    public async Task UpdateConversationTitleAsync(string conversationId, string title)
    {
        if (!_isConfigured) return;

        try
        {
            var container = GetContainer("conversations");

            var patchOperations = new[]
            {
                PatchOperation.Set("/title", title),
                PatchOperation.Set("/updatedAt", DateTime.UtcNow)
            };

            // We need the userId to patch; query first
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", conversationId);

            using var iterator = container.GetItemQueryIterator<Conversation>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var conversation = response.FirstOrDefault();
                if (conversation != null)
                {
                    await container.PatchItemAsync<Conversation>(conversationId, new PartitionKey(conversation.UserId), patchOperations);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update conversation title {ConversationId}", conversationId);
        }
    }

    public async Task DeleteConversationAsync(string userId, string conversationId)
    {
        if (!_isConfigured) return;

        try
        {
            var container = GetContainer("conversations");
            await container.DeleteItemAsync<Conversation>(conversationId, new PartitionKey(userId));

            // Also delete messages
            var messagesContainer = GetContainer("messages");
            var query = new QueryDefinition("SELECT c.id FROM c WHERE c.conversationId = @conversationId")
                .WithParameter("@conversationId", conversationId);

            using var iterator = messagesContainer.GetItemQueryIterator<dynamic>(query, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId)
            });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    string id = item.id;
                    await messagesContainer.DeleteItemAsync<dynamic>(id, new PartitionKey(conversationId));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete conversation {ConversationId}", conversationId);
        }
    }
}

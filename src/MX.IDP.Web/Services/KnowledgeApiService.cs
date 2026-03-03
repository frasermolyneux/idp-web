using System.Text.Json;

using Microsoft.Identity.Web;

namespace MX.IDP.Web.Services;

public interface IKnowledgeApiService
{
    Task<KnowledgeStatsResponse> GetStatsAsync();
    Task<string> TriggerReindexAsync(string sourceType = "all");
}

public class KnowledgeApiService : AuthenticatedApiService, IKnowledgeApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public KnowledgeApiService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IConfiguration configuration)
        : base(httpClient, tokenAcquisition, configuration)
    {
    }

    public async Task<KnowledgeStatsResponse> GetStatsAsync()
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync("/api/knowledge/stats");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KnowledgeStatsResponse>(json, JsonOptions) ?? new();
    }

    public async Task<string> TriggerReindexAsync(string sourceType = "all")
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/knowledge/reindex?sourceType={sourceType}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}

public class KnowledgeStatsResponse
{
    public long TotalChunks { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalSources { get; set; }
    public List<KnowledgeSourceStatsDto> Sources { get; set; } = [];
}

public class KnowledgeSourceStatsDto
{
    public string SourceType { get; set; } = "";
    public string SourceName { get; set; } = "";
    public long ChunkCount { get; set; }
    public int DocumentCount { get; set; }
    public List<KnowledgeDocumentDto> Documents { get; set; } = [];
}

public class KnowledgeDocumentDto
{
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int ChunkCount { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}

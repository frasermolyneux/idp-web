using System.Text;
using System.Text.Json;

using Microsoft.Identity.Web;

namespace MX.IDP.Web.Services;

public interface ICampaignApiService
{
    Task<List<CampaignSummary>> ListCampaignsAsync();
    Task<CampaignSummary?> GetCampaignAsync(string campaignId);
    Task<List<CampaignFindingSummary>> GetFindingsAsync(string campaignId);
    Task<List<CampaignFindingSummary>> GetPendingApprovalsAsync(string campaignId);
    Task<CampaignSummary> CreateCampaignAsync(CreateCampaignRequest request);
    Task<CampaignSummary> RunCampaignAsync(string campaignId);
    Task<List<CampaignTemplateSummary>> ListTemplatesAsync();
    Task ApproveFindingAsync(string campaignId, string findingId);
    Task RejectFindingAsync(string campaignId, string findingId);
    Task BulkApproveAsync(string campaignId);
    Task BulkRejectAsync(string campaignId);
}

public class CampaignApiService : ICampaignApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;

    public CampaignApiService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
    }

    private async Task AttachBearerTokenAsync()
    {
        var scopes = new[] { _configuration["IdpAgents:Scopes"] ?? "" };
        var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<CampaignSummary>> ListCampaignsAsync()
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.GetAsync("/api/campaigns?userId=system");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignSummary>>(json, JsonOptions) ?? [];
    }

    public async Task<CampaignSummary?> GetCampaignAsync(string campaignId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.GetAsync($"/api/campaigns/{campaignId}?userId=system");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions);
    }

    public async Task<List<CampaignFindingSummary>> GetFindingsAsync(string campaignId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.GetAsync($"/api/campaigns/{campaignId}/findings");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignFindingSummary>>(json, JsonOptions) ?? [];
    }

    public async Task<CampaignSummary> CreateCampaignAsync(CreateCampaignRequest request)
    {
        await AttachBearerTokenAsync();
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/campaigns", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<CampaignSummary> RunCampaignAsync(string campaignId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/run?userId=system", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<List<CampaignTemplateSummary>> ListTemplatesAsync()
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.GetAsync("/api/campaigns/templates");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignTemplateSummary>>(json, JsonOptions) ?? [];
    }

    public async Task<List<CampaignFindingSummary>> GetPendingApprovalsAsync(string campaignId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.GetAsync($"/api/campaigns/{campaignId}/findings?status=pending_approval");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignFindingSummary>>(json, JsonOptions) ?? [];
    }

    public async Task ApproveFindingAsync(string campaignId, string findingId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/findings/{findingId}/approve", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectFindingAsync(string campaignId, string findingId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/findings/{findingId}/reject", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkApproveAsync(string campaignId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/findings/approve-all", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkRejectAsync(string campaignId)
    {
        await AttachBearerTokenAsync();
        var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/findings/reject-all", null);
        response.EnsureSuccessStatusCode();
    }
}

public class CampaignSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string Status { get; set; } = "";
    public string Description { get; set; } = "";
    public CampaignStatsSummary Stats { get; set; } = new();
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CampaignStatsSummary
{
    public int TotalFindings { get; set; }
    public int IssuesCreated { get; set; }
    public int IssuesOpen { get; set; }
    public int IssuesClosed { get; set; }
    public int IssuesSkipped { get; set; }
    public double ProgressPercent { get; set; }
}

public class CampaignFindingSummary
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Severity { get; set; } = "";
    public string? Repo { get; set; }
    public string Status { get; set; } = "";
    public int? IssueNumber { get; set; }
    public string? IssueUrl { get; set; }
}

public class CampaignTemplateSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string Icon { get; set; } = "📋";
}

public class CreateCampaignRequest
{
    public string Name { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string? Description { get; set; }
    public string UserId { get; set; } = "system";
}

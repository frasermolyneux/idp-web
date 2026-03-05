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
    Task<CampaignSummary> PreviewCampaignAsync(string campaignId);
    Task<CampaignSummary> PauseCampaignAsync(string campaignId);
    Task<CampaignSummary> ResumeCampaignAsync(string campaignId);
    Task<CampaignSummary> CancelCampaignAsync(string campaignId);
    Task DeleteCampaignAsync(string campaignId);
    Task<List<CampaignTemplateSummary>> ListTemplatesAsync();
    Task<CampaignSummary> CreateFromTemplateAsync(string templateId, string? name = null);
    Task ApproveFindingAsync(string campaignId, string findingId);
    Task RejectFindingAsync(string campaignId, string findingId);
    Task BulkApproveAsync(string campaignId);
    Task BulkRejectAsync(string campaignId);
}

public class CampaignApiService : AuthenticatedApiService, ICampaignApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CampaignApiService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IConfiguration configuration)
        : base(httpClient, tokenAcquisition, configuration)
    {
    }

    public async Task<List<CampaignSummary>> ListCampaignsAsync()
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync("/api/campaigns?userId=system");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignSummary>>(json, JsonOptions) ?? [];
    }

    public async Task<CampaignSummary?> GetCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync($"/api/campaigns/{campaignId}?userId=system");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions);
    }

    public async Task<List<CampaignFindingSummary>> GetFindingsAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync($"/api/campaigns/{campaignId}/findings");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignFindingSummary>>(json, JsonOptions) ?? [];
    }

    public async Task<CampaignSummary> CreateCampaignAsync(CreateCampaignRequest request)
    {
        await EnsureAuthAsync();
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await Http.PostAsync("/api/campaigns", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<CampaignSummary> RunCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/run?userId=system", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<List<CampaignTemplateSummary>> ListTemplatesAsync()
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync("/api/campaigns/templates");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignTemplateSummary>>(json, JsonOptions) ?? [];
    }

    public async Task<List<CampaignFindingSummary>> GetPendingApprovalsAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync($"/api/campaigns/{campaignId}/findings?status=pending_approval");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CampaignFindingSummary>>(json, JsonOptions) ?? [];
    }

    public async Task ApproveFindingAsync(string campaignId, string findingId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/findings/{findingId}/approve", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectFindingAsync(string campaignId, string findingId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/findings/{findingId}/reject", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkApproveAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/findings/approve-all", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkRejectAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/findings/reject-all", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CampaignSummary> PreviewCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/run?userId=system&dryRun=true", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<CampaignSummary> PauseCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/pause?userId=system", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<CampaignSummary> ResumeCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/resume?userId=system", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task<CampaignSummary> CancelCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.PostAsync($"/api/campaigns/{campaignId}/cancel?userId=system", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CampaignSummary>(json, JsonOptions)!;
    }

    public async Task DeleteCampaignAsync(string campaignId)
    {
        await EnsureAuthAsync();
        var response = await Http.DeleteAsync($"/api/campaigns/{campaignId}?userId=system");
        response.EnsureSuccessStatusCode();
    }

    public async Task<CampaignSummary> CreateFromTemplateAsync(string templateId, string? name = null)
    {
        await EnsureAuthAsync();
        var templates = await ListTemplatesAsync();
        var template = templates.FirstOrDefault(t => t.Id == templateId);
        if (template is null) throw new InvalidOperationException($"Template '{templateId}' not found");

        return await CreateCampaignAsync(new CreateCampaignRequest
        {
            Name = name ?? template.Name,
            SourceType = template.SourceType,
            Description = template.Description
        });
    }
}

public class CampaignSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string Status { get; set; } = "";
    public string Description { get; set; } = "";
    public string ActionMode { get; set; } = "issue";
    public bool RequireApproval { get; set; }
    public CampaignScheduleSummary? Schedule { get; set; }
    public CampaignStatsSummary Stats { get; set; } = new();
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CampaignScheduleSummary
{
    public string CronExpression { get; set; } = "";
    public bool Enabled { get; set; }
    public DateTimeOffset? NextRun { get; set; }
    public DateTimeOffset? LastScheduledRun { get; set; }
}

public class CampaignStatsSummary
{
    public int TotalFindings { get; set; }
    public int IssuesCreated { get; set; }
    public int IssuesOpen { get; set; }
    public int IssuesClosed { get; set; }
    public int IssuesSkipped { get; set; }
    public int PendingApproval { get; set; }
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
    public string ActionMode { get; set; } = "issue";
    public bool RequireApproval { get; set; }
    public CreateCampaignFilter? Filter { get; set; }
    public CreateCampaignSchedule? Schedule { get; set; }
}

public class CreateCampaignFilter
{
    public string? Category { get; set; }
    public string? Impact { get; set; }
    public List<string>? Repos { get; set; }
    public List<string>? RepoTopics { get; set; }
    public List<string>? ExcludeRepos { get; set; }
    public List<string>? ResourceGroups { get; set; }
    public string? Severity { get; set; }
    public string? AssignTo { get; set; }
}

public class CreateCampaignSchedule
{
    public string CronExpression { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

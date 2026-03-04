using System.Text.Json;

using Microsoft.Identity.Web;

namespace MX.IDP.Web.Services;

public interface IToolsCatalogApiService
{
    Task<ToolsCatalogResponse> GetCatalogAsync();
}

public class ToolsCatalogApiService : AuthenticatedApiService, IToolsCatalogApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ToolsCatalogApiService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IConfiguration configuration)
        : base(httpClient, tokenAcquisition, configuration)
    {
    }

    public async Task<ToolsCatalogResponse> GetCatalogAsync()
    {
        await EnsureAuthAsync();
        var response = await Http.GetAsync("/api/tools/catalog");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ToolsCatalogResponse>(json, JsonOptions) ?? new();
    }
}

public class ToolsCatalogResponse
{
    public int TotalPlugins { get; set; }
    public int TotalFunctions { get; set; }
    public List<ToolPluginDto> Plugins { get; set; } = [];
}

public class ToolPluginDto
{
    public string Plugin { get; set; } = "";
    public string ToolClass { get; set; } = "";
    public List<string> Agents { get; set; } = [];
    public List<ToolFunctionDto> Functions { get; set; } = [];
}

public class ToolFunctionDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ToolParameterDto> Parameters { get; set; } = [];
}

public class ToolParameterDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
}

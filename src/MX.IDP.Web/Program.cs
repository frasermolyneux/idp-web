using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Azure.Cosmos;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MX.IDP.Web;
using MX.IDP.Web.Components;
using MX.IDP.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Entra ID Authentication
var initialScopes = new[] { builder.Configuration["IdpAgents:Scopes"] ?? "" }
    .Where(s => !string.IsNullOrEmpty(s)).ToArray();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Cosmos DB client
var cosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"];
if (!string.IsNullOrEmpty(cosmosEndpoint))
{
    builder.Services.AddSingleton(sp => new CosmosClient(cosmosEndpoint, new DefaultAzureCredential()));
}

// Register services
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<ConversationNotifier>();

// Configure ChatApiService HttpClient
builder.Services.AddHttpClient<IChatApiService, ChatApiService>(client =>
{
    var baseUrl = builder.Configuration["IdpAgents:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    // Chat completions with multi-step tool calling need generous timeouts
    client.Timeout = TimeSpan.FromMinutes(5);
});

// FluentUI
builder.Services.AddFluentUIComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapControllers();

app.MapInfoEndpoint();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

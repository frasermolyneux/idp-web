using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace MX.IDP.Web.Services;

/// <summary>
/// Base class for API services that need bearer token auth.
/// Centralises token acquisition and handles MsalUiRequiredException
/// by throwing MicrosoftIdentityWebChallengeUserException, which pages
/// can catch to trigger a re-sign-in.
/// </summary>
public abstract class AuthenticatedApiService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    protected AuthenticatedApiService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _scopes = new[] { configuration["IdpAgents:Scopes"] ?? "" };
    }

    protected HttpClient Http => _httpClient;

    protected async Task EnsureAuthAsync()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            throw; // Already the right type — let it propagate
        }
        catch (Microsoft.Identity.Client.MsalUiRequiredException ex)
        {
            // Wrap in the type that Blazor pages handle
            throw new MicrosoftIdentityWebChallengeUserException(ex, _scopes);
        }
    }
}

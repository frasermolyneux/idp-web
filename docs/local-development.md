# Local Development - IDP Web Portal

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [VS Code](https://code.visualstudio.com/) (recommended)

## Authentication Setup

The web app uses Entra ID (Azure AD) for authentication with OBO flow to call idp-agents. You need the following secrets configured via `dotnet user-secrets`:

```bash
cd src/MX.IDP.Web

# Entra ID app registration (get values from Azure Portal or idp-core Terraform outputs)
dotnet user-secrets set "AzureAd:Instance" "https://login.microsoftonline.com/"
dotnet user-secrets set "AzureAd:TenantId" "<your-tenant-id>"
dotnet user-secrets set "AzureAd:ClientId" "<idp-web-client-id>"
dotnet user-secrets set "AzureAd:ClientSecret" "<idp-web-client-secret>"
dotnet user-secrets set "AzureAd:CallbackPath" "/signin-oidc"

# IDP Agents API
dotnet user-secrets set "IdpAgents:BaseUrl" "https://fn-idp-agents-dev-swedencentral.azurewebsites.net"
dotnet user-secrets set "IdpAgents:Scopes" "api://<tenant-id>/idp-agents-dev/Mcp.ReadWrite"

# Cosmos DB (optional — conversation history disabled if not set)
dotnet user-secrets set "CosmosDb:Endpoint" "https://cosmos-idp-core-dev-swedencentral.documents.azure.com:443/"
dotnet user-secrets set "CosmosDb:DatabaseName" "idp"
```

> **Note**: For Cosmos DB access, ensure your Azure AD user has the "Cosmos DB Built-in Data Contributor" role on the Cosmos account. The app uses `DefaultAzureCredential` which picks up your `az login` session.

## Azure CLI Login

```bash
az login
az account set --subscription "<dev-subscription-id>"
```

`DefaultAzureCredential` uses your Azure CLI session for Cosmos DB access locally.

## Running

### VS Code

1. Open the `idp-web` folder in VS Code
2. Press `F5` or use the **Run and Debug** panel → select **Launch IDP Web**
3. The app starts on `https://localhost:7100`

### Command Line

```bash
cd src/MX.IDP.Web
dotnet run
```

## Building & Testing

```bash
cd src

# Build
dotnet build MX.IDP.Web/MX.IDP.Web.csproj

# Run tests
dotnet test MX.IDP.Web.Tests/MX.IDP.Web.Tests.csproj
```

## Project Structure

```
src/
├── MX.IDP.Web/                    # Main web application
│   ├── Components/
│   │   ├── Layout/                # MainLayout, ConversationList
│   │   └── Pages/                 # Home (chat page)
│   ├── Models/                    # Conversation, ChatMessage, ChatApiResponse
│   ├── Services/                  # ChatApiService, ConversationService
│   └── wwwroot/css/               # Dark theme CSS
├── MX.IDP.Web.ServiceDefaults/    # Shared service configuration
└── MX.IDP.Web.Tests/              # Unit tests (xUnit + Moq)
```

## Troubleshooting

### IDW10502: MsalUiRequiredException
Token cache lost (in-memory). Sign out and back in, or restart the app. This happens after app restarts since tokens are cached in memory.

### 401 Unauthorized on chat
Check that `IdpAgents:Scopes` matches the Entra app registration scope URI and that the web app's client is pre-authorized on the agents API app registration.

### Cosmos DB errors
If Cosmos DB is not configured, conversation history is silently disabled. Chat still works but conversations won't persist.

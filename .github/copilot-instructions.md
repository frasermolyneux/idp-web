# IDP Web Portal

## Overview
Blazor Server web application for the Internal Developer Platform. Thin chat UI that delegates all AI operations to `idp-agents` via HTTP.

## Build & Run
```bash
cd src && dotnet build MX.IDP.Web.sln
dotnet test MX.IDP.Web.sln
cd MX.IDP.Web && dotnet run
```

## Project Structure
- `src/MX.IDP.Web/` — Main Blazor Server application
  - `Components/Chat/` — Chat UI components (ChatWindow, MessageBubble, ToolResultCard)
  - `Components/Layout/` — Layout components
  - `Services/ChatService.cs` — HTTP client to idp-agents agent orchestrations
  - `Services/ConversationStore.cs` — Cosmos DB conversation history
  - `Services/CampaignService.cs` — HTTP client to idp-agents campaign endpoints
  - `Program.cs` — App startup and DI configuration
- `src/MX.IDP.Web.Tests/` — Unit tests (xUnit + Moq)
- `terraform/` — App-level Terraform (web app deployment, slots)

## Key Patterns
- Fluent UI Blazor components (Microsoft.FluentUI.AspNetCore.Components)
- OBO flow for authentication: acquires delegated token for idp-agents API
- Cosmos DB for conversation/message storage (idp-core provisions the database)
- ErrorBoundary pattern for Blazor circuit resilience
- Microsoft.Extensions.Logging → Application Insights

## Conventions
- Nullable reference types enabled
- File-scoped namespaces
- 4-space indent, CRLF line endings
- xUnit + Moq for testing, `MethodName_Condition_ExpectedResult` naming

# Refractored.GitHub.Copilot.SDK.Helpers

Reusable helper utilities for the **GitHub Copilot SDK for .NET**. This library provides common functionality for CLI-based Copilot applications.

[![NuGet](https://img.shields.io/nuget/v/Refractored.GitHub.Copilot.SDK.Helpers.svg)](https://www.nuget.org/packages/Refractored.GitHub.Copilot.SDK.Helpers/)
[![Build](https://github.com/jamesmontemagno/copilot-sdk-dotnet-helpers/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesmontemagno/copilot-sdk-dotnet-helpers/actions/workflows/ci.yml)

## Installation

```bash
dotnet add package Refractored.GitHub.Copilot.SDK.Helpers
```

## Features

### CliChecker
Checks if the Copilot CLI is installed and the user is authenticated using the Copilot SDK.

```csharp
using Refractored.GitHub.Copilot.SDK.Helpers;

// Check status with console output
var status = await CliChecker.CheckCopilotStatusAsync();

// Or check silently (no console output)
var status = await CliChecker.CheckCopilotStatusSilentAsync();

if (!CliChecker.IsReady(status))
{
    Console.WriteLine("Copilot is not ready!");
    return;
}
```

### ModelSelector
Fetches available models from the Copilot SDK and allows selection.

```csharp
using Refractored.GitHub.Copilot.SDK.Helpers;

// Interactive selection via console
var model = await ModelSelector.SelectModelAsync();

// Get all available models (names only)
var models = await ModelSelector.GetModelsAsync();

// Get models with full info including billing multipliers
var modelsInfo = await ModelSelector.GetModelsWithInfoAsync();
foreach (var modelInfo in modelsInfo)
{
    Console.WriteLine($"{modelInfo.Id}: Multiplier = {modelInfo.Billing?.Multiplier}");
}

// Get the default (first) model
var defaultModel = await ModelSelector.GetDefaultModelAsync();

// Select by index (0-based)
var model = await ModelSelector.SelectModelByIndexAsync(2);

// Optionally pass an existing CopilotClient to reuse the connection
var client = new CopilotClient();
await client.StartAsync();
var models = await ModelSelector.GetModelsAsync(client);
```

### ChatHelper
Helper methods for streaming chat interactions.

```csharp
using GitHub.Copilot.SDK;
using Refractored.GitHub.Copilot.SDK.Helpers;

// Stream response to console with colored output
await ChatHelper.SendMessageAndStreamResponse(session, "Hello, Copilot!");

// Get complete response as string
var response = await ChatHelper.SendMessageAndGetResponseAsync(session, "What is 2+2?");

// Use callbacks for custom handling
await ChatHelper.SendMessageWithCallbacksAsync(
    session,
    "Explain async/await",
    onDelta: text => Console.Write(text),
    onComplete: () => Console.WriteLine("\n[Done]"),
    onError: err => Console.WriteLine($"Error: {err}")
);
```

### SessionManager
Manages Copilot sessions including listing, resuming, and cleanup.

```csharp
using GitHub.Copilot.SDK;
using Refractored.GitHub.Copilot.SDK.Helpers;

// List all available sessions with formatted console output
var sessions = await SessionManager.ListSessionsAsync();

// Get sessions without console output
var sessions = await SessionManager.GetSessionsAsync();

// Get the most recently used session ID
var lastSessionId = await SessionManager.GetLastSessionIdAsync();

// Resume the last session
var client = new CopilotClient();
await client.StartAsync();
var session = await SessionManager.ResumeLastSessionAsync(client);

// Delete a specific session
await SessionManager.DeleteSessionAsync("session-id-123");

// Clean up old sessions (older than 7 days)
int deletedCount = await SessionManager.CleanupOldSessionsAsync(TimeSpan.FromDays(7));

// Get CLI version and protocol information
var status = await SessionManager.ShowCliStatusAsync();
Console.WriteLine($"CLI Version: {status.Version}");

// Optionally pass an existing CopilotClient to reuse the connection
var client = new CopilotClient();
await client.StartAsync();
var sessions = await SessionManager.GetSessionsAsync(client);
```

## Prerequisites

- **.NET 8.0**, **.NET 9.0**, or **.NET 10.0**
- **GitHub Copilot CLI** installed and configured
- **GitHub Copilot** access on your GitHub account

### Installing Copilot CLI

```bash
# macOS/Linux (Homebrew)
brew install copilot-cli

# Windows (WinGet)
winget install GitHub.Copilot

# npm (requires Node.js 22+)
npm install -g @github/copilot

# macOS/Linux (install script)
curl -fsSL https://gh.io/copilot-install | bash
```

### Authentication

Either:
- Run `copilot`, then type `/login` and follow prompts
- Set `GH_TOKEN` environment variable with a token that has "Copilot Requests" permission

## Full Example

```csharp
using GitHub.Copilot.SDK;
using Refractored.GitHub.Copilot.SDK.Helpers;

// Check prerequisites
var status = await CliChecker.CheckCopilotStatusAsync();
if (!CliChecker.IsReady(status))
    return;

// Select a model
var model = await ModelSelector.SelectModelAsync();
if (model == null)
    return;

// Start client and create session
using var client = new CopilotClient();
await client.StartAsync();

using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model,
    Streaming = true
});

// Chat!
await ChatHelper.SendMessageAndStreamResponse(session, "Hello, Copilot!");
```

## License

MIT License - see [LICENSE](LICENSE) file.

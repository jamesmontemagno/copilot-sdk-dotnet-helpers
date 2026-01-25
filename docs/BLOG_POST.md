# Refractored.GitHub.Copilot.SDK.Helpers: Making Copilot SDK Development Easier

**Published:** January 25, 2026

## TL;DR

We created `Refractored.GitHub.Copilot.SDK.Helpers`, a reusable .NET library that bundles common helper utilities for the GitHub Copilot SDK. Stop copying helper code between projects—now you can just install a NuGet package.

**Install:** `dotnet add package Refractored.GitHub.Copilot.SDK.Helpers`

**GitHub:** https://github.com/jamesmontemagno/copilot-sdk-dotnet-helpers

---

## The Problem

If you're building applications with the [GitHub Copilot SDK for .NET](https://github.com/github/copilot-sdk), you likely need the same boilerplate code in every project:

- ✅ Checking if the Copilot CLI is installed and authenticated
- ✅ Fetching available models from the CLI
- ✅ Streaming chat responses with proper error handling
- ✅ Handling console UI for model selection

We found ourselves copying these helpers between projects, and it became clear: these utilities should be shared as a NuGet package.

## The Solution: Refractored.GitHub.Copilot.SDK.Helpers

This library provides **three well-tested helper classes** that handle the common patterns you'll encounter when building Copilot SDK applications:

### 1. **CliChecker** - Verify Prerequisites

Before you can use Copilot, you need to check:
- Is the Copilot CLI installed?
- Is the user authenticated (via `GH_TOKEN` or interactive login)?

```csharp
var status = await CliChecker.CheckCopilotStatusAsync();

if (!CliChecker.IsReady(status))
{
    Console.WriteLine("Copilot is not ready!");
    return;
}
```

There's also a silent version if you don't want console output:
```csharp
var status = await CliChecker.CheckCopilotStatusSilentAsync();
```

### 2. **ModelSelector** - Get & Select Models

Dynamically fetch available models from the Copilot CLI, then let users pick one:

```csharp
// Interactive selection via console
var model = await ModelSelector.SelectModelAsync();

// Or get all models programmatically
var models = await ModelSelector.GetModelsFromCliAsync();

// Or get the default (first) model
var defaultModel = await ModelSelector.GetDefaultModelAsync();

// Or select by index
var model = await ModelSelector.SelectModelByIndexAsync(2);
```

### 3. **ChatHelper** - Stream & Collect Responses

Handle the most common chat patterns without repeating the same event subscription logic:

#### Stream to console with colors:
```csharp
await ChatHelper.SendMessageAndStreamResponse(session, "What is 2+2?");
```

#### Get the complete response as a string:
```csharp
var response = await ChatHelper.SendMessageAndGetResponseAsync(session, "Explain async/await");
```

#### Use custom callbacks for maximum control:
```csharp
await ChatHelper.SendMessageWithCallbacksAsync(
    session,
    "Tell me a joke",
    onDelta: text => Console.Write(text),
    onComplete: () => Console.WriteLine("\n[Done]"),
    onError: err => Console.WriteLine($"Error: {err}")
);
```

## Full Example

Here's a complete example showing all three helpers working together:

```csharp
using GitHub.Copilot.SDK;
using Refractored.GitHub.Copilot.SDK.Helpers;

// Step 1: Check prerequisites
var status = await CliChecker.CheckCopilotStatusAsync();
if (!CliChecker.IsReady(status))
    return;

// Step 2: Select a model
var model = await ModelSelector.SelectModelAsync();
if (model == null)
    return;

// Step 3: Start client and chat
using var client = new CopilotClient();
await client.StartAsync();

using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model,
    Streaming = true
});

// Step 4: Chat!
await ChatHelper.SendMessageAndStreamResponse(session, "Hello, Copilot!");
```

## Key Features

✅ **Multi-targeted** - Supports net8.0, net9.0, and net10.0  
✅ **Zero dependencies** - Only depends on `GitHub.Copilot.SDK`  
✅ **XML documented** - Full IntelliSense support  
✅ **Process-safe** - Fixed deadlock issues with process stdio handling  
✅ **Timeout-protected** - All CLI calls have 10-second timeouts  
✅ **MIT Licensed** - Free to use and modify  

## Installation

### Via NuGet.org
```bash
dotnet add package Refractored.GitHub.Copilot.SDK.Helpers
```

### Via GitHub Packages
```bash
nuget sources Add -Name github -Source https://nuget.pkg.github.com/jamesmontemagno/index.json -Username USERNAME -Password TOKEN

dotnet add package Refractored.GitHub.Copilot.SDK.Helpers -s github
```

## CI/CD Out of the Box

The repository includes:

- **CI Workflow** - Builds on every PR and push to `main`
- **Deploy Workflow** - Automatically publishes to both NuGet.org and GitHub Packages on version tag push

To release a new version:
```bash
git tag v1.1.0
git push origin v1.1.0
```

That's it! The workflow handles building, packing, and publishing to both registries.

## Try the Sample App

A complete sample app is included showing all helpers in action:

```bash
cd samples/TestApp
dotnet run
```

Features:
- ✅ Animated spinner while waiting for responses
- ✅ Prerequisites checking
- ✅ Interactive model selection
- ✅ Real-time streaming output
- ✅ Clean CLI UX

## Under the Hood: What We Fixed

### Process Deadlock Prevention
The original helpers had a subtle bug: when redirecting `StandardOutput` and `StandardError`, you must read from them or the process hangs when the buffer fills. We now:
- Read both streams concurrently
- Add a 10-second timeout to prevent indefinite hangs
- Clean up resources properly

### Timeout Protection
All CLI calls now use `CancellationTokenSource` with a 10-second timeout to ensure your app never hangs waiting for the Copilot CLI.

## Future Possibilities

Ideas for future versions:
- [ ] Support for custom streaming handlers
- [ ] Batch message support
- [ ] Rate limiting helpers
- [ ] Caching helpers for model lists
- [ ] Integration with dependency injection containers

## Get Involved

**Repo:** https://github.com/jamesmontemagno/copilot-sdk-dotnet-helpers  
**Issues:** https://github.com/jamesmontemagno/copilot-sdk-dotnet-helpers/issues  
**Discussions:** https://github.com/jamesmontemagno/copilot-sdk-dotnet-helpers/discussions  

Contributions welcome! Whether it's bug reports, feature requests, or pull requests.

## Acknowledgments

Built with love as a companion to the [hello-copilot-sdk-dotnet](https://github.com/jamesmontemagno/hello-copilot-sdk-dotnet) demo project by [James Montemagno](https://github.com/jamesmontemagno).

---

**Happy coding! 🚀**

Have you built something cool with Copilot SDK? Share it in the GitHub Discussions!

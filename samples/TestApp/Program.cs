using GitHub.Copilot.SDK;
using Refractored.GitHub.Copilot.SDK.Helpers;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       Refractored.GitHub.Copilot.SDK.Helpers - Test App      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// Step 1: Check prerequisites
Console.WriteLine("🔍 Checking prerequisites...\n");
var status = await CliChecker.CheckCopilotStatusAsync();

if (!CliChecker.IsReady(status))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Press any key to exit...");
    Console.ResetColor();
    Console.ReadKey(true);
    return;
}

// Step 2: Display available models with billing info
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("📊 Available Models & Billing Info:\n");
Console.ResetColor();

CopilotClient? client = null;
CopilotSession? session = null;

try
{
    client = new CopilotClient();
    await client.StartAsync();
    
    var modelsWithInfo = await ModelSelector.GetModelsWithInfoAsync(client);
    if (modelsWithInfo != null && modelsWithInfo.Length > 0)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("   Model                          Multiplier");
        Console.WriteLine("   ─────────────────────────────  ──────────");
        Console.ResetColor();
        
        foreach (var modelInfo in modelsWithInfo)
        {
            var multiplier = modelInfo.Billing?.Multiplier.ToString("F2") ?? "N/A";
            Console.WriteLine($"   {modelInfo.Id,-30} {multiplier,10}");
        }
        Console.WriteLine();
    }

    // Step 3: Select model
    var model = await ModelSelector.SelectModelAsync();
    if (model == null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press any key to exit...");
        Console.ResetColor();
        Console.ReadKey(true);
        return;
    }
    Console.WriteLine();

    // Step 4: Create session
    await RunWithSpinnerAsync($"Creating session with {model}", async () =>
    {
        session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = model,
            Streaming = true
        });
    });
    
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"   Session ID: {session!.SessionId}\n");
    Console.ResetColor();

    // Step 5: Interactive chat
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("💬 Interactive Chat - Type 'exit' to quit\n");
    Console.ResetColor();

    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("You: ");
        Console.ResetColor();

        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            continue;

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\n👋 Goodbye!");
            break;
        }

        // Show spinner while waiting for response
        await SendMessageWithSpinnerAsync(session, input);
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.ResetColor();
}
finally
{
    if (session != null)
        await session.DisposeAsync();
    if (client != null)
        await client.DisposeAsync();
    Console.WriteLine("\n🛑 Client stopped.");
}

/// <summary>
/// Runs an async task with a spinner animation.
/// </summary>
static async Task RunWithSpinnerAsync(string message, Func<Task> action)
{
    var spinnerChars = new[] { '|', '/', '-', '\\' };
    var cts = new CancellationTokenSource();
    
    // Hide cursor for cleaner animation
    Console.CursorVisible = false;
    Console.Write($"  {message}...");
    
    var spinnerTask = Task.Run(async () =>
    {
        int i = 0;
        var left = Console.CursorLeft;
        while (!cts.Token.IsCancellationRequested)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(spinnerChars[i++ % spinnerChars.Length]);
            try { await Task.Delay(100, cts.Token); } catch { break; }
        }
    });

    try
    {
        await action();
        cts.Cancel();
        await spinnerTask;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("✓");
        Console.ResetColor();
        Console.WriteLine();
    }
    catch
    {
        cts.Cancel();
        await spinnerTask;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("✗");
        Console.ResetColor();
        Console.WriteLine();
        throw;
    }
    finally
    {
        Console.CursorVisible = true;
    }
}

/// <summary>
/// Sends a message with a spinner while waiting for the first response.
/// </summary>
static async Task SendMessageWithSpinnerAsync(CopilotSession session, string message)
{
    var spinnerChars = new[] { '|', '/', '-', '\\' };
    var done = new TaskCompletionSource();
    var hasStartedResponse = false;
    var spinnerCts = new CancellationTokenSource();
    
    // Start spinner
    Console.CursorVisible = false;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nCopilot: ");
    Console.ResetColor();
    var spinnerLeft = Console.CursorLeft;
    var spinnerTop = Console.CursorTop;
    
    var spinnerTask = Task.Run(async () =>
    {
        int i = 0;
        while (!spinnerCts.Token.IsCancellationRequested)
        {
            Console.SetCursorPosition(spinnerLeft, spinnerTop);
            Console.Write(spinnerChars[i++ % spinnerChars.Length]);
            try { await Task.Delay(100, spinnerCts.Token); } catch { break; }
        }
    });

    var subscription = session.On(evt =>
    {
        switch (evt)
        {
            case AssistantMessageDeltaEvent delta:
                if (!hasStartedResponse)
                {
                    spinnerCts.Cancel();
                    Console.SetCursorPosition(spinnerLeft, spinnerTop);
                    Console.Write(" "); // Clear spinner
                    Console.SetCursorPosition(spinnerLeft, spinnerTop);
                    Console.CursorVisible = true;
                    hasStartedResponse = true;
                }
                Console.Write(delta.Data.DeltaContent);
                break;
                
            case AssistantMessageEvent msg:
                if (!hasStartedResponse)
                {
                    spinnerCts.Cancel();
                    Console.SetCursorPosition(spinnerLeft, spinnerTop);
                    Console.Write(" ");
                    Console.SetCursorPosition(spinnerLeft, spinnerTop);
                    Console.CursorVisible = true;
                    Console.Write(msg.Data.Content);
                }
                break;
                
            case SessionIdleEvent:
                done.TrySetResult();
                break;
                
            case SessionErrorEvent err:
                spinnerCts.Cancel();
                Console.CursorVisible = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error: {err.Data.Message}");
                Console.ResetColor();
                done.TrySetResult();
                break;
        }
    });

    try
    {
        await session.SendAsync(new MessageOptions { Prompt = message });
        await done.Task;
        spinnerCts.Cancel();
        await spinnerTask;
        Console.WriteLine("\n");
    }
    finally
    {
        spinnerCts.Cancel();
        Console.CursorVisible = true;
        subscription.Dispose();
    }
}

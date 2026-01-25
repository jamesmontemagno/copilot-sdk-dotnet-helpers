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

// Step 2: Select model
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

// Step 3: Start Copilot client
CopilotClient? client = null;
CopilotSession? session = null;

try
{
    client = new CopilotClient();
    
    await RunWithSpinnerAsync("Starting Copilot client", async () =>
    {
        await client.StartAsync();
    });

    // Create session
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

    // Step 4: Interactive chat
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

        await ChatHelper.SendMessageAndStreamResponse(session, input);
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

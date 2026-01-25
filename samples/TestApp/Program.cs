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
Console.WriteLine("🚀 Starting Copilot client...");

CopilotClient? client = null;
CopilotSession? session = null;

try
{
    client = new CopilotClient();
    await client.StartAsync();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Copilot client started!\n");
    Console.ResetColor();

    // Create session
    Console.WriteLine($"📝 Creating session with model: {model}...");
    session = await client.CreateSessionAsync(new SessionConfig
    {
        Model = model,
        Streaming = true
    });
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Session created! (ID: {session.SessionId})\n");
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

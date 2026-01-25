using GitHub.Copilot.SDK;

namespace Refractored.GitHub.Copilot.SDK.Helpers;

/// <summary>
/// Helper methods for chat interactions with Copilot.
/// </summary>
public static class ChatHelper
{
    /// <summary>
    /// Sends a message and streams the response to the console with colored output.
    /// </summary>
    /// <param name="session">The Copilot session.</param>
    /// <param name="message">The message to send.</param>
    public static async Task SendMessageAndStreamResponse(CopilotSession session, string message)
    {
        var done = new TaskCompletionSource();
        var hasStartedResponse = false;

        var subscription = session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    if (!hasStartedResponse)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\nCopilot: ");
                        Console.ResetColor();
                        hasStartedResponse = true;
                    }
                    Console.Write(delta.Data.DeltaContent);
                    break;
                    
                case AssistantMessageEvent msg:
                    if (!hasStartedResponse)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\nCopilot: ");
                        Console.ResetColor();
                        Console.Write(msg.Data.Content);
                    }
                    break;
                    
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                    
                case SessionErrorEvent err:
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
            Console.WriteLine("\n");
        }
        finally
        {
            subscription.Dispose();
        }
    }

    /// <summary>
    /// Sends a message and streams the response, invoking callbacks for each event.
    /// </summary>
    /// <param name="session">The Copilot session.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="onDelta">Callback for each content delta.</param>
    /// <param name="onComplete">Callback when response is complete (optional).</param>
    /// <param name="onError">Callback on error (optional).</param>
    public static async Task SendMessageWithCallbacksAsync(
        CopilotSession session,
        string message,
        Action<string> onDelta,
        Action? onComplete = null,
        Action<string>? onError = null)
    {
        var done = new TaskCompletionSource();

        var subscription = session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    onDelta(delta.Data.DeltaContent);
                    break;
                    
                case AssistantMessageEvent msg:
                    onDelta(msg.Data.Content);
                    break;
                    
                case SessionIdleEvent:
                    onComplete?.Invoke();
                    done.TrySetResult();
                    break;
                    
                case SessionErrorEvent err:
                    onError?.Invoke(err.Data.Message);
                    done.TrySetResult();
                    break;
            }
        });

        try
        {
            await session.SendAsync(new MessageOptions { Prompt = message });
            await done.Task;
        }
        finally
        {
            subscription.Dispose();
        }
    }

    /// <summary>
    /// Sends a message and collects the full response as a string.
    /// </summary>
    /// <param name="session">The Copilot session.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>The complete response text, or null on error.</returns>
    public static async Task<string?> SendMessageAndGetResponseAsync(CopilotSession session, string message)
    {
        var done = new TaskCompletionSource();
        var responseBuilder = new System.Text.StringBuilder();
        string? error = null;

        var subscription = session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    responseBuilder.Append(delta.Data.DeltaContent);
                    break;
                    
                case AssistantMessageEvent msg:
                    responseBuilder.Append(msg.Data.Content);
                    break;
                    
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                    
                case SessionErrorEvent err:
                    error = err.Data.Message;
                    done.TrySetResult();
                    break;
            }
        });

        try
        {
            await session.SendAsync(new MessageOptions { Prompt = message });
            await done.Task;
            
            return error == null ? responseBuilder.ToString() : null;
        }
        finally
        {
            subscription.Dispose();
        }
    }
}

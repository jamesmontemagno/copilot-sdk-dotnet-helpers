using GitHub.Copilot.SDK;

namespace Refractored.GitHub.Copilot.SDK.Helpers;

/// <summary>
/// Manages Copilot sessions including listing, resuming, and cleanup.
/// </summary>
public static class SessionManager
{
    /// <summary>
    /// Lists all available sessions with their metadata.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of SessionMetadata, or null if unavailable.</returns>
    public static async Task<List<SessionMetadata>?> GetSessionsAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                await client.StartAsync(cancellationToken);
                return await client.ListSessionsAsync(cancellationToken);
            }
            finally
            {
                if (shouldDispose)
                {
                    await client.DisposeAsync();
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Lists all sessions and displays them in the console with formatted output.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of SessionMetadata, or null if unavailable.</returns>
    public static async Task<List<SessionMetadata>?> ListSessionsAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        var sessions = await GetSessionsAsync(client, cancellationToken);
        
        if (sessions == null || sessions.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No sessions found.");
            Console.ResetColor();
            return null;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"📋 Found {sessions.Count} session(s):\n");
        Console.ResetColor();

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {i + 1}. {session.SessionId}");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"     Started: {session.StartTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"     Modified: {session.ModifiedTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"     Remote: {(session.IsRemote ? "Yes" : "No")}");
            
            if (!string.IsNullOrEmpty(session.Summary))
            {
                Console.WriteLine($"     Summary: {session.Summary}");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        return sessions;
    }

    /// <summary>
    /// Gets the ID of the most recently used session.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The session ID, or null if no sessions exist.</returns>
    public static async Task<string?> GetLastSessionIdAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                await client.StartAsync(cancellationToken);
                return await client.GetLastSessionIdAsync(cancellationToken);
            }
            finally
            {
                if (shouldDispose)
                {
                    await client.DisposeAsync();
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resumes the most recently used session.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use.</param>
    /// <param name="config">Optional configuration for the resumed session.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resumed CopilotSession, or null if no recent session exists.</returns>
    public static async Task<CopilotSession?> ResumeLastSessionAsync(CopilotClient client, ResumeSessionConfig? config = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var lastId = await client.GetLastSessionIdAsync(cancellationToken);
            if (string.IsNullOrEmpty(lastId))
                return null;

            return await client.ResumeSessionAsync(lastId, config, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes a session by its ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session to delete.</param>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    public static async Task<bool> DeleteSessionAsync(string sessionId, CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                await client.StartAsync(cancellationToken);
                await client.DeleteSessionAsync(sessionId, cancellationToken);
                return true;
            }
            finally
            {
                if (shouldDispose)
                {
                    await client.DisposeAsync();
                }
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes old sessions based on a time threshold.
    /// </summary>
    /// <param name="olderThan">Delete sessions older than this timespan.</param>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Number of sessions deleted.</returns>
    public static async Task<int> CleanupOldSessionsAsync(TimeSpan olderThan, CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        var sessions = await GetSessionsAsync(client, cancellationToken);
        if (sessions == null || sessions.Count == 0)
            return 0;

        var threshold = DateTime.UtcNow - olderThan;
        var oldSessions = sessions.Where(s => s.ModifiedTime.ToUniversalTime() < threshold).ToList();

        if (oldSessions.Count == 0)
            return 0;

        int deleted = 0;
        var shouldDispose = client == null;
        client ??= new CopilotClient();

        try
        {
            await client.StartAsync(cancellationToken);
            
            foreach (var session in oldSessions)
            {
                try
                {
                    await client.DeleteSessionAsync(session.SessionId, cancellationToken);
                    deleted++;
                }
                catch
                {
                    // Continue with other sessions even if one fails
                }
            }
        }
        finally
        {
            if (shouldDispose)
            {
                await client.DisposeAsync();
            }
        }

        return deleted;
    }

    /// <summary>
    /// Gets the Copilot CLI status including version and protocol information.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>GetStatusResponse with version info, or null if unavailable.</returns>
    public static async Task<GetStatusResponse?> GetCliStatusAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                await client.StartAsync(cancellationToken);
                return await client.GetStatusAsync(cancellationToken);
            }
            finally
            {
                if (shouldDispose)
                {
                    await client.DisposeAsync();
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the foreground session ID (only available in TUI+server mode).
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The foreground session ID, or null if unavailable.</returns>
    public static async Task<string?> GetForegroundSessionIdAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                await client.StartAsync(cancellationToken);
                return await client.GetForegroundSessionIdAsync(cancellationToken);
            }
            finally
            {
                if (shouldDispose)
                {
                    await client.DisposeAsync();
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the foreground session in TUI mode.
    /// </summary>
    /// <param name="sessionId">The session ID to bring to the foreground.</param>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static async Task<bool> SetForegroundSessionIdAsync(string sessionId, CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                await client.StartAsync(cancellationToken);
                await client.SetForegroundSessionIdAsync(sessionId, cancellationToken);
                return true;
            }
            finally
            {
                if (shouldDispose)
                {
                    await client.DisposeAsync();
                }
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Displays the CLI status information in the console.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>GetStatusResponse with version info, or null if unavailable.</returns>
    public static async Task<GetStatusResponse?> ShowCliStatusAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        var status = await GetCliStatusAsync(client, cancellationToken);
        
        if (status == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Could not retrieve CLI status.");
            Console.ResetColor();
            return null;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("ℹ️  Copilot CLI Status:");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"   Version: {status.Version}");
        Console.WriteLine($"   Protocol Version: {status.ProtocolVersion}");
        Console.ResetColor();
        Console.WriteLine();

        return status;
    }
}

using System.Diagnostics;
using GitHub.Copilot.SDK;

namespace Refractored.GitHub.Copilot.SDK.Helpers;

/// <summary>
/// Checks if the Copilot CLI is installed and the user is authenticated.
/// </summary>
public static class CliChecker
{
    /// <summary>
    /// Result of the Copilot readiness check.
    /// </summary>
    /// <param name="IsInstalled">Whether the Copilot CLI is installed.</param>
    /// <param name="IsTokenSet">Whether the GH_TOKEN environment variable is set.</param>
    /// <param name="IsAuthenticated">Whether the user is authenticated with Copilot.</param>
    /// <param name="ErrorMessage">Error message if any check failed.</param>
    public record CopilotStatus(
        bool IsInstalled,
        bool IsTokenSet,
        bool IsAuthenticated,
        string? ErrorMessage);

    /// <summary>
    /// Verifies Copilot CLI installation and authentication status.
    /// Outputs status messages to the console.
    /// </summary>
    /// <returns>CopilotStatus with detailed information.</returns>
    public static async Task<CopilotStatus> CheckCopilotStatusAsync()
    {
        Console.Write("   Checking for Copilot CLI... ");
        
        var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        var isTokenSet = !string.IsNullOrEmpty(ghToken);
        
        var isInstalled = await CheckCopilotInstalledAsync();
        
        if (!isInstalled)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Not found!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("   Please install Copilot CLI:");
            Console.WriteLine("   macOS/Linux: brew install copilot-cli");
            Console.WriteLine("   Windows:     winget install GitHub.Copilot");
            Console.WriteLine("   npm:         npm install -g @github/copilot");
            Console.WriteLine("   Script:      curl -fsSL https://gh.io/copilot-install | bash");
            
            return new CopilotStatus(
                IsInstalled: false,
                IsTokenSet: isTokenSet,
                IsAuthenticated: false,
                ErrorMessage: "Copilot CLI is not installed.");
        }
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Installed");
        Console.ResetColor();
        
        Console.Write("   Checking GH_TOKEN environment variable... ");
        if (isTokenSet)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Set");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("○ Not set (will use interactive auth)");
            Console.ResetColor();
        }
        
        Console.Write("   Checking authentication... ");
        var authResult = await CheckCopilotAuthAsync();
        
        if (authResult.isAuthenticated)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Authenticated");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Not authenticated");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("   Please authenticate with Copilot:");
            Console.WriteLine("   Run: copilot");
            Console.WriteLine("   Then type: /login");
            Console.WriteLine("   Or set GH_TOKEN environment variable with a token that has 'Copilot Requests' permission.");
        }
        
        Console.WriteLine();
        
        return new CopilotStatus(
            IsInstalled: true,
            IsTokenSet: isTokenSet,
            IsAuthenticated: authResult.isAuthenticated,
            ErrorMessage: authResult.error);
    }

    /// <summary>
    /// Verifies Copilot CLI installation and authentication status without console output.
    /// </summary>
    /// <returns>CopilotStatus with detailed information.</returns>
    public static async Task<CopilotStatus> CheckCopilotStatusSilentAsync()
    {
        var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        var isTokenSet = !string.IsNullOrEmpty(ghToken);
        
        var isInstalled = await CheckCopilotInstalledAsync();
        
        if (!isInstalled)
        {
            return new CopilotStatus(
                IsInstalled: false,
                IsTokenSet: isTokenSet,
                IsAuthenticated: false,
                ErrorMessage: "Copilot CLI is not installed.");
        }
        
        var authResult = await CheckCopilotAuthAsync();
        
        return new CopilotStatus(
            IsInstalled: true,
            IsTokenSet: isTokenSet,
            IsAuthenticated: authResult.isAuthenticated,
            ErrorMessage: authResult.error);
    }

    /// <summary>
    /// Quick check if Copilot is ready (installed + authenticated or has token).
    /// </summary>
    /// <param name="status">The CopilotStatus to check.</param>
    /// <returns>True if Copilot is ready to use.</returns>
    public static bool IsReady(CopilotStatus status)
    {
        return status.IsInstalled && (status.IsTokenSet || status.IsAuthenticated);
    }

    /// <summary>
    /// Checks if the Copilot CLI is installed.
    /// </summary>
    /// <returns>True if installed, false otherwise.</returns>
    public static async Task<bool> CheckCopilotInstalledAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "copilot",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();
            
            // Must read streams to avoid deadlock when buffer fills
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                return false;
            }
            
            await Task.WhenAll(outputTask, errorTask);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(bool isAuthenticated, string? error)> CheckCopilotAuthAsync()
    {
        try
        {
            await using var client = new CopilotClient();
            var authStatus = await client.GetAuthStatusAsync();
            
            // Check if properly authenticated - user should have a login name
            var isAuthenticated = !string.IsNullOrEmpty(authStatus.Login);
            return (isAuthenticated, authStatus.StatusMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

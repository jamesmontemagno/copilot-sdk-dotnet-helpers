using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Refractored.GitHub.Copilot.SDK.Helpers;

/// <summary>
/// Model metadata including pricing tier information.
/// </summary>
public record ModelInfo(string Name, string PricingTier);

/// <summary>
/// Handles model selection for Copilot sessions.
/// Fetches available models directly from the Copilot CLI.
/// </summary>
public static partial class ModelSelector
{
    [GeneratedRegex("\"([^\"]+)\"")]
    private static partial Regex QuotedModelRegex();

    /// <summary>
    /// Pricing tier mapping for available models.
    /// 1x = Standard pricing
    /// 0.33x = Discounted pricing (e.g., haiku/mini models)
    /// 0x = Free tier (if available)
    /// </summary>
    private static readonly Dictionary<string, string> ModelPricingTiers = new()
    {
        // Claude models
        { "claude-opus-4.5", "1x" },
        { "claude-sonnet-4.5", "1x" },
        { "claude-sonnet-4", "1x" },
        { "claude-haiku-4.5", "0.33x" },
        
        // GPT-5 models
        { "gpt-5.2-codex", "1x" },
        { "gpt-5.2", "1x" },
        { "gpt-5.1-codex-max", "1x" },
        { "gpt-5.1-codex", "1x" },
        { "gpt-5.1", "1x" },
        { "gpt-5", "1x" },
        { "gpt-5.1-codex-mini", "0.33x" },
        { "gpt-5-mini", "0.33x" },
        { "gpt-4.1", "0.33x" },
        
        // Gemini models
        { "gemini-3-pro-preview", "1x" },
    };

    /// <summary>
    /// Fetches the list of available models from the Copilot CLI as strings.
    /// </summary>
    /// <returns>Array of model names, or null if unavailable.</returns>
    public static async Task<string[]?> GetModelsFromCliAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "copilot",
                Arguments = "--help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();
            
            // Read both streams to avoid deadlock
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
                return null;
            }
            
            var output = await outputTask;
            await errorTask;
            
            if (process.ExitCode != 0)
                return null;
            
            var modelIndex = output.IndexOf("--model", StringComparison.OrdinalIgnoreCase);
            if (modelIndex < 0)
                return null;
            
            var choicesIndex = output.IndexOf("choices:", modelIndex, StringComparison.OrdinalIgnoreCase);
            if (choicesIndex < 0)
                return null;
            
            var endIndex = output.IndexOf("\n  --", choicesIndex + 1);
            if (endIndex < 0)
                endIndex = output.Length;
            
            var choicesSection = output[choicesIndex..endIndex];
            
            var matches = QuotedModelRegex().Matches(choicesSection);
            var models = matches.Select(m => m.Groups[1].Value).ToArray();
            
            return models.Length > 0 ? models : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches available models with pricing information as a dictionary.
    /// Pricing information is returned from the CLI if available.
    /// </summary>
    /// <returns>Dictionary mapping model names to their pricing modifiers, or null if models unavailable.</returns>
    public static async Task<Dictionary<string, string>?> GetModelsWithPricingAsync()
    {
        var models = await GetModelsFromCliAsync();
        
        if (models == null || models.Length == 0)
            return null;
        
        var result = new Dictionary<string, string>();
        foreach (var model in models)
        {
            result[model] = "unknown";
        }
        
        return result;
    }

    /// <summary>
    /// Prompts the user to select a model from available options via console, showing pricing tiers.
    /// </summary>
    /// <returns>The selected model ID, or null if no models available.</returns>
    public static async Task<string?> SelectModelAsync()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("   Fetching available models from Copilot CLI...");
        Console.ResetColor();
        
        var models = await GetModelsFromCliAsync();
        
        if (models == null || models.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Could not fetch models from Copilot CLI.");
            Console.WriteLine("   Make sure 'copilot' is installed and working.");
            Console.ResetColor();
            return null;
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🤖 Select a model:");
        Console.ResetColor();
        
        for (int i = 0; i < models.Length; i++)
        {
            Console.WriteLine($"   {i + 1}. {models[i]}");
        }
        
        Console.Write($"\nEnter choice (1-{models.Length}) [default: 1]: ");
        var choice = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(choice) || !int.TryParse(choice, out int index) || index < 1 || index > models.Length)
        {
            index = 1;
        }

        var selected = models[index - 1];
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Selected: {selected}");
        Console.ResetColor();
        
        return selected;
    }

    /// <summary>
    /// Selects a model by index from the available models (0-based).
    /// </summary>
    /// <param name="index">Zero-based index of the model to select.</param>
    /// <returns>The selected model ID, or null if index is invalid or models unavailable.</returns>
    public static async Task<string?> SelectModelByIndexAsync(int index)
    {
        var models = await GetModelsFromCliAsync();
        
        if (models == null || models.Length == 0 || index < 0 || index >= models.Length)
            return null;
        
        return models[index];
    }

    /// <summary>
    /// Gets the first available model.
    /// </summary>
    /// <returns>The first model ID, or null if no models available.</returns>
    public static async Task<string?> GetDefaultModelAsync()
    {
        return await SelectModelByIndexAsync(0);
    }
}

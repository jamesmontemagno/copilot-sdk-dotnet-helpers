using GitHub.Copilot.SDK;

namespace Refractored.GitHub.Copilot.SDK.Helpers;

/// <summary>
/// Handles model selection for Copilot sessions.
/// Fetches available models from the Copilot SDK client.
/// </summary>
public static class ModelSelector
{
    /// <summary>
    /// Fetches the list of available models with full information from the Copilot SDK.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Array of ModelInfo containing model details including ID and multiplier, or null if unavailable.</returns>
    public static async Task<ModelInfo[]?> GetModelsWithInfoAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                var models = await client.ListModelsAsync(cancellationToken);
                return models?.ToArray();
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
    /// Fetches the list of available models from the Copilot SDK.
    /// </summary>
    /// <param name="client">The CopilotClient instance to use. If null, a new client will be created and disposed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Array of model names, or null if unavailable.</returns>
    public static async Task<string[]?> GetModelsAsync(CopilotClient? client = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var shouldDispose = client == null;
            client ??= new CopilotClient();
            
            try
            {
                var models = await client.ListModelsAsync(cancellationToken);
                return models?.Select(m => m.Id).ToArray();
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
    /// Prompts the user to select a model from available options via console,
    /// displaying model details including reasoning support.
    /// </summary>
    /// <param name="showDetails">Whether to show billing multiplier and reasoning support (default: true).</param>
    /// <returns>The selected model ID, or null if no models available.</returns>
    public static async Task<string?> SelectModelAsync(bool showDetails = true)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("   Fetching available models from Copilot SDK...");
        Console.ResetColor();
        
        var modelsWithInfo = showDetails ? await GetModelsWithInfoAsync() : null;
        var models = modelsWithInfo?.Select(m => m.Id).ToArray() ?? await GetModelsAsync();
        
        if (models == null || models.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Could not fetch models from Copilot SDK.");
            Console.WriteLine("   Make sure the Copilot CLI is installed and authenticated.");
            Console.ResetColor();
            return null;
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🤖 Select a model:");
        Console.ResetColor();
        
        for (int i = 0; i < models.Length; i++)
        {
            var extras = "";
            if (modelsWithInfo != null && i < modelsWithInfo.Length)
            {
                var info = modelsWithInfo[i];
                var parts = new List<string>();
                if (info.Billing?.Multiplier is { } mult)
                    parts.Add($"{mult:F1}x");
                if (info.SupportedReasoningEfforts is { Count: > 0 })
                    parts.Add("reasoning");
                if (parts.Count > 0)
                    extras = $" ({string.Join(", ", parts)})";
            }
            Console.WriteLine($"   {i + 1}. {models[i]}{extras}");
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
        var models = await GetModelsAsync();
        
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

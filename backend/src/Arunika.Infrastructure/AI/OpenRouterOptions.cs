namespace Arunika.Infrastructure.AI;

public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string Model { get; set; } = "openrouter/free";
    public List<string> FallbackModels { get; set; } =
    [
        "qwen/qwen3-coder:free",
        "nvidia/nemotron-3-ultra-550b-a55b:free",
        "google/gemma-4-31b-it:free",
        "poolside/laguna-m.1:free",
    ];
}

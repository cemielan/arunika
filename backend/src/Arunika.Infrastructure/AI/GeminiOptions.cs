namespace Arunika.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public List<string> FallbackModels { get; set; } =
    [
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash",
        "gemini-2.5-flash-lite",
        "gemini-3-flash",
        "gemini-3.5-flash",
        "gemini-3.6-flash",
    ];
}

namespace dme_workflow_parser;

/// <summary>
/// External API configuration setting.
/// </summary>
/// <param name="Endpoint">The URL to the external API's endpoint.</param>
public record ExternalApiSettings(string Endpoint);

/// <summary>
/// Configuration settings for the DME workflow parser application.
/// </summary>
public class Settings
{
    public string InputFolder { get; init; } = string.Empty;
    public string OutputFolder { get; init; } = string.Empty;
    public string TextInputFile { get; init; } = string.Empty;
    public string JsonInputFile { get; init; } = string.Empty;
    public string OutputFile { get; init; } = "output.json";
    public ExternalApiSettings ExternalApi { get; init; } = new("https://alert-api.com/DrExtract");
}

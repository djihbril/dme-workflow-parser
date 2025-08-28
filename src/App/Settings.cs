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
    public string InputFolder { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
    public string TextInputFile { get; set; } = string.Empty;
    public string JsonInputFile { get; set; } = string.Empty;
    public string OutputFile { get; set; } = "output.json";
    public ExternalApiSettings ExternalApi { get; set; } = new("https://alert-api.com/DrExtract");
}

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace dme_workflow_parser.Services;

public interface INoteSender
{
    Task<bool> PostOrderAsync(string url, string orderJson);
}

/// <summary>
/// Structured physician notes sending service.
/// </summary>
/// <param name="settings">Dependency-injected application configuration settings.</param>
/// <param name="httpClients">Dependency-injected http clients.</param>
/// <param name="logger">Dependency-injected logger.</param>
public class NoteSender(Settings settings, IHttpClientFactory httpClients, ILogger<NoteSender> logger) : INoteSender
{
    /// <summary>
    /// JSON serialization options to be used globally.
    /// </summary>
    public static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

/// <summary>
/// POSTs the given order's JSON to the specified URL.
/// </summary>
/// <param name="url">External API endpoint.</param>
/// <param name="orderJson">JSON-serialized DME order.</param>
/// <returns>A flag indicating wether the request succeeded.</returns>
    public async Task<bool> PostOrderAsync(string url, string orderJson)
    {
        logger.LogInformation("POSTing order '{Order}' to URL: '{Url}'", orderJson, url);
        HttpClient client = httpClients.CreateClient(settings.HttpClientKey);
        StringContent httpContent = new(orderJson, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, httpContent);
            logger.LogInformation("Order POSTed with response status code: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Error response content: {ResponseContent}", responseContent);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error POSTing order '{Order}' to URL '{Url}'.", orderJson, url);

            return false;
        }
    }
}
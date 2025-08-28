/// <summary>
/// Extracts patient info and their DME needs data from physician notes.
/// POSTs the extracted data as JSON to an external API endpoint.
/// </summary>

using System.Text.Json;
using dme_workflow_parser;
using dme_workflow_parser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Load and register configuration settings along with other services.
var config = ConfigurationLoader.Load();
Settings? settings = config.GetSection("Settings").Get<Settings>();

ServiceCollection services = new();

services
    .AddSingleton(settings ?? new())
    .AddHttpClient(settings?.httpClientKey ?? "externalEndpoint");


ServiceProvider serviceProvider = services
    .AddTransient<NoteParser>()
    .AddSingleton<INoteSender, NoteSender>()
    // .AddLogging(builder => builder.AddConsole())
    .BuildServiceProvider();

// var logger = services.GetRequiredService<ILogger<Program>>();
// logger.LogInformation("Workflow parser starting with {Threads} threads", settings.MaxThreads);

NoteParser parser = serviceProvider.GetRequiredService<NoteParser>();
NoteSender sender = (NoteSender)serviceProvider.GetRequiredService<INoteSender>();

var (errorMsg, order) = parser.Parse(true);

if (errorMsg != null) return;

string serializedOrder = JsonSerializer.Serialize(order, NoteSender.jsonSerializerOptions);

if (settings != null)
{
    string outputPath = Path.Combine(settings.OutputFolder, settings.OutputFile);
    File.WriteAllText(outputPath, serializedOrder);
}

bool sent = await sender.PostOrderAsync(settings?.ExternalApi.Endpoint ?? "", serializedOrder);

Console.WriteLine($"DME order{(sent ? string.Empty : " not")} sent.");

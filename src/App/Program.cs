/// <summary>
/// Extracts patient info and their DME needs data from physician notes.
/// POSTs the extracted data as JSON to an external API endpoint.
/// </summary>

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using dme_workflow_parser;
using dme_workflow_parser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

// Load and register configuration settings along with other services.
var config = ConfigurationLoader.Load();
Settings? settings = config.GetSection("Settings").Get<Settings>();

ServiceCollection services = new();

ServiceProvider serviceProvider = services
    .AddSingleton(settings ?? new())
    .AddTransient<NoteParser>()
    // .AddLogging(builder => builder.AddConsole())
    .BuildServiceProvider();

// var logger = services.GetRequiredService<ILogger<Program>>();
// logger.LogInformation("Workflow parser starting with {Threads} threads", settings.MaxThreads);

NoteParser parser = serviceProvider.GetRequiredService<NoteParser>();
var (errorMsg, order) = parser.Parse(true);

if (errorMsg != null) return;

string serializedOrder = JsonSerializer.Serialize(order,
    new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });

if (settings != null)
{
    string outputPath = Path.Combine(settings.OutputFolder, settings.OutputFile);
    File.WriteAllText(outputPath, serializedOrder);
}

using (var h = new HttpClient())
{
    var u = "https://alert-api.com/DrExtract";
    var c = new StringContent(serializedOrder, Encoding.UTF8, "application/json");
    var resp = h.PostAsync(u, c).GetAwaiter().GetResult();
}

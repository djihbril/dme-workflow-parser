/// <summary>
/// Extracts patient info and their DME needs data from physician notes.
/// POSTs the extracted data as JSON to an external API endpoint.
/// </summary>

using System.Text.Json;
using dme_workflow_parser;
using dme_workflow_parser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.CallerInfo;
using Serilog.Templates;

// A variable to hold any error that occurs during log directory creation.
string? logDirectoryCreationError = null;

// Load configuration settings.
var config = ConfigurationLoader.Load();
Settings? settings = config.GetSection("Settings").Get<Settings>();

ServiceCollection builder = new();

bool hasLogFilePath = settings != null && !string.IsNullOrWhiteSpace(settings.LogFilePath);

// Configure and register 'Serilog' only if a log file path is provided. Otherwise, use default logging.
if (hasLogFilePath)
{
    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settings!.LogFilePath) ?? string.Empty);
    }
    catch (Exception ex)
    {
        // If we could not create the log directory, use default logging and save this exception to log later when the logger is resolved.
        logDirectoryCreationError =
            $"Error creating log directory for path '{settings!.LogFilePath}. Using default console logging.'.\n{ex.Message}\n{ex.StackTrace}";
        builder.AddLogging(builder => builder.AddConsole());
    }

    if (logDirectoryCreationError == null)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithCallerInfo(
                includeFileInfo: true,
                assemblyPrefix: "App",
                filePathDepth: 3
            )
            .MinimumLevel.Debug()
            .WriteTo.File(
                // Using Serilog.Expressions for more advanced formatting features like conditional expressions.
                // outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}.{Method}({LineNumber}): {Message:lj}{NewLine}{Exception}")
                new ExpressionTemplate("{@t:yyyy-MM-dd HH:mm:ss.fff zzz} [{@l:u3}] {SourceContext}{#if Method is not null}.{Method}({LineNumber}){#end}: {@m:lj}\n{@x}"),
                path: settings.LogFilePath,
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
    }
}
else
{
    builder.AddLogging(builder => builder.AddConsole());
}

// Register remaining services.
builder
    .AddSingleton(settings ?? new())
    .AddTransient<NoteParser>()
    .AddSingleton<INoteSender, NoteSender>()
    // Call AddHttpClient last because it returns an IHttpClientBuilder
    // which does not have the other service registration methods.
    .AddHttpClient(settings?.HttpClientKey ?? "externalEndpoint");

// Build the service provider.
ServiceProvider services = builder.BuildServiceProvider();

// Resolve logger service.
var logger = services.GetRequiredService<ILogger<Program>>();

if (!hasLogFilePath)
{
    logger.LogWarning("No log file path provided in settings. Using default console logging.");
}
else if (logDirectoryCreationError != null)
{
    logger.LogWarning("{Error}", logDirectoryCreationError);
}

logger.LogInformation("Application started.");

logger.LogInformation("Resolving services...");
NoteParser parser = services.GetRequiredService<NoteParser>();
NoteSender sender = (NoteSender)services.GetRequiredService<INoteSender>();
logger.LogInformation("Services resolved.");

logger.LogInformation("Parsing notes...");
var (errorMsg, order) = parser.Parse(true);

if (errorMsg != null)
{
logger.LogInformation("Notes not parsed.");

return;
}
logger.LogInformation("Notes parsed.");

string? serializedOrder = null;

logger.LogInformation("Serializing order to JSON...");
try
{
    serializedOrder = JsonSerializer.Serialize(order, NoteSender.jsonSerializerOptions);
    logger.LogInformation("Order serialized to JSON.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error serializing order to JSON.");

    return;
}

if (serializedOrder != null && settings != null)
{
    logger.LogInformation("Writing serialized order to output file '{OutputFile}' from folder '{OutputFolder}'...",
        settings.OutputFile, settings.OutputFolder);
    string outputPath = Path.Combine(settings.OutputFolder, settings.OutputFile);
    bool hasOutputFolder = false;

    try
    {
        Directory.CreateDirectory(settings.OutputFolder);
        hasOutputFolder = true;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error ensuring output folder '{OutputFolder}' exists. Serialized order not written.", settings.OutputFolder);
    }

    if (hasOutputFolder)
    {
        try
        {
            File.WriteAllText(outputPath, serializedOrder);
            logger.LogInformation("Serialized order written to '{OutputPath}'.", outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing serialized order to '{OutputPath}'.", outputPath);
        }
    }
}

if (serializedOrder != null)
{
    logger.LogInformation("Sending DME order to external API endpoint '{Endpoint}'...", settings?.ExternalApi.Endpoint ?? "");
    bool sent = await sender.PostOrderAsync(settings?.ExternalApi.Endpoint ?? "", serializedOrder);
    logger.LogInformation("DME order{Sent} sent.", sent ? string.Empty : " not");
}
else
{
    logger.LogWarning("Serialized order not found. Not sending to external API endpoint.");
}

logger.LogInformation("Application ended.\n\n");

Log.CloseAndFlush();

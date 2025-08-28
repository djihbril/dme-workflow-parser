namespace dme_workflow_parser;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Loads configuration settings from appsettings.json.
/// </summary>
public static class ConfigurationLoader
{
    public static IConfigurationRoot Load()
    {
        var basePath = AppContext.BaseDirectory;

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
    }
}

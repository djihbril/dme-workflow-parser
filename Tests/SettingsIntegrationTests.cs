using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace dme_workflow_parser.Tests;

/// <summary>
/// A base class for tests that behave like loading settings.
/// </summary>
public abstract class BehavesLikeLoadingSettingsFromAppSettingsJson
{
    protected readonly IConfigurationRoot configuration;
    protected ServiceProvider services;

    // Override this in derived classes to provide the appsettings.json file path dynamically.
    protected virtual string ConfigDir => string.Empty;

    protected Settings? Settings => services.GetRequiredService<Settings>();

    protected BehavesLikeLoadingSettingsFromAppSettingsJson()
    {
        // Mimic settings loading and registration logic from scr/App/Program.cs.
        configuration =
        // The following logic is from ConfigurationLoader.Load(). We want to be able to override the path.
        new ConfigurationBuilder()
            .SetBasePath(ConfigDir ?? string.Empty)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        Settings? settings = configuration.GetSection("Settings").Get<Settings>();

        ServiceCollection serviceCol = new();
        services = serviceCol.AddSingleton(settings ?? new())
            .BuildServiceProvider();
    }
}

[Trait("Category", "Settings loading")]
[Trait("Scenario", "Loading settings from appsettings.json file")]
public class WhenLoadingSettingsFromAppSettingsJson : BehavesLikeLoadingSettingsFromAppSettingsJson
{
    protected override string ConfigDir => AppContext.BaseDirectory;

    [Fact]
    public void ShouldHaveAppSettingsJsonFile()
    {
        string configPath = Path.Combine(ConfigDir, "appsettings.json");
        Assert.True(File.Exists(configPath), $"Missing config file: {configPath}");
    }

    [Fact]
    public void ShouldFillSettingsWithDataFromAppsettingsJson()
    {
        Assert.NotNull(Settings);
        Assert.Equal("C:\\projects\\dotnet\\dme-workflow-parser\\Input", Settings.InputFolder);
        Assert.Equal("C:\\projects\\dotnet\\dme-workflow-parser\\Output", Settings.OutputFolder);
        Assert.Equal("physician_note.txt", Settings.TextInputFile);
        Assert.Equal("physician_note.json", Settings.JsonInputFile);
        Assert.Equal("output.json", Settings.OutputFile);
        Assert.NotNull(Settings.ExternalApi);
        Assert.Equal("https://alert-api.com/DrExtract", Settings.ExternalApi.Endpoint);
        Assert.Equal("externalEndpoint", Settings.HttpClientKey);
    }
}

[Trait("Category", "Settings loading")]
[Trait("Scenario", "Loading settings with no appsettings.json file")]
public class WhenLoadingSettingsWithNoAppsettingsJson : BehavesLikeLoadingSettingsFromAppSettingsJson
{
    protected override string ConfigDir => Directory.GetParent(AppContext.BaseDirectory)?.Parent?.FullName ?? string.Empty;

    [Fact]
    public void ShouldNotHaveAppSettingsJsonFile()
    {
        string configPath = Path.Combine(ConfigDir, "appsettings.json");
        Assert.False(File.Exists(configPath), $"Config file exists: {configPath}");
    }

    [Fact]
    public void ShouldHaveSettingsWithOnlyDefaultValues()
    {
        Assert.NotNull(Settings);
        Assert.Empty(Settings.InputFolder);
        Assert.Empty(Settings.OutputFolder);
        Assert.Empty(Settings.TextInputFile);
        Assert.Empty(Settings.JsonInputFile);
        Assert.Equal("output.json", Settings.OutputFile);
        Assert.NotNull(Settings.ExternalApi);
        Assert.Equal("https://alert-api.com/DrExtract", Settings.ExternalApi.Endpoint);
        Assert.Equal("externalEndpoint", Settings.HttpClientKey);
    }
}

using Microsoft.Extensions.Configuration;

namespace dme_workflow_parser.Tests;

    /// <summary>
    /// A base class for tests that behave like loading settings.
    /// </summary>
    public abstract class BehavesLikeLoadingSettings
    {
        protected readonly IConfigurationRoot configuration;

        // Override this in derived classes to provide settings data dynamically.
        protected virtual Dictionary<string, string?>? InMemorySettings => null;

        protected Settings? Settings => configuration.GetSection("Settings").Get<Settings>();

        protected BehavesLikeLoadingSettings()
        {
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(InMemorySettings)
                .Build();
        }
    }

    [Trait("Category", "Settings loading")]
    [Trait("Scenario", "Loading settings with data")]
    public class WhenLoadingSettingsWithData : BehavesLikeLoadingSettings
    {
        protected override Dictionary<string, string?>? InMemorySettings => new()
        {
            {"Settings:InputFolder", "C:\\projects\\dotnet\\dme-workflow-parser\\Input"},
            {"Settings:OutputFolder", "C:\\projects\\dotnet\\dme-workflow-parser\\Output"},
            {"Settings:TextInputFile", "physician_note.txt"},
            {"Settings:JsonInputFile", "physician_note.json"},
            {"Settings:OutputFile", "output_1.json"},
            {"Settings:ExternalApi:Endpoint", "https://alert-api.com/DrExtract_1"}
        };

        [Fact]
        public void ShouldFillSettingsWithData()
        {
            Assert.NotNull(Settings);
        Assert.Equal("C:\\projects\\dotnet\\dme-workflow-parser\\Input", Settings.InputFolder);
        Assert.Equal("C:\\projects\\dotnet\\dme-workflow-parser\\Output", Settings.OutputFolder);
        Assert.Equal("physician_note.txt", Settings.TextInputFile);
            Assert.Equal("physician_note.json", Settings.JsonInputFile);
            Assert.Equal("output_1.json", Settings.OutputFile);
            Assert.NotNull(Settings.ExternalApi);
            Assert.Equal("https://alert-api.com/DrExtract_1", Settings.ExternalApi.Endpoint);
        }
    }

    [Trait("Category", "Settings loading")]
    [Trait("Scenario", "Loading settings without data")]
    public class WhenLoadingSettingsWithoutData : BehavesLikeLoadingSettings
    {
        [Fact]
        public void ShouldNotHaveSettings()
        {
            Assert.Null(Settings);
        }
    }

using System.Text.Json;
using System.Text.RegularExpressions;
using dme_workflow_parser.Models;
using Microsoft.Extensions.Logging;

namespace dme_workflow_parser.Services;

/// <summary>
/// Model for deserializing JSON physician notes from input file.
/// </summary>
public class JsonContent
{
    public string? Data { get; set; }
}

/// <summary>
/// Physician notes parsing service.
/// </summary>
/// <param name="settings">Dependency-injected application configuration settings.</param>
/// <param name="logger">Dependency-injected logger.</param>
public partial class NoteParser(Settings settings, ILogger<NoteParser> logger)
{
    readonly List<string> maskTypes = ["full face", "nasal"];

    // Note: the order of items in this list matters. More specific items should come first.
    readonly List<string> addOns = ["heated tubing", "heated humidifier", "tubing", "humidifier"];

    // Note: the order of items in this list matters. More specific items should come first.
    readonly List<string> usageTypes = ["sleep and exertion", "sleep", "exertion", "movement"];

    readonly List<string> qualifiers = ["ahi > 20", "ahi < 20", "ahi = 20"];
    readonly List<string> devices = ["cpap", "oxygen tank", "wheelchair", "rollator", "crutches", "bipap", "nebulizer"];

    /// <summary>
    /// Parses physician notes to extract and return DME orders.
    /// In order to extract info more efficiently, We'll make a few assumptions based on how the notes text is formatted:
    /// - Each piece of information is on its own line, starting with a label (e.g., "Patient Name:", "DOB:", "Diagnosis:", etc.).
    /// - The prescription (or recommendation) line however may contain multiple pieces of information in a single line.
    /// - The usage line seems to be a phrase that may contain a keyword or key-phrase for a single piece of information, so it warrants a "contains" search.
    /// So our approach will be to break down each line into key/value pairs and assign the values 1 to 1 with the Order properties, except for prescription.
    /// There, we will attempt to extract multiple pieces of information by checking if certain keywords exist in the line.
    /// </summary>
    /// <returns>A named tuple with an order, if parsing was successful or and error message if not.</returns>
    public (string? ErrorMsg, Order? order) Parse(bool fromJson = false)
    {
        logger.LogInformation("Parsing {Format} notes '{InputFile}' from folder '{InputFolder}'...",
            fromJson ? "JSON" : "text", fromJson ? settings.JsonInputFile : settings.TextInputFile, settings.InputFolder);

        string notePath = Path.Combine(settings.InputFolder, fromJson ? settings.JsonInputFile : settings.TextInputFile);

        if (!File.Exists(notePath))
        {
            string err = $"Input file not found: '{notePath}'.";
            logger.LogError("{Error}", err);

            return (err, null);
        }

        List<string> notes = [];

        try
        {
            if (fromJson)
            {
                string jsonContent = File.ReadAllText(notePath);

                logger.LogInformation("Deserializing JSON content from '{File}'...", notePath);
                // We'll assume that the notes are in a property called "data".
                JsonContent? content = JsonSerializer.Deserialize<JsonContent>(jsonContent, NoteSender.jsonSerializerOptions);
                logger.LogInformation("JSON content deserialized.");

                logger.LogInformation("Extracting notes from JSON content...");
                content?.Data
                    ?.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                    ?.ToList()
                    ?.ForEach(notes.Add);
                logger.LogInformation("Notes extracted from JSON content.");
            }
            else
            {
                logger.LogInformation("Reading text notes from '{File}'...", notePath);
                notes.AddRange(File.ReadAllLines(notePath));
                logger.LogInformation("Text notes read.");
            }
        }
        catch (Exception ex)
        {
            string err = $"Error reading input text file '{notePath}'";
            logger.LogError(ex, "{Error}.", err);

            return ($"{err}: {ex.Message}", null);
        }

        Order order = new();

        logger.LogInformation("Extracting order information from notes...");
        notes.ForEach(note =>
        {
            string[] pair = note.Trim().Split(':', 2);

            // Log and skip lines that don't have a key/value format.
            if (pair.Length != 2)
            {
                logger.LogWarning("Skipping malformed line: {Line}.", note);

                return;
            }

            switch (pair[0].Trim().ToLower())
            {
                case "ahi":
                    logger.LogInformation("Found qualifier 'ahi = {Ahi}'.", pair[1].Trim());
                    order.Qualifier = $"ahi = {pair[1].Trim()}";
                    break;

                case "patient name":
                    logger.LogInformation("Found patient name '{Name}'.", pair[1].Trim());
                    order.PatientName = pair[1].Trim();
                    break;

                case "dob":
                    logger.LogInformation("Found patient date of birth '{Dob}'.", pair[1].Trim());
                    order.PatientDob = pair[1].Trim();
                    break;

                case "diagnosis":
                    logger.LogInformation("Found diagnosis '{Diagnosis}'.", pair[1].Trim());
                    order.Diagnosis = pair[1].Trim();
                    break;

                case "ordering physician":
                    logger.LogInformation("Found ordering provider '{Provider}'.", pair[1].Trim());
                    order.OrderingProvider = pair[1].Trim();
                    break;

                case "usage":
                    logger.LogInformation("Found usage '{Usage}'.", pair[1].Trim());
                    string use = pair[1].Trim();
                    string? usage = usageTypes.FirstOrDefault(u => use.Contains(u, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(usage))
                    {
                        order.Usage = usage;
                    }
                    break;

                case "prescription":
                case "recommendation":
                    logger.LogInformation("Found prescription/recommendation '{Presc}'.", pair[1].Trim());
                    string presc = pair[1].Trim();

                    // Device.
                    string? device = devices.FirstOrDefault(d => presc.Contains(d, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(device))
                    {
                        logger.LogInformation("Identified device '{Device}' from prescription.", device);
                        order.Device = device;
                    }

                    // Mask Type.
                    string? mask = maskTypes.FirstOrDefault(m => presc.Contains(m, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(mask))
                    {
                        logger.LogInformation("Identified mask type '{Mask}' from prescription.", mask);
                        order.MaskType = mask;
                    }

                    // Add-Ons.
                    List<string> addons = [.. addOns.Where(a => presc.Contains(a, StringComparison.OrdinalIgnoreCase))];

                    if (addons.Count != 0)
                    {
                        // Remove any add-ons that are substrings of other add-ons already in the list.
                        // E.g., if "heated humidifier" is in the list, remove "humidifier".
                        List<string>? filtered = [.. addons.Where(s => !addons.Any(other => other != s && other.Contains(s)))];

                        logger.LogInformation("Identified add-ons '{AddOns}' from prescription.", string.Join(", ", filtered));
                        order.AddOns = filtered;
                    }

                    // Qualifier.
                    string? qual = qualifiers.FirstOrDefault(q => presc.Contains(q, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(qual))
                    {
                        logger.LogInformation("Identified qualifier '{Qualifier}' from prescription.", qual);
                        order.Qualifier = qual;
                    }

                    // Liters (specific to Oxygen Tank).
                    if (!string.IsNullOrWhiteSpace(order.Device) && order.Device.Equals("Oxygen Tank", StringComparison.OrdinalIgnoreCase))
                    {
                        Match literValueMatch = LiterPattern().Match(presc);

                        if (literValueMatch.Success)
                        {
                            logger.LogInformation("Identified liter value '{Liters}' from prescription.", literValueMatch.Groups[1].Value);
                            order.Liters = $"{literValueMatch.Groups[1].Value} L";
                        }
                    }
                    break;
                default:
                    logger.LogWarning("Unrecognized note label '{Label}'.", pair[0]);
                    break;
            }
        });
        logger.LogInformation("Order information extracted from notes.");

        logger.LogInformation("Finished Parsing {Format} notes.", fromJson ? "JSON" : "text");

        return (null, order);
    }

    /// <summary>
    /// Why use "GeneratedRegexAttribute"?
    /// Summary from Copilot: the "GeneratedRegexAttribute" is a feature in .NET (introduced in .NET7) that allows you to define a regular expression as a source generator.
    ///     This means that the regex is compiled at build time, rather than at runtime, which can lead to performance improvements.
    /// </summary>
    [GeneratedRegex(@"(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex LiterPattern();
}
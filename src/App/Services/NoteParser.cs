using System.Text.RegularExpressions;
using dme_workflow_parser.Models;

namespace dme_workflow_parser.Services;

/// <summary>
/// Physician notes parsing service.
/// </summary>
/// <param name="settings">Dependency-injected application configuration settings.</param>
public partial class NoteParser(Settings settings /*, ILogger<NoteParser> logger*/)
{
    readonly List<string> maskTypes = ["full face", "nasal"];
    readonly List<string> addOns = ["humidifier", "heated tubing"];

    // Note: the order of items in this list matters. More specific items should come first.
    readonly List<string> usageTypes = ["sleep and exertion", "sleep", "exertion", "movement"];

    readonly List<string> qualifiers = ["ahi > 20", "ahi < 20", "ahi = 20"];
    readonly List<string> devices = ["cpap", "oxygen tank", "wheelchair"];

    /// <summary>
    /// Parses physician notes to extract and return DME orders.
    /// In order to extract info more efficiently, We'll make a few assumptions based on how the notes text is formatted:
    /// - Each piece of information is on its own line, starting with a label (e.g., "Patient Name:", "DOB:", "Diagnosis:", etc.).
    /// - The Prescription line however may contain multiple pieces of information in a single line.
    /// - The usage line seems to be a phrase that may contain a keyword or key-phrase for a single piece of information, so it warrants a "contains" search.
    /// So our approach will be to break down each line into key/value pairs and assign the values 1 to 1 with the Order properties, except for prescription.
    /// There, we will attempt to extract multiple pieces of information by checking if certain keywords exist in the line.
    /// </summary>
    /// <returns>A named tuple with an order, if parsing was successful or and error message if not.</returns>
    public (string? ErrorMsg, Order? order) Parse()
    {
        // logger.LogInformation("Parsing notes from {InputFolder}", settings.InputFolder);
        string textNotePath = Path.Combine(settings.InputFolder, settings.TextInputFile);

        if (!File.Exists(textNotePath))
        {
            string err = $"Input text file not found: '{textNotePath}'.";
            // logger.LogError(err);
            return (err, null);
        }

        List<string> notes = [];

        try
        {
            notes.AddRange(File.ReadAllLines(textNotePath));
        }
        catch (Exception ex)
        {
            string err = $"Error reading input text file '{textNotePath}': {ex.Message}";
            // logger.LogError(ex, err);
            return (err, null);
        }

        Order order = new();

        notes.ForEach(note =>
        {
            string[] pair = note.Trim().Split(':', 2);

            // Log and skip lines that don't have a key/value format.
            if (pair.Length != 2)
            {
                // logger.LogWarning("Skipping malformed line: {Line}", note);
                return;
            }

            switch (pair[0].Trim().ToLower())
            {
                case "patient name":
                    order.PatientName = pair[1].Trim();
                    break;
                case "dob":
                    order.PatientDob = pair[1].Trim();
                    break;
                case "diagnosis":
                    order.Diagnosis = pair[1].Trim();
                    break;
                case "ordering physician":
                    order.OrderingProvider = pair[1].Trim();
                    break;
                case "usage":
                    string use = pair[1].Trim();
                    string? usage = usageTypes.FirstOrDefault(u => use.Contains(u, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(usage))
                    {
                        order.Usage = usage;
                    }
                    break;
                case "prescription":
                    string presc = pair[1].Trim();

                    // Device.
                    string? device = devices.FirstOrDefault(d => presc.Contains(d, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(device))
                    {
                        order.Device = device;
                    }

                    // Mask Type.
                    string? mask = maskTypes.FirstOrDefault(m => presc.Contains(m, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(mask))
                    {
                        order.MaskType = mask;
                    }

                    // Add-Ons.
                    List<string> addons = [.. addOns.Where(a => presc.Contains(a, StringComparison.OrdinalIgnoreCase))];

                    if (addons.Count != 0)
                    {
                        order.AddOns = addons;
                    }

                    // Qualifier.
                    string? qual = qualifiers.FirstOrDefault(q => presc.Contains(q, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(qual))
                    {
                        order.Qualifier = qual;
                    }

                    // Liters (specific to Oxygen Tank).
                    if (!string.IsNullOrWhiteSpace(order.Device) && order.Device.Equals("Oxygen Tank", StringComparison.OrdinalIgnoreCase))
                    {
                        Match literValueMatch = LiterPattern().Match(presc);

                        if (literValueMatch.Success)
                        {
                            order.Liters = $"{literValueMatch.Groups[1].Value} L";
                        }
                    }
                    break;
                default:
                    // logger.LogWarning("Unrecognized note labe;: {Label}", pair[0]);
                    break;
            }
        });

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
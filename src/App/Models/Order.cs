using System.Text.Json.Serialization;

namespace dme_workflow_parser.Models;

public class Order
{
    public string? Device { get; set; }
    public string? MaskType { get; set; }
    public List<string>? AddOns { get; set; }
    public string? Qualifier { get; set; }
    public string? OrderingProvider { get; set; }
    public string? Liters { get; set; }
    public string? Usage { get; set; }
    public string? Diagnosis { get; set; }
    public string? PatientName { get; set; }

    [JsonPropertyName("dob")]
    public string? PatientDob { get; set; }
}
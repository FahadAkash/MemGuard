
namespace MemGuard.Core.Models;

public class FeatureStatus
{

    public string FeatureName { get; set; } = string.Empty;
    public FeatureAvailability Availability { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public enum FeatureAvailability
{
    Available,
    LimitedByDump,
    NotAvailable
}
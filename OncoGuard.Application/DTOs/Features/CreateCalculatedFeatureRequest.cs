namespace OncoGuard.Application.DTOs.Features;

public class CreateCalculatedFeatureRequest
{
    public int PatientId { get; set; }

    public string FeatureName { get; set; } = null!;

    public double FeatureValue { get; set; }

    public DateTime CalculatedAt { get; set; }

    public string? Description { get; set; }
}
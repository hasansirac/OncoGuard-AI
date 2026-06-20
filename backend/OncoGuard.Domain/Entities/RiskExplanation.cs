using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class RiskExplanation : BaseEntity
{
    public int RiskScoreId { get; set; }
    public RiskScore RiskScore { get; set; } = null!;

    public string ExplanationTitle { get; set; } = null!;

    public string ExplanationText { get; set; } = null!;

    public double ContributionScore { get; set; }

    public bool IsPrimaryReason { get; set; }

    public string? SuggestedAction { get; set; }
}
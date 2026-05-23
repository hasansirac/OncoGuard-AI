namespace OncoGuard.Application.Interfaces.Features;

public interface IFeatureEngineeringService
{
    Task GenerateFeaturesAsync(int patientId);
}

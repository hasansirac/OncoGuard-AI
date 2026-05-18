using OncoGuard.Application.DTOs.Labs;

namespace OncoGuard.Application.Interfaces.Labs;

public interface ILabService
{
    Task AddLabResultAsync(CreateLabResultRequest request);
}

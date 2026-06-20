using OncoGuard.Application.DTOs.FoodLogs;

namespace OncoGuard.Application.Interfaces.FoodLogs;

public interface IFoodLogService
{
    Task AddFoodLogAsync(CreateFoodLogRequest request);
}

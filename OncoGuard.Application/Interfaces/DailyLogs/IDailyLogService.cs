using OncoGuard.Application.DTOs.DailyLogs;

namespace OncoGuard.Application.Interfaces.DailyLogs;

public interface IDailyLogService
{
    Task CreateDailyLogAsync(CreateDailyLogRequest request);
}
using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<AdminRecentActivityDto> GetRecentActivityAsync(CancellationToken cancellationToken = default);
}

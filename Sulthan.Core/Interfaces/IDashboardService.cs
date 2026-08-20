using Sulthan.Core.DTOs.Dashboard;

namespace Sulthan.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardAsync();
}
using Sulthan.Core.DTOs.CashClosings;

namespace Sulthan.Core.Interfaces;

public interface ICashClosingService
{
    Task<CashClosingSummaryDto> GetTodayAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<CashClosingSummaryDto> CreateTodayAsync(
        CreateCashClosingDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}

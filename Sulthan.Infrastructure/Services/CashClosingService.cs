using System.Data;
using Microsoft.EntityFrameworkCore;
using Sulthan.Core.DTOs.CashClosings;
using Sulthan.Core.Entities;
using Sulthan.Core.Enums;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Services;

/// <summary>
/// Creates one cash-count snapshot per cashier per business day. Sales remain available after closing.
/// </summary>
public sealed class CashClosingService : ICashClosingService
{
    private readonly RestaurantDbContext _context;

    public CashClosingService(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<CashClosingSummaryDto> GetTodayAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCashierExistsAsync(authenticatedUserId, cancellationToken);

        var businessDate = DateOnly.FromDateTime(DateTime.Today);
        var closing = await _context.CashClosings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CashierId == authenticatedUserId && x.BusinessDate == businessDate,
                cancellationToken);

        if (closing is not null)
            return MapClosing(closing);

        var collection = await GetTodayCollectionAsync(authenticatedUserId, businessDate, cancellationToken);
        return MapOpenDay(businessDate, collection);
    }

    public async Task<CashClosingSummaryDto> CreateTodayAsync(
        CreateCashClosingDto dto,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        if (dto.CountedCash < 0)
            throw new ArgumentException("Counted cash cannot be negative.");

        if (dto.Notes?.Length > 500)
            throw new ArgumentException("Notes cannot exceed 500 characters.");

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureCashierExistsAsync(authenticatedUserId, cancellationToken);

            var businessDate = DateOnly.FromDateTime(DateTime.Today);
            var exists = await _context.CashClosings
                .AnyAsync(
                    x => x.CashierId == authenticatedUserId && x.BusinessDate == businessDate,
                    cancellationToken);

            if (exists)
                throw new InvalidOperationException("Today's cash closing has already been recorded.");

            var collection = await GetTodayCollectionAsync(authenticatedUserId, businessDate, cancellationToken);
            var closing = new CashClosing
            {
                CashierId = authenticatedUserId,
                BusinessDate = businessDate,
                ExpectedCash = collection.Cash,
                CardCollection = collection.Card,
                UpiCollection = collection.Upi,
                TotalCollection = collection.Total,
                CountedCash = dto.CountedCash,
                Variance = dto.CountedCash - collection.Cash,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                ClosedOn = DateTime.Now
            };

            _context.CashClosings.Add(closing);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapClosing(closing);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException("Today's cash closing could not be saved. Please refresh and try again.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task EnsureCashierExistsAsync(int cashierId, CancellationToken cancellationToken)
    {
        var exists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == cashierId && x.IsActive, cancellationToken);

        if (!exists)
            throw new UnauthorizedAccessException("The signed-in cashier no longer exists.");
    }

    private async Task<CollectionTotals> GetTodayCollectionAsync(
        int cashierId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var start = businessDate.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);
        var payments = await _context.Payments
            .AsNoTracking()
            .Include(x => x.Allocations)
            .Where(x =>
                x.IsActive &&
                x.UserId == cashierId &&
                x.PaymentStatus == PaymentStatus.Paid &&
                x.PaymentDate >= start &&
                x.PaymentDate < end)
            .ToListAsync(cancellationToken);

        var lines = payments.SelectMany(payment =>
                payment.Allocations.Count > 0
                    ? payment.Allocations.Select(x => new CollectionAmount(x.PaymentMethod, x.Amount))
                    : new[] { new CollectionAmount(payment.PaymentMethod, payment.PaidAmount) })
            .ToList();

        return new CollectionTotals(
            Cash: lines.Where(x => x.PaymentMethod == PaymentMode.Cash).Sum(x => x.Amount),
            Card: lines.Where(x => x.PaymentMethod == PaymentMode.Card).Sum(x => x.Amount),
            Upi: lines.Where(x => x.PaymentMethod == PaymentMode.Upi).Sum(x => x.Amount));
    }

    private static CashClosingSummaryDto MapOpenDay(DateOnly businessDate, CollectionTotals collection)
    {
        return new CashClosingSummaryDto
        {
            BusinessDate = businessDate,
            ExpectedCash = collection.Cash,
            CardCollection = collection.Card,
            UpiCollection = collection.Upi,
            TotalCollection = collection.Total,
            IsClosed = false
        };
    }

    private static CashClosingSummaryDto MapClosing(CashClosing closing)
    {
        return new CashClosingSummaryDto
        {
            BusinessDate = closing.BusinessDate,
            ExpectedCash = closing.ExpectedCash,
            CardCollection = closing.CardCollection,
            UpiCollection = closing.UpiCollection,
            TotalCollection = closing.TotalCollection,
            IsClosed = true,
            CountedCash = closing.CountedCash,
            Variance = closing.Variance,
            Notes = closing.Notes,
            ClosedOn = closing.ClosedOn
        };
    }

    private sealed record CollectionAmount(PaymentMode PaymentMethod, decimal Amount);

    private sealed record CollectionTotals(decimal Cash, decimal Card, decimal Upi)
    {
        public decimal Total => Cash + Card + Upi;
    }
}

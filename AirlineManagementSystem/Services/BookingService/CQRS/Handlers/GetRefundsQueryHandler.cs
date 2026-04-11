using BookingService.CQRS.Queries;
using BookingService.Data;
using BookingService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BookingService.CQRS.Handlers;

public class GetRefundsQueryHandler
{
    private readonly BookingDbContext _dbContext;

    public GetRefundsQueryHandler(BookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RefundDetailsDto>> HandleAsync(GetRefundsQuery query)
    {
        var refunds = await _dbContext.Refunds
            .Include(r => r.Booking)
            .OrderByDescending(r => r.CancellationTime)
            .ToListAsync();

        var passengerIds = refunds.Where(r => r.PassengerId.HasValue).Select(r => r.PassengerId.Value).Distinct().ToList();
        var passengers = await _dbContext.Passengers.Where(p => passengerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var result = refunds.Select(r =>
        {
            var dto = new RefundDetailsDto
            {
                Id = r.Id,
                BookingId = r.BookingId,
                PNR = r.Booking?.PNR ?? "Unknown",
                PassengerId = r.PassengerId,
                RefundAmount = r.RefundAmount,
                RefundPercentage = r.RefundPercentage,
                RefundStatus = r.RefundStatus,
                CancellationTime = r.CancellationTime,
                DepartureTime = r.DepartureTime,
                PassengerName = "Entire Booking",
                passengerAadhar = "N/A",
                CancellationReason = "Cancelled by User"
            };

            if (r.PassengerId.HasValue && passengers.TryGetValue(r.PassengerId.Value, out var passenger))
            {
                dto.PassengerName = passenger.Name;
                dto.passengerAadhar = passenger.AadharCardNo;
                dto.CancellationReason = passenger.CancellationReason ?? "Cancelled by User";
            }

            return dto;
        }).ToList();

        return result;
    }
}

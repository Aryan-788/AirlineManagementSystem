using AgentService.DTOs;
using AgentService.Models;
using AgentService.Repositories;

namespace AgentService.Services;

public interface IAgentService
{
    Task<DealerDto> CreateDealerAsync(CreateDealerDto dto);
    Task<DealerDto> GetDealerAsync(int id);
    Task<DealerDto> GetDealerByEmailAsync(string email);
    Task<DealerDto> AllocateSeatsAsync(int dealerId, int seats);
    Task<DealerBookingDto> RecordDealerBookingAsync(int dealerId, int bookingId, int flightId, decimal bookingAmount);
    Task<IEnumerable<DealerCommissionReportDto>> GetCommissionReportAsync();
    Task<IEnumerable<DealerDto>> GetAllDealersAsync();
    Task<DealerPerformanceDto> GetDealerPerformanceAsync(int dealerId);
}

public class AgentServiceImpl : IAgentService
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IDealerBookingRepository _dealerBookingRepository;

    public AgentServiceImpl(IDealerRepository dealerRepository, IDealerBookingRepository dealerBookingRepository)
    {
        _dealerRepository = dealerRepository;
        _dealerBookingRepository = dealerBookingRepository;
    }

    public async Task<DealerDto> CreateDealerAsync(CreateDealerDto dto)
    {
        var existingDealer = await _dealerRepository.GetByEmailAsync(dto.DealerEmail);
        if (existingDealer != null) 
        {
            return MapToDto(existingDealer);
        }

        var dealer = new Dealer
        {
            DealerName = dto.DealerName,
            DealerEmail = dto.DealerEmail,
            AllocatedSeats = dto.AllocatedSeats,
            CommissionRate = dto.CommissionRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dealerRepository.AddAsync(dealer);
        return MapToDto(dealer);
    }

    public async Task<DealerDto> GetDealerAsync(int id)
    {
        var dealer = await _dealerRepository.GetByIdAsync(id);
        if (dealer == null)
            throw new KeyNotFoundException($"Dealer {id} not found");

        return MapToDto(dealer);
    }

    public async Task<DealerDto> GetDealerByEmailAsync(string email)
    {
        var dealer = await _dealerRepository.GetByEmailAsync(email);
        if (dealer == null)
            throw new KeyNotFoundException($"Dealer with email {email} not found");

        return MapToDto(dealer);
    }

    public async Task<DealerDto> AllocateSeatsAsync(int dealerId, int seats)
    {
        var dealer = await _dealerRepository.GetByIdAsync(dealerId);
        if (dealer == null)
            throw new KeyNotFoundException($"Dealer {dealerId} not found");

        dealer.AllocatedSeats += seats;
        await _dealerRepository.UpdateAsync(dealer);

        return MapToDto(dealer);
    }

    public async Task<DealerBookingDto> RecordDealerBookingAsync(int dealerId, int bookingId, int flightId, decimal bookingAmount)
    {
        var dealer = await _dealerRepository.GetByIdAsync(dealerId);
        if (dealer == null)
            throw new KeyNotFoundException($"Dealer {dealerId} not found");

        var commission = bookingAmount * (dealer.CommissionRate / 100);

        var dealerBooking = new DealerBooking
        {
            DealerId = dealerId,
            BookingId = bookingId,
            FlightId = flightId,
            Commission = commission,
            CreatedAt = DateTime.UtcNow
        };

        await _dealerBookingRepository.AddAsync(dealerBooking);

        dealer.UsedSeats++;
        await _dealerRepository.UpdateAsync(dealer);

        return MapDealerBookingToDto(dealerBooking);
    }

    public async Task<IEnumerable<DealerCommissionReportDto>> GetCommissionReportAsync()
    {
        var dealers = await _dealerRepository.GetAllAsync();
        var reports = new List<DealerCommissionReportDto>();

        foreach (var dealer in dealers)
        {
            var bookings = await _dealerBookingRepository.GetByDealerIdAsync(dealer.Id);
            var totalCommission = await _dealerBookingRepository.GetTotalCommissionAsync(dealer.Id);

            reports.Add(new DealerCommissionReportDto
            {
                DealerId = dealer.Id,
                DealerName = dealer.DealerName,
                TotalBookings = bookings.Count(),
                TotalCommission = totalCommission
            });
        }

        return reports;
    }

    public async Task<IEnumerable<DealerDto>> GetAllDealersAsync()
    {
        var dealers = await _dealerRepository.GetAllAsync();
        return dealers.Select(MapToDto);
    }

    public async Task<DealerPerformanceDto> GetDealerPerformanceAsync(int dealerId)
    {
        var dealer = await _dealerRepository.GetByIdAsync(dealerId);
        if (dealer == null)
            throw new KeyNotFoundException($"Dealer {dealerId} not found");

        var bookings = await _dealerBookingRepository.GetByDealerIdAsync(dealerId);
        var totalCommission = await _dealerBookingRepository.GetTotalCommissionAsync(dealerId);

        return new DealerPerformanceDto
        {
            AllocatedSeats = dealer.AllocatedSeats,
            UsedSeats = dealer.UsedSeats,
            AvailableSeats = dealer.AllocatedSeats - dealer.UsedSeats,
            TotalCommission = totalCommission,
            TotalBookings = bookings.Count(),
            CommissionRate = dealer.CommissionRate
        };
    }

    private DealerDto MapToDto(Dealer dealer)
    {
        return new DealerDto
        {
            Id = dealer.Id,
            DealerName = dealer.DealerName,
            DealerEmail = dealer.DealerEmail,
            AllocatedSeats = dealer.AllocatedSeats,
            UsedSeats = dealer.UsedSeats,
            AvailableSeats = dealer.AllocatedSeats - dealer.UsedSeats,
            CommissionRate = dealer.CommissionRate,
            IsActive = dealer.IsActive
        };
    }

    private DealerBookingDto MapDealerBookingToDto(DealerBooking dealerBooking)
    {
        return new DealerBookingDto
        {
            Id = dealerBooking.Id,
            DealerId = dealerBooking.DealerId,
            BookingId = dealerBooking.BookingId,
            FlightId = dealerBooking.FlightId,
            Commission = dealerBooking.Commission
        };
    }
}

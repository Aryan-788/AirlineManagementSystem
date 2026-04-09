using FlightService.Caching;
using FlightService.DTOs;
using FlightService.Models;
using FlightService.Repositories;
using FlightService.Services;
using Moq;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FlightService.Tests;

public class FlightServiceTests
{
    private readonly Mock<IFlightRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<FlightService.Services.FlightService>> _mockLogger;
    private readonly FlightService.Services.FlightService _flightService;

    public FlightServiceTests()
    {
        _mockRepository = new Mock<IFlightRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<FlightService.Services.FlightService>>();

        _flightService = new FlightService.Services.FlightService(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    #region GetFlightAsync Tests

    [Fact]
    public async Task GetFlightAsync_ShouldReturnFlight_WhenFlightExists()
    {
        // Arrange
        var flightId = 1;
        var flight = new Flight
        {
            Id = flightId,
            FlightNumber = "AI101",
            Source = "Delhi",
            Destination = "Mumbai",
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Aircraft = "Boeing 737",
            TotalSeats = 180,
            AvailableSeats = 180,
            EconomySeats = 140,
            BusinessSeats = 30,
            FirstSeats = 10,
            EconomyPrice = 5000,
            BusinessPrice = 15000,
            FirstClassPrice = 30000,
            Status = FlightStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(flightId))
            .ReturnsAsync(flight);

        // Act
        var result = await _flightService.GetFlightAsync(flightId);

        // Assert
        result.Should().NotBeNull();
        result.FlightNumber.Should().Be("AI101");
        result.Source.Should().Be("Delhi");
        result.Destination.Should().Be("Mumbai");
        _mockRepository.Verify(r => r.GetByIdAsync(flightId), Times.Once);
    }

    [Fact]
    public async Task GetFlightAsync_ShouldThrowKeyNotFoundException_WhenFlightNotFound()
    {
        // Arrange
        var flightId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(flightId))
            .ReturnsAsync((Flight?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _flightService.GetFlightAsync(flightId));
    }

    #endregion

    #region SearchFlightsAsync Tests

    [Fact]
    public async Task SearchFlightsAsync_ShouldReturnFlights_WhenFlightsExist()
    {
        // Arrange
        var source = "Delhi";
        var destination = "Mumbai";
        var departureDate = DateTime.UtcNow.AddDays(1);

        var flights = new List<Flight>
        {
            new Flight
            {
                Id = 1,
                FlightNumber = "AI101",
                Source = source,
                Destination = destination,
                DepartureTime = departureDate,
                ArrivalTime = departureDate.AddHours(2),
                Aircraft = "Boeing 737",
                TotalSeats = 180,
                AvailableSeats = 180,
                EconomySeats = 140,
                BusinessSeats = 30,
                FirstSeats = 10,
                EconomyPrice = 5000,
                BusinessPrice = 15000,
                FirstClassPrice = 30000,
                Status = FlightStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockRepository.Setup(r => r.SearchAsync(source, destination, It.IsAny<DateTime>()))
            .ReturnsAsync(flights);

        // Act
        var result = await _flightService.SearchFlightsAsync(source, destination, departureDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().FlightNumber.Should().Be("AI101");
        _mockRepository.Verify(r => r.SearchAsync(source, destination, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task SearchFlightsAsync_ShouldReturnEmptyList_WhenNoFlightsFound()
    {
        // Arrange
        var source = "Delhi";
        var destination = "Mumbai";
        var departureDate = DateTime.UtcNow.AddDays(1);

        _mockRepository.Setup(r => r.SearchAsync(source, destination, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Flight>());

        // Act
        var result = await _flightService.SearchFlightsAsync(source, destination, departureDate);

        // Assert
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.SearchAsync(source, destination, It.IsAny<DateTime>()), Times.Once);
    }

    #endregion

    #region CreateFlightAsync Tests

    [Fact]
    public async Task CreateFlightAsync_ShouldCreateFlight_WithValidDto()
    {
        // Arrange
        var createDto = new CreateFlightDto
        {
            FlightNumber = "AI102",
            Source = "Delhi",
            Destination = "Bangalore",
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(3),
            Aircraft = "Airbus A320",
            TotalSeats = 200,
            EconomySeats = 150,
            BusinessSeats = 40,
            FirstSeats = 10,
            EconomyPrice = 4500,
            BusinessPrice = 14000,
            FirstClassPrice = 28000
        };

        Flight? capturedFlight = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Flight>()))
            .Callback<Flight>(f => capturedFlight = f)
            .ReturnsAsync((Flight f) => f);

        // Act
        var result = await _flightService.CreateFlightAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.FlightNumber.Should().Be("AI102");
        result.Status.Should().Be("Scheduled");
        capturedFlight?.TotalSeats.Should().Be(200);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Flight>()), Times.Once);
    }

    #endregion

    #region BookSeatAsync Tests

    [Fact]
    public async Task BookSeatAsync_ShouldDecreaseAvailableSeats_WhenSeatsAvailable()
    {
        // Arrange
        var flightId = 1;
        var flight = new Flight
        {
            Id = flightId,
            FlightNumber = "AI101",
            Source = "Delhi",
            Destination = "Mumbai",
            EconomySeats = 50,
            AvailableSeats = 180,
            Status = FlightStatus.Scheduled
        };

        _mockCacheService.Setup(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        _mockCacheService.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockRepository.Setup(r => r.GetByIdAsync(flightId))
            .ReturnsAsync(flight);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Flight>()))
            .Returns(Task.CompletedTask);

        // Act
        await _flightService.BookSeatAsync(flightId, "Economy", 5);

        // Assert
        flight.EconomySeats.Should().Be(45);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Flight>()), Times.Once);
    }

    [Fact]
    public async Task BookSeatAsync_ShouldThrowException_WhenLockNotAcquired()
    {
        // Arrange
        var flightId = 1;
        _mockCacheService.Setup(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _flightService.BookSeatAsync(flightId, "Economy", 5));

        exception.Message.Should().Contain("currently being booked");
    }

    #endregion

    #region DelayFlightAsync Tests

    [Fact]
    public async Task DelayFlightAsync_ShouldUpdateFlightStatus_AndPublishEvent()
    {
        // Arrange
        var flightId = 1;
        var flight = new Flight
        {
            Id = flightId,
            FlightNumber = "AI101",
            DepartureTime = DateTime.UtcNow.AddDays(1),
            Status = FlightStatus.Scheduled
        };

        var newDepartureTime = DateTime.UtcNow.AddDays(1).AddHours(3);

        _mockRepository.Setup(r => r.GetByIdAsync(flightId))
            .ReturnsAsync(flight);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Flight>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<FlightDelayedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _flightService.DelayFlightAsync(flightId, newDepartureTime);

        // Assert
        flight.Status.Should().Be(FlightStatus.Delayed);
        flight.DepartureTime.Should().Be(newDepartureTime);
        _mockRepository.Verify(r => r.UpdateAsync(flight), Times.Once);
        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<FlightDelayedEvent>()), Times.Once);
    }

    #endregion

    #region CancelFlightAsync Tests

    [Fact]
    public async Task CancelFlightAsync_ShouldCancelFlight()
    {
        // Arrange
        var flightId = 1;
        var flight = new Flight
        {
            Id = flightId,
            FlightNumber = "AI101",
            Status = FlightStatus.Scheduled
        };

        _mockRepository.Setup(r => r.GetByIdAsync(flightId))
            .ReturnsAsync(flight);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Flight>()))
            .Returns(Task.CompletedTask);

        // Act
        await _flightService.CancelFlightAsync(flightId);

        // Assert
        flight.Status.Should().Be(FlightStatus.Cancelled);
        _mockRepository.Verify(r => r.UpdateAsync(flight), Times.Once);
    }

    #endregion
}

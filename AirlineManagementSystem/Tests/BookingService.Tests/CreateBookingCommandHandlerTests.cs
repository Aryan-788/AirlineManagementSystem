using BookingService.CQRS.Commands;
using BookingService.CQRS.Handlers;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Repositories;
using Moq;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BookingService.Tests;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly Mock<ILogger<CreateBookingCommandHandler>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandHandlerTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockLogger = new Mock<ILogger<CreateBookingCommandHandler>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["ServiceUrls:FlightService"])
            .Returns("http://localhost:5002");

        _handler = new CreateBookingCommandHandler(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockHttpClient.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowInvalidOperationException_WhenInvalidSeatClass()
    {
        // Arrange
        var dto = new CreateBookingDto
        {
            UserId = 1,
            FlightId = 1,
            SeatClass = "InvalidClass",
            BaggageWeight = 20,
            PassengerCount = 1,
            TotalAmount = 5000
        };

        var command = new CreateBookingCommand(dto);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        exception.Message.Should().Contain("Invalid seat class");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowInvalidOperationException_WhenBaggageWeightNegative()
    {
        // Arrange
        var dto = new CreateBookingDto
        {
            UserId = 1,
            FlightId = 1,
            SeatClass = "Economy",
            BaggageWeight = -5,
            PassengerCount = 1,
            TotalAmount = 5000
        };

        var command = new CreateBookingCommand(dto);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        exception.Message.Should().Contain("Baggage weight must be between 0 and 100 kg");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowInvalidOperationException_WhenBaggageWeightTooHigh()
    {
        // Arrange
        var dto = new CreateBookingDto
        {
            UserId = 1,
            FlightId = 1,
            SeatClass = "Economy",
            BaggageWeight = 150,
            PassengerCount = 1,
            TotalAmount = 5000
        };

        var command = new CreateBookingCommand(dto);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        exception.Message.Should().Contain("Baggage weight must be between 0 and 100 kg");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowInvalidOperationException_WhenUserIdInvalid()
    {
        // Arrange
        var dto = new CreateBookingDto
        {
            UserId = -1,
            FlightId = 1,
            SeatClass = "Economy",
            BaggageWeight = 20,
            PassengerCount = 1,
            TotalAmount = 5000
        };

        var command = new CreateBookingCommand(dto);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        exception.Message.Should().Contain("User ID must be greater than 0");
    }
}

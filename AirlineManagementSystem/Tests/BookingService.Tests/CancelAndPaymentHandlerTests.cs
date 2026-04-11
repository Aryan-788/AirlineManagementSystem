using BookingService.CQRS.Commands;
using BookingService.CQRS.Handlers;
using BookingService.Models;
using BookingService.Repositories;
using Moq;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using BookingService.Services;

namespace BookingService.Tests;

public class CancelBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IRefundService> _mockRefundService;
    private readonly Mock<ILogger<CancelBookingCommandHandler>> _mockLogger;
    private readonly CancelBookingCommandHandler _handler;

    public CancelBookingCommandHandlerTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockRefundService = new Mock<IRefundService>();
        _mockLogger = new Mock<ILogger<CancelBookingCommandHandler>>();

        // Default refund service behavior
        _mockRefundService.Setup(r => r.ProcessRefundAsync(It.IsAny<Booking>(), It.IsAny<int?>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _handler = new CancelBookingCommandHandler(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockRefundService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelBooking_WithValidBookingId()
    {
        // Arrange
        var bookingId = 1;
        var booking = new Booking
        {
            Id = bookingId,
            UserId = 1,
            FlightId = 1,
            Status = BookingStatus.Pending,
            PNR = "ABC123",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<BookingCancelledEvent>()))
            .Returns(Task.CompletedTask);

        var command = new CancelBookingCommand(bookingId);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
        _mockRepository.Verify(r => r.GetByIdAsync(bookingId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(booking), Times.Once);
        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<BookingCancelledEvent>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowKeyNotFoundException_WhenBookingNotFound()
    {
        // Arrange
        var bookingId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync((Booking?)null);

        var command = new CancelBookingCommand(bookingId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.HandleAsync(command));

        exception.Message.Should().Contain($"Booking {bookingId} not found");
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishBookingCancelledEvent_WithCorrectData()
    {
        // Arrange
        var bookingId = 1;
        var booking = new Booking
        {
            Id = bookingId,
            UserId = 5,
            FlightId = 10,
            Status = BookingStatus.Pending,
            PNR = "XYZ789",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .Returns(Task.CompletedTask);

        BookingCancelledEvent? publishedEvent = null;
        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<BookingCancelledEvent>()))
            .Callback<BookingCancelledEvent>(e => publishedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CancelBookingCommand(bookingId);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.BookingId.Should().Be(bookingId);
        publishedEvent.UserId.Should().Be(5);
        publishedEvent.FlightId.Should().Be(10);
    }
}

public class HandlePaymentSuccessCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<HandlePaymentSuccessCommandHandler>> _mockLogger;
    private readonly HandlePaymentSuccessCommandHandler _handler;

    public HandlePaymentSuccessCommandHandlerTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<HandlePaymentSuccessCommandHandler>>();

        _handler = new HandlePaymentSuccessCommandHandler(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldConfirmBooking_WhenPaymentSucceeds()
    {
        // Arrange
        var paymentEvent = new PaymentSuccessEvent(
            PaymentId: 1,
            BookingId: 1,
            UserId: 1,
            Amount: 5000,
            ProcessedAt: DateTime.UtcNow);

        var booking = new Booking
        {
            Id = 1,
            UserId = 1,
            FlightId = 1,
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PNR = "ABC123",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(paymentEvent.BookingId))
            .ReturnsAsync(booking);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<RewardEarnedEvent>()))
            .Returns(Task.CompletedTask);

        var command = new HandlePaymentSuccessCommand(paymentEvent);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.PaymentStatus.Should().Be(PaymentStatus.Success);
        _mockRepository.Verify(r => r.UpdateAsync(booking), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishRewardEarnedEvent_WhenPaymentSucceeds()
    {
        // Arrange
        var paymentEvent = new PaymentSuccessEvent(
            PaymentId: 1,
            BookingId: 1,
            UserId: 5,
            Amount: 5000,
            ProcessedAt: DateTime.UtcNow);

        var booking = new Booking
        {
            Id = 1,
            UserId = 5,
            FlightId = 1,
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PNR = "ABC123",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(paymentEvent.BookingId))
            .ReturnsAsync(booking);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .Returns(Task.CompletedTask);

        RewardEarnedEvent? rewardEvent = null;
        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<RewardEarnedEvent>()))
            .Callback<RewardEarnedEvent>(e => rewardEvent = e)
            .Returns(Task.CompletedTask);

        var command = new HandlePaymentSuccessCommand(paymentEvent);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        rewardEvent.Should().NotBeNull();
        rewardEvent!.UserId.Should().Be(5);
        rewardEvent.Points.Should().Be(100);
        rewardEvent.BookingId.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowKeyNotFoundException_WhenBookingNotFound()
    {
        // Arrange
        var paymentEvent = new PaymentSuccessEvent(
            PaymentId: 1,
            BookingId: 999,
            UserId: 1,
            Amount: 5000,
            ProcessedAt: DateTime.UtcNow);

        _mockRepository.Setup(r => r.GetByIdAsync(paymentEvent.BookingId))
            .ReturnsAsync((Booking?)null);

        var command = new HandlePaymentSuccessCommand(paymentEvent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateTimestamp_WhenProcessingPaymentSuccess()
    {
        // Arrange
        var paymentEvent = new PaymentSuccessEvent(
            PaymentId: 1,
            BookingId: 1,
            UserId: 1,
            Amount: 5000,
            ProcessedAt: DateTime.UtcNow);

        var booking = new Booking
        {
            Id = 1,
            UserId = 1,
            FlightId = 1,
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            UpdatedAt = null,
            PNR = "ABC123",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(paymentEvent.BookingId))
            .ReturnsAsync(booking);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<RewardEarnedEvent>()))
            .Returns(Task.CompletedTask);

        var command = new HandlePaymentSuccessCommand(paymentEvent);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        booking.UpdatedAt.Should().NotBeNull();
        booking.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}

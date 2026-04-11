using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;
using PaymentService.Services;
using Moq;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Tests;

// Test HttpMessageHandler for mocking HTTP requests
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler = null)
    {
        _handler = handler ?? (req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": 1, \"status\": \"Pending\"}")
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<PaymentServiceImpl>> _mockLogger;
    private readonly PaymentServiceImpl _paymentService;

    public PaymentServiceTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<PaymentServiceImpl>>();

        _mockConfiguration.Setup(c => c["ServiceUrls:BookingService"])
            .Returns("http://localhost:5003");
        _mockConfiguration.Setup(c => c["Razorpay:KeyId"])
            .Returns("test_key_id");
        _mockConfiguration.Setup(c => c["Razorpay:KeySecret"])
            .Returns("test_key_secret");

        // Use a test HttpClient with TestHttpMessageHandler
        _httpClient = new HttpClient(new TestHttpMessageHandler());

        _paymentService = new PaymentServiceImpl(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _httpClient,
            _mockConfiguration.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);
    }

    #region GetPaymentAsync Tests

    [Fact]
    public async Task GetPaymentAsync_ShouldReturnPayment_WhenPaymentExists()
    {
        // Arrange
        var paymentId = 1;
        var payment = new Payment
        {
            Id = paymentId,
            BookingId = 1,
            Amount = 5000,
            PaymentMethod = "RazorPay",
            TransactionId = "TXN123",
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetPaymentAsync(paymentId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(paymentId);
        result.Amount.Should().Be(5000);
        result.Status.Should().Be("Success");
        _mockRepository.Verify(r => r.GetByIdAsync(paymentId), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAsync_ShouldThrowKeyNotFoundException_WhenPaymentNotFound()
    {
        // Arrange
        var paymentId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _paymentService.GetPaymentAsync(paymentId));
    }

    #endregion

    #region ProcessPaymentAsync Tests

    [Fact]
    public async Task ProcessPaymentAsync_ShouldMarkPaymentSuccess_WhenPaymentSucceeds()
    {
        // Arrange
        var dto = new ProcessPaymentDto
        {
            BookingId = 1,
            Amount = 5000,
            PaymentMethod = "RazorPay",
            UserId = 1
        };

        Payment? savedPayment = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(p => savedPayment = p)
            .ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<PaymentSuccessEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Success");
        savedPayment?.Amount.Should().Be(5000);
        savedPayment?.Status.Should().Be(PaymentStatus.Success);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldPublishPaymentSuccessEvent()
    {
        // Arrange
        var dto = new ProcessPaymentDto
        {
            BookingId = 1,
            Amount = 5000,
            PaymentMethod = "RazorPay",
            UserId = 1
        };

        Payment? savedPayment = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(p => 
            { 
                p.Id = 1; // Set the Id when AddAsync is called
                savedPayment = p;
            })
            .ReturnsAsync((Payment p) => p);

        PaymentSuccessEvent? publishedEvent = null;
        _mockEventPublisher.Setup(p => p.PublishAsync(It.IsAny<PaymentSuccessEvent>()))
            .Callback<PaymentSuccessEvent>(e => publishedEvent = e)
            .Returns(Task.CompletedTask);

        // Act
        await _paymentService.ProcessPaymentAsync(dto);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.PaymentId.Should().Be(1);
        publishedEvent.BookingId.Should().Be(1);
        publishedEvent.Amount.Should().Be(5000);
    }

    #endregion

    #region RefundAsync Tests

    [Fact]
    public async Task RefundAsync_ShouldMarkPaymentRefunded_WhenRefundSucceeds()
    {
        // Arrange
        var paymentId = 1;
        var payment = new Payment
        {
            Id = paymentId,
            BookingId = 1,
            Amount = 5000,
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.RefundAsync(paymentId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Refunded");
        payment.Status.Should().Be(PaymentStatus.Refunded);
        _mockRepository.Verify(r => r.UpdateAsync(payment), Times.Once);
    }

    [Fact]
    public async Task RefundAsync_ShouldThrowKeyNotFoundException_WhenPaymentNotFound()
    {
        // Arrange
        var paymentId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _paymentService.RefundAsync(paymentId));
    }

    #endregion
}

public class PaymentRepositoryTests
{
    [Fact]
    public async Task GetByBookingIdAsync_ShouldReturnPayment_WhenPaymentExists()
    {
        // Arrange
        var dbOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PaymentService.Data.PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new PaymentService.Data.PaymentDbContext(dbOptions))
        {
            var payment = new Payment
            {
                Id = 1,
                BookingId = 1,
                Amount = 5000,
                Status = PaymentStatus.Success,
                CreatedAt = DateTime.UtcNow
            };

            context.Payments.Add(payment);
            await context.SaveChangesAsync();
        }

        using (var context = new PaymentService.Data.PaymentDbContext(dbOptions))
        {
            var repository = new PaymentRepository(context);

            // Act
            var result = await repository.GetByBookingIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result?.BookingId.Should().Be(1);
            result?.Amount.Should().Be(5000);
        }
    }
}

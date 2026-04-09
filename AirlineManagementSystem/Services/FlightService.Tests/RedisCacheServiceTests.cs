using FlightService.Caching;
using Moq;
using StackExchange.Redis;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Text.Json;

namespace FlightService.Tests;

/// <summary>
/// Unit tests for RedisCacheService
/// Tests verify proper error handling and logging when Redis operations succeed or fail
/// </summary>
public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly RedisCacheService _cacheService;

    public RedisCacheServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();
        _cacheService = new RedisCacheService(_mockRedis.Object, _mockLogger.Object);
    }

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ShouldReturnValue_WhenKeyExists()
    {
        // Arrange
        var key = "flight:1";
        var testData = new FlightTestData { Id = 1, Name = "AI101" };
        var serialized = JsonSerializer.Serialize(testData);
        var redisValue = new RedisValue(serialized);

        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(d => d.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(redisValue);

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        // Act
        var result = await _cacheService.GetAsync<FlightTestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("AI101");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyNotFound()
    {
        // Arrange
        var key = "flight:999";
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(d => d.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        // Act
        var result = await _cacheService.GetAsync<FlightTestData>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenRedisThrowsException()
    {
        // Arrange
        var key = "flight:1";
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(d => d.StringGetAsync(key, CommandFlags.None))
            .ThrowsAsync(new Exception("Redis connection failed"));

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        // Act
        var result = await _cacheService.GetAsync<FlightTestData>(key);

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ShouldReturnTrue_WhenKeyRemoved()
    {
        // Arrange
        var key = "flight:1";
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(d => d.KeyDeleteAsync(key, CommandFlags.None))
            .ReturnsAsync(true);

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        // Act
        var result = await _cacheService.RemoveAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "flight:999";
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(d => d.KeyDeleteAsync(key, CommandFlags.None))
            .ReturnsAsync(false);

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        // Act
        var result = await _cacheService.RemoveAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_ShouldReturnFalse_WhenRedisThrowsException()
    {
        // Arrange
        var key = "flight:1";
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase.Setup(d => d.KeyDeleteAsync(key, CommandFlags.None))
            .ThrowsAsync(new Exception("Redis connection failed"));

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        // Act
        var result = await _cacheService.RemoveAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

// Test data class to support JSON serialization tests
public class FlightTestData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

using GuardMetrics.Controllers;
using GuardMetrics.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GuardMetrics.Tests;

public class HealthControllerTests
{
    private readonly Mock<ILogger<HealthController>> _loggerMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _redisDbMock;
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly Mock<DatabaseFacade> _databaseMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _loggerMock = new Mock<ILogger<HealthController>>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _redisDbMock = new Mock<IDatabase>();
        _dbContextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        _databaseMock = new Mock<DatabaseFacade>(_dbContextMock.Object);

        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDbMock.Object);

        _dbContextMock.Setup(x => x.Database)
            .Returns(_databaseMock.Object);

        _controller = new HealthController(_loggerMock.Object, _redisMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task Get_WhenAllServicesHealthy_ReturnsOkResult()
    {
        // Arrange
        _redisDbMock.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(1));

        _databaseMock.Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var healthStatus = Assert.IsType<HealthController.HealthStatus>(okResult.Value);
        
        Assert.Equal("Healthy", healthStatus.Status);
        Assert.Equal(2, healthStatus.Components.Count);
        Assert.Equal("Healthy", healthStatus.Components["redis"].Status);
        Assert.Equal("Healthy", healthStatus.Components["postgresql"].Status);
    }

    [Fact]
    public async Task Get_WhenRedisUnhealthy_ReturnsPartiallyHealthyResult()
    {
        // Arrange
        _redisDbMock.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Test error"));

        _databaseMock.Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var healthStatus = Assert.IsType<HealthController.HealthStatus>(okResult.Value);
        
        Assert.Equal("Unhealthy", healthStatus.Status);
        Assert.Equal("Unhealthy", healthStatus.Components["redis"].Status);
        Assert.Equal("Healthy", healthStatus.Components["postgresql"].Status);
    }

    [Fact]
    public async Task Get_WhenPostgresUnhealthy_ReturnsPartiallyHealthyResult()
    {
        // Arrange
        _redisDbMock.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(1));

        _databaseMock.Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var healthStatus = Assert.IsType<HealthController.HealthStatus>(okResult.Value);
        
        Assert.Equal("Unhealthy", healthStatus.Status);
        Assert.Equal("Healthy", healthStatus.Components["redis"].Status);
        Assert.Equal("Unhealthy", healthStatus.Components["postgresql"].Status);
    }

    [Fact]
    public async Task Get_WhenAllServicesUnhealthy_ReturnsUnhealthyResult()
    {
        // Arrange
        _redisDbMock.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis error"));

        _databaseMock.Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var healthStatus = Assert.IsType<HealthController.HealthStatus>(okResult.Value);
        
        Assert.Equal("Unhealthy", healthStatus.Status);
        Assert.Equal("Unhealthy", healthStatus.Components["redis"].Status);
        Assert.Equal("Unhealthy", healthStatus.Components["postgresql"].Status);
    }
} 
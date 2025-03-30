using GuardMetrics.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace GuardMetrics.Tests;

public class AuthControllerTests
{
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _loggerMock = new Mock<ILogger<AuthController>>();
        _configMock = new Mock<IConfiguration>();

        // Настраиваем конфигурацию для JWT
        _configMock.Setup(x => x["JWT:Secret"]).Returns("your_test_secret_key_minimum_16_chars_long");
        _configMock.Setup(x => x["JWT:ValidIssuer"]).Returns("http://test.com");
        _configMock.Setup(x => x["JWT:ValidAudience"]).Returns("http://test.com");

        _controller = new AuthController(_configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GetToken_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new AuthController.AuthRequest
        {
            ApiKey = "test_api_key",
            AgentId = "test_agent"
        };

        // Act
        var result = _controller.GetToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic tokenResponse = okResult.Value;
        Assert.NotNull(tokenResponse.Token);
        
        // Проверяем, что токен действительно является JWT
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadToken(tokenResponse.Token.ToString()) as JwtSecurityToken;
        
        Assert.NotNull(token);
        Assert.Equal("test_agent", token.Claims.First(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name").Value);
        Assert.Equal("test_api_key", token.Claims.First(c => c.Type == "ApiKey").Value);
    }

    [Theory]
    [InlineData("", "agent")]
    [InlineData("key", "")]
    [InlineData("", "")]
    public void GetToken_WithInvalidRequest_ReturnsBadRequest(string apiKey, string agentId)
    {
        // Arrange
        var request = new AuthController.AuthRequest
        {
            ApiKey = apiKey,
            AgentId = agentId
        };

        // Act
        var result = _controller.GetToken(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetToken_WhenConfigurationMissing_ThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AuthController>>();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["JWT:Secret"]).Returns((string)null);
        var controller = new AuthController(configMock.Object, loggerMock.Object);
        var request = new AuthController.AuthRequest
        {
            ApiKey = "test_api_key",
            AgentId = "test_agent"
        };

        // Act & Assert
        var result = controller.GetToken(request);
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 
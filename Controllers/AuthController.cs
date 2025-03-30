using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GuardMetrics.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public class AuthRequest
    {
        public string ApiKey { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
    }

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("token")]
    public IActionResult GetToken([FromBody] AuthRequest request)
    {
        try
        {
            // В реальном приложении здесь должна быть проверка API ключа в базе данных
            if (string.IsNullOrEmpty(request.ApiKey) || string.IsNullOrEmpty(request.AgentId))
            {
                return BadRequest("API Key и Agent ID обязательны");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, request.AgentId),
                    new Claim("ApiKey", request.ApiKey)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["JWT:ValidIssuer"],
                Audience = _configuration["JWT:ValidAudience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("Generated token for agent: {AgentId}", request.AgentId);

            return Ok(new
            {
                Token = tokenString,
                ExpiresAt = tokenDescriptor.Expires
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token");
            return StatusCode(500, "Internal server error");
        }
    }
} 
using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Controllers;

/// <summary>
/// Endpoint công khai để client kiểm tra server (tùy chọn).
/// URL ngrok chính vẫn nên cập nhật qua discovery.url khi dùng ngrok free.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("ping")]
    public IActionResult Ping() =>
        Ok(new { ok = true, time = DateTime.UtcNow });

    [HttpGet("client")]
    public IActionResult GetClientConfig()
    {
        var apiBaseUrl = _configuration["ClientPublish:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            return Ok(new
            {
                message = "Chưa cấu hình ClientPublish:ApiBaseUrl. Dùng discovery.url hoặc server.url trên client."
            });
        }

        return Ok(new { apiBaseUrl });
    }
}

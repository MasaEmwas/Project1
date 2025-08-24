using Microsoft.AspNetCore.Mvc;
using Project1.Services;

namespace Project1.Controller;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokens;
    public AuthController(TokenService tokens) => _tokens = tokens;

    public record LoginDto(string Username, string Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        var user = Project1.Services.UserStore.Find(dto.Username);
        if (user is null || !Project1.Services.UserStore.CheckPassword(user, dto.Password))
            return Unauthorized(new { message = "Invalid username or password" });

        var token = _tokens.Create(user.Username, user.Role);
        return Ok(new { token, role = user.Role, username = user.Username });
    }
}

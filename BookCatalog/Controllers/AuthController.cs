using Microsoft.AspNetCore.Mvc;
using BookCatalog.Services;

namespace BookCatalog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;
    public AuthController(TokenService tokenService) => _tokenService = tokenService;

    public record LoginDto(string Username, string Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto loginDto)
    {
        var user = UserStore.Find(loginDto.Username);
        if (user is null || !UserStore.CheckPassword(user, loginDto.Password))
            return Unauthorized(new { message = "Invalid username or password" });

        var token = _tokenService.Create(user.Username, user.Role);
        return Ok(new { token, role = user.Role, username = user.Username });
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Project1.Services;

public class TokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public string Create(string username, string role)
    {
        var jwt = _config.GetSection("Jwt"); 
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)); //take the key from the app settings
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); //header algorithm to sign the token using the secret key

        var claims = new List<Claim> 
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

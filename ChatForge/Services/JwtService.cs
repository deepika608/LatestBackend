using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService
{
    private readonly string _key;

    public JwtService(IConfiguration config)
    {
        _key = config["Jwt:Key"];
    }

    public string GenerateToken(string userEmail) // 🔥 use email
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userEmail) // 🔥 IMPORTANT FIX
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
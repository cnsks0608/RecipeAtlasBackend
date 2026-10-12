using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RecipeAtlas.Api.Models;

namespace RecipeAtlas.Api.Services.Auth;

// Kullanıcı için JWT üretir. Ayarlar appsettings.json'dan, gizli anahtar user-secrets'tan okunur.
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key ayarı bulunamadı.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var descriptor = new SecurityTokenDescriptor
        {
            // Token'ın içine giren kullanıcı bilgileri. Gizli bilgi (şifre vb.) konmaz.
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),  // ClaimTypes .Net in hazır bir sabit listesi 
                // new Claim("username", user.Username), -> kendi belirlediğimiz adı kullanmak isteseydik bu şekilde yapacaktık 
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:ExpireDays")),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

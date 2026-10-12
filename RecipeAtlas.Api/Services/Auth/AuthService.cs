using Microsoft.AspNetCore.Identity;
using RecipeAtlas.Api.Data;
using RecipeAtlas.Api.DTOs.Auth;
using RecipeAtlas.Api.Models;
using Microsoft.EntityFrameworkCore;
using RecipeAtlas.Api.Enums;
using RecipeAtlas.Api.Exceptions;


namespace RecipeAtlas.Api.Services.Auth;

// Kayıt ve giriş işlemlerinin iş mantığı burada.
public class AuthService : IAuthService
{
    // Veritabanına erişim.
    private readonly AppDbContext _context;

    // Token üretimi.
    private readonly IJwtService _jwtService;

    // appsettings.json'daki ayarları okumak için (silinen hesabın geri alınma süresi gibi).
    private readonly IConfiguration _configuration;

    // Şifreyi hash'leyen ve doğrulayan hazır .NET aracı.
    private readonly PasswordHasher<User> _passwordHasher = new();

    // Bu üç nesneyi .NET kendisi verir (Program.cs'te kayıtlı oldukları için).
    public AuthService(AppDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<string> LoginAsync(LoginDto loginDto)
    {
        // boşlukları kırp, küçük harfe çevir. Kayıtta kurallar zaten katı olduğu için veritabanındaki veri temiz.
        var input = loginDto.EmailOrUsername.Trim().ToLowerInvariant();

        // E-posta ya da kullanıcı adı eşleşen kullanıcıyı bul.
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == input || u.Username == input);

        if (user is null)
        {
            // HATA 1: Kullanıcı bulunamadı (eşleşen mail veya username yok)
            throw new UnauthorizedException("E-posta/kullanıcı adı veya şifre hatalı.");
    
        }

        // Yazılan şifreyi kayıtlı hash ile karşılaştır. Şifreye Trim ya da küçük harf uygulanmaz.
        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);

        if (passwordResult == PasswordVerificationResult.Failed) // .Net in içinde hazır
        {
            // HATA 2: Şifre yanlış
            throw new UnauthorizedException("E-posta/kullanıcı adı veya şifre hatalı.");
        }


        if (user.Status == UserStatus.Deleted)
        {
            var graceDays = _configuration.GetValue<int>("Account:DeletionGraceDays"); // appsettings.json'dan o ayar okunur

            // Süre dolduysa hesap artık yok sayılır: kullanıcı bulunamamış gibi AYNI genel hata.
            if (user.DeletedAt is null || user.DeletedAt.Value.AddDays(graceDays) < DateTime.UtcNow)
            {
                // HATA 3: Hesap silinmiş ve geri alma süresi dolmuş
                throw new UnauthorizedException("E-posta/kullanıcı adı veya şifre hatalı.");
            }

            // Süre dolmadı: silme iptal edilir, hesap yeniden aktif olur.
            user.Status = UserStatus.Active;
            user.DeletedAt = null;
            await _context.SaveChangesAsync();
        }

        // Tüm kontroller geçti: kullanıcı için token üret ve döndür.
        return _jwtService.GenerateToken(user);

    }
}

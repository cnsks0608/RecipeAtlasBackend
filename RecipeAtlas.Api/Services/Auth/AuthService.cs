using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using RecipeAtlas.Api.Data;
using RecipeAtlas.Api.DTOs.Auth;
using RecipeAtlas.Api.Models;
using Microsoft.EntityFrameworkCore;
using RecipeAtlas.Api.Enums;
using RecipeAtlas.Api.Exceptions;
using RecipeAtlas.Api.Services.Email;


namespace RecipeAtlas.Api.Services.Auth;

// Kayıt ve giriş işlemlerinin iş mantığı burada.
public class AuthService : IAuthService
{
    // Veritabanına erişim.
    private readonly AppDbContext _context;

    // Token üretimi.
    private readonly IJwtService _jwtService;

    // Doğrulama kodunu e-postayla göndermek için.
    private readonly IEmailService _emailService;

    // appsettings.json'daki ayarları okumak için (silinen hesabın geri alınma süresi gibi).
    private readonly IConfiguration _configuration;

    // Şifreyi hash'leyen ve doğrulayan hazır .NET aracı.
    private readonly PasswordHasher<User> _passwordHasher = new();

    // Bu dört nesneyi .NET kendisi verir (Program.cs'te kayıtlı oldukları için).
    public AuthService(AppDbContext context, IJwtService jwtService, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task RegisterAsync(RegisterDto registerDto)
    {
        // Kayıtta kurallar validator'da katı olduğu için e-posta ve kullanıcı adı zaten temiz (küçük harf, boşluksuz) geliyor.
        // Bu e-postayla kayıtlı bir kullanıcı var mı? (silinmiş hesaplar da (30 gün süresi dolmamış) Users'ta durduğu için onlar da sayılır)
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == registerDto.Email);

        if (emailTaken)
        {
            // REGISTER HATA 1: E-posta zaten kayıtlı
            throw new ConflictException("Bu e-posta zaten kayıtlı. Lütfen başka bir e-posta kullanın.");
        }

        // Bu kullanıcı adıyla kayıtlı bir kullanıcı var mı?
        var usernameTaken = await _context.Users.AnyAsync(u => u.Username == registerDto.Username);

        if (usernameTaken)
        {
            // REGISTER HATA 2: Kullanıcı adı zaten alınmış
            throw new ConflictException("Bu kullanıcı adı zaten kayıtlı. Lütfen başka bir kullanıcı adı seçin.");
        }

        var passwordHash = _passwordHasher.HashPassword(new User(), registerDto.Password);

        // 000000 ile 999999 arası rastgele kod. RandomNumberGenerator güvenlik için yapılmış, tahmin edilemez sayı üretir.
        // "D6" baştaki sıfırları korur: 42 -> 000042
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        var expiryMinutes = _configuration.GetValue<int>("Verification:CodeExpiryMinutes");

        // Bu e-posta için bekleyen bir kayıt var mı? Varsa yeni satır eklemeyip aynısını güncelleriz (e-posta başına tek satır).
        var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == registerDto.Email);

        if (pending is null)
        {
            pending = new PendingRegistration { Id = Guid.NewGuid(), Email = registerDto.Email };
            _context.PendingRegistrations.Add(pending);
        }

        // Yeni ya da var olan kaydı en son istekteki bilgilerle doldur. Yanlış deneme sayısı sıfırlanır.
        pending.Username = registerDto.Username;
        pending.PasswordHash = passwordHash;
        pending.VerificationCode = code;
        pending.ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        pending.FailedAttempts = 0;

        await _context.SaveChangesAsync();

        // Önce kayıt yazılır, sonra e-posta gönderilir. E-posta giderken hata çıkarsa kullanıcı aynı bilgilerle tekrar kayıt olur, kayıt güncellenir ve yeni kod gönderilir.
        await _emailService.SendVerificationCodeAsync(registerDto.Email, code);
    }

    public async Task<string> VerifyEmailAsync(VerifyEmailDto verifyEmailDto)
    {
        // Girişteki gibi hoşgörülü: boşlukları kırp, küçük harfe çevir.
        var email = verifyEmailDto.Email.Trim().ToLowerInvariant();

        // Bu e-posta için bekleyen kayıt var mı?
        var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email);

        if (pending is null)
        {
            // VERIFY HATA 1: Bu e-posta için bekleyen kayıt yok
            throw new BadRequestException("Geçerli bir doğrulama kodu girin.");
        }

        // Çok fazla yanlış deneme yapıldıysa kayıt kilitlenir: doğru kod bile artık kabul edilmez. Kullanıcı tekrar kayıt olunca sayı sıfırlanır.
        var maxAttempts = _configuration.GetValue<int>("Verification:MaxFailedAttempts");

        if (pending.FailedAttempts >= maxAttempts)
        {
            // VERIFY HATA 2: Deneme sınırı aşıldı.
            throw new BadRequestException("Çok fazla yanlış deneme yaptınız. Lütfen tekrar kayıt olun.");
        }

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            // VERIFY HATA 3: Kodun süresi dolmuş.
            throw new BadRequestException("Doğrulama kodunun süresi dolmuş. Lütfen tekrar kayıt olun.");
        }

        if (pending.VerificationCode != verifyEmailDto.Code)
        {
            // Yanlış denemeyi sayarız. Hatayı fırlatmadan ÖNCE kaydetmezsek sayaç hiç artmaz.
            pending.FailedAttempts++;
            await _context.SaveChangesAsync();

            // VERIFY HATA 4: Kod yanlış.
            throw new BadRequestException("Doğrulama kodu hatalı. Lütfen tekrar deneyin.");
        }

        // Kod doğru. Ama kayıt ile doğrulama arasında başka biri bu e-postayı ya da kullanıcı adını almış olabilir, son bir kez bakarız
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == pending.Email);

        if (emailTaken)
        {
            // VERIFY HATA 5: E-posta bu arada alınmış. Çakışma hatası atılacak (sonra yazılacak).
            throw new ConflictException("Bu e-posta zaten kayıtlı. Lütfen başka bir e-posta kullanın.");
        }

        var usernameTaken = await _context.Users.AnyAsync(u => u.Username == pending.Username);

        if (usernameTaken)
        {
            // VERIFY HATA 6: Kullanıcı adı bu arada alınmış. Çakışma hatası atılacak (sonra yazılacak).
            throw new ConflictException("Bu kullanıcı adı zaten kayıtlı. Lütfen başka bir kullanıcı adı seçin.");
        }

        // Gerçek kullanıcıyı oluştur. Status (Active) ve CreatedAt (şimdi) User sınıfındaki varsayılan değerlerden gelir.
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = pending.Username,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash
        };

        // Kullanıcıyı ekle, bekleyen kaydı sil, ikisini TEK SaveChanges ile kaydet: ya ikisi de olur ya da hiçbiri.
        _context.Users.Add(user);
        _context.PendingRegistrations.Remove(pending);
        await _context.SaveChangesAsync();

        // Otomatik giriş: kullanıcı için token üret ve döndür.
        return _jwtService.GenerateToken(user);
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

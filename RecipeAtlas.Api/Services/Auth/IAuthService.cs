using RecipeAtlas.Api.DTOs.Auth;

namespace RecipeAtlas.Api.Services.Auth;

// Kayıt ve giriş işlemlerinin sözleşmesi
public interface IAuthService
{
    // Kayıt isteğini alır, bekleyen kayıt oluşturur ve e-postaya 6 haneli doğrulama kodu gönderir.
    // E-posta ya da kullanıcı adı zaten alınmışsa exception fırlatır.
    Task RegisterAsync(RegisterDto registerDto);

    // E-postaya gelen kodu doğrular. Doğruysa gerçek kullanıcıyı oluşturur ve giriş yapmış sayar, JWT döner.
    // Kod yanlışsa, süresi dolmuşsa ya da çok fazla yanlış deneme yapılmışsa exception fırlatır.
    Task<string> VerifyEmailAsync(VerifyEmailDto verifyEmailDto);

    
    // E-posta ya da kullanıcı adı + şifre ile giriş yapar, başarılıysa JWT döner.
    // Başarısızsa exception fırlatır 
    Task<string> LoginAsync(LoginDto loginDto);
}

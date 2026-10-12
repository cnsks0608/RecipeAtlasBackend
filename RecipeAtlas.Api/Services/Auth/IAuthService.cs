using RecipeAtlas.Api.DTOs.Auth;

namespace RecipeAtlas.Api.Services.Auth;

// Kayıt ve giriş işlemlerinin sözleşmesi
public interface IAuthService
{
    // E-posta ya da kullanıcı adı + şifre ile giriş yapar, başarılıysa JWT döner.
    // Başarısızsa exception fırlatır 
    Task<string> LoginAsync(LoginDto loginDto);
}

namespace RecipeAtlas.Api.DTOs.Auth;

// E-posta doğrulama kodunu girerken kullanıcıdan gelen veriyi taşır.
// Kurallar (kod 6 haneli mi vb.) Validators klasöründe olacak.
public class VerifyEmailDto
{
    // Kayıt olurken yazılan e-posta
    // kullanıcı ekranda sadece kodu yazar, e-postayı uygulama kendisi ekleyip gönderir
    public string Email { get; set; } = string.Empty;

    // E-postaya gelen 6 haneli kod
    public string Code { get; set; } = string.Empty;
}

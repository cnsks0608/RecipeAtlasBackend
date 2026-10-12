namespace RecipeAtlas.Api.DTOs.Auth;

// Kayıt olurken kullanıcıdan gelen veriyi taşır.
// Kurallar (şifre uzunluğu vb., FluentValidation) Validators klasöründe olacak
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    // Veritabanına yazılmaz Password ile eşleşiyor mu diye kontrol edilir.
    public string ConfirmPassword { get; set; } = string.Empty;
}

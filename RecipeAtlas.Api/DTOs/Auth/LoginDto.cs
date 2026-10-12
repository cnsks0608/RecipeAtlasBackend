namespace RecipeAtlas.Api.DTOs.Auth;

// Giriş yaparken kullanıcıdan gelen veriyi taşır

public class LoginDto
{
    // Kullanıcı buraya e-postasını ya da kullanıcı adını yazar.
    public string EmailOrUsername { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

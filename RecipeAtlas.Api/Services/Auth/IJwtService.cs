using RecipeAtlas.Api.Models;

namespace RecipeAtlas.Api.Services.Auth;

// Kullanıcı için JWT token üreten servisin sözleşmesi
public interface IJwtService
{
    // Verilen kullanıcı için imzalı bir JWT üretir ve metin olarak döndürür
    string GenerateToken(User user);
}

namespace RecipeAtlas.Api.Exceptions;

// Kimlik doğrulanamadığında atılır: kullanıcı yok, şifre yanlış gibi.
// HTTP 401 Unauthorized cevabına çevrilecek.
public class UnauthorizedException : Exception
{
    // message kullanıcıya gösterilecek açıklama olur.
    public UnauthorizedException(string message) : base(message)
    {
    }
}

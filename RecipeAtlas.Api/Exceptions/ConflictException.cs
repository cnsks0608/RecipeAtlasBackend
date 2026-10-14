namespace RecipeAtlas.Api.Exceptions;

// Çakışma olduğunda atılır: örneğin e-posta ya da kullanıcı adı zaten alınmışsa.
// HTTP 409 Conflict cevabına çevrilecek.
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

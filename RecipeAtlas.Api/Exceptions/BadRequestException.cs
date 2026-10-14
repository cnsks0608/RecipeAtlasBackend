namespace RecipeAtlas.Api.Exceptions;

// İstek geçersiz olduğunda atılır: örneğin doğrulama kodu yanlış, süresi dolmuş ya da deneme sınırı aşılmışsa.
// HTTP 400 Bad Request cevabına çevrilecek.
public class BadRequestException : Exception
{
    // message kullanıcıya gösterilecek açıklama olur.
    public BadRequestException(string message) : base(message)
    {
    }
}

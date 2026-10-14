namespace RecipeAtlas.Api.Services.Email;

public interface IEmailService
{
    // Kayıt olan kullanıcıya 6 haneli doğrulama kodunu e-postayla gönderir
    Task SendVerificationCodeAsync(string toEmail, string code);
}

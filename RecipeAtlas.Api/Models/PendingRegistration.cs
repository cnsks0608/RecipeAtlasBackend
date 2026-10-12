namespace RecipeAtlas.Api.Models;

// E-postası henüz doğrulanmamış kayıt isteklerinin bekleme odası.
// Kod doğru girilince buradaki bilgilerden gerçek bir User oluşturulur.
public class PendingRegistration
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Şifre düz tutulmaz, kayıt anında hash'lenmiş hâli yazılır.
    public string PasswordHash { get; set; } = string.Empty;

    // E-postaya gönderilen 6 haneli kod. Kısa ömürlü ve deneme sınırlı olduğu için hashlemeden sakladık 
    public string VerificationCode { get; set; } = string.Empty;

    // Kodun geçerlilik bitişi (UTC).
    public DateTime ExpiresAt { get; set; }

    // Yanlış kod denemesi sayısı. Sınırı aşınca kod geçersiz sayılır.
    public int FailedAttempts { get; set; }
}

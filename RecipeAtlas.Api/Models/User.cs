using RecipeAtlas.Api.Enums;
namespace RecipeAtlas.Api.Models;
// Veritabanındaki "Users" tablosunun C# karşılığı.
// EF Core bu sınıfa bakarak tabloyu ve sütunlarını oluşturacak.
public class User
{
    // Guid: tahmin edilemez, sıra numarası dışarı sızmaz.
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Veritabanında dosya değil sadece URL tutulur. Fotoğraf yoksa null.
    public string? ProfilePhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hesabın durumu. Soft delete burada: kayıt silinmez, Status = Deleted olur.
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    // Hesap silinmişse silinme zamanı burada tutulur (İnstagramdaki gibi hesap silinince öncelikle soft delete olur belli bir süre geçtikten sonra kayıt kalıcı olarak silinir) Null ise silinmemiştir.
    public DateTime? DeletedAt { get; set; }
}

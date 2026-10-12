using Microsoft.EntityFrameworkCore;
using RecipeAtlas.Api.Models;


namespace RecipeAtlas.Api.Data;

// Uygulamanın veritabanıyla konuşan ana sınıfı.
// Her DbSet<T> veritabanında bir tabloya karşılık gelir.
public class AppDbContext : DbContext
{
    // Bağlantı bilgisi (connection string) dışarıdan gelecek.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();

    // Tabloların ayrıntılı kuralları (sütun uzunluğu, benzersizlik vb.) burada tanımlanır.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.ProfilePhotoUrl)
                .HasMaxLength(500);

            // Enum'ı veritabanında sayı (1,2) yerine Active, Deleted gibi okunaklı metin olarak sakla.
            entity.Property(u => u.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Aynı e-posta ya da kullanıcı adıyla ikinci kayıt olamaz.
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<PendingRegistration>(entity =>
        {
            entity.Property(p => p.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.PasswordHash)
                .IsRequired();

            // 6 haneli kod.
            entity.Property(p => p.VerificationCode)
                .IsRequired()
                .HasMaxLength(6);

            // Aynı e-postayla tekrar kayıt denenirse yeni satır eklenmez, mevcut satır güncellenir.
            // Username'de benzersizlik bilerek yok: ilk doğrulayan kazanır.
            entity.HasIndex(p => p.Email).IsUnique(); // Email sütununda aynı değerden iki satır olamaz. İkinci bir satır eklemeye çalışırsan PostgreSQL hata verir.
        });
    }
}

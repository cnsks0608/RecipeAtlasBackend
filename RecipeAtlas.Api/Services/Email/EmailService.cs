using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RecipeAtlas.Api.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        // Sunucu ve port appsettings.json'dan, kullanıcı adı ve şifre user-secrets'tan gelir
        var host = _configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("Smtp:Host ayarı bulunamadı.");
        var port = _configuration.GetValue<int>("Smtp:Port");
        var username = _configuration["Smtp:Username"]
            ?? throw new InvalidOperationException("Smtp:Username ayarı bulunamadı.");
        var password = _configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("Smtp:Password ayarı bulunamadı.");
        var fromName = _configuration["Smtp:FromName"] ?? "RecipeAtlas";

        // E-postanın başlığı ve metni appsettings.json'daki Email bölümünden gelir
        var subject = _configuration["Email:VerificationSubject"]
            ?? throw new InvalidOperationException("Email:VerificationSubject ayarı bulunamadı.");
        var bodyTemplate = _configuration["Email:VerificationBody"]
            ?? throw new InvalidOperationException("Email:VerificationBody ayarı bulunamadı.");

        // Metindeki {Code} ve {ExpiryMinutes} işaretlerinin yerine gerçek değerleri yaz
        var expiryMinutes = _configuration.GetValue<int>("Verification:CodeExpiryMinutes");
        var body = bodyTemplate
            .Replace("{Code}", code)
            .Replace("{ExpiryMinutes}", expiryMinutes.ToString());

        // Gmail'de gönderen adres her zaman hesabın kendi adresidir, o yüzden Username kullanılır.
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, username));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        // StartTls: bağlantı önce açılır, sonra şifrelenir. Gmail'in 587 portu böyle çalışır.
        using var client = new SmtpClient();


        // Sertifika iptal kontrolü (revocation) bazı ağlarda ve macOS'ta tamamlanamıyor, bağlantı reddediliyor.
        // Kapatınca sertifikanın geçerliliği ve imzası yine kontrol edilir, sadece "iptal listesi" sorgusu atlanır.
        client.CheckCertificateRevocation = false;
        // bu kısım olmadığı için hata aldım

        
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

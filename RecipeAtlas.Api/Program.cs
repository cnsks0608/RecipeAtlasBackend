using Microsoft.EntityFrameworkCore;
using RecipeAtlas.Api.Data;
using RecipeAtlas.Api.Services.Auth;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RecipeAtlas.Api.Middleware;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// AppDbContext'i PostgreSQL'e bağla. Bağlantı bilgisi user-secrets'tan gelir.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
// Projedeki tüm validator'ları (RegisterDtoValidator, LoginDtoValidator...) otomatik bulup kaydeder
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Controller sınıflarını uygulamaya tanıtır.
builder.Services.AddControllers();
// Beklenmeyen hataları yakalayan sınıfı kaydeder.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// Giriş endpoint'i için hız sınırı: aynı IP, belirlenen sürede belirlenen sayıdan fazla istek atamaz.
// Değerler appsettings.json'daki RateLimit bölümünden gelir.
var loginPermitLimit = builder.Configuration.GetValue<int>("RateLimit:LoginPermitLimit");
var loginWindowSeconds = builder.Configuration.GetValue<int>("RateLimit:LoginWindowSeconds");

builder.Services.AddRateLimiter(options =>
{
    // "login" adlı politika: IP adresine göre sayar, sınırı aşan istek reddedilir (kuyrukta bekletilmez).
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromSeconds(loginWindowSeconds),
                QueueLimit = 0
            }));

    // Sınır aşılınca dönecek cevap: 429 ve diğer hata cevaplarımızla aynı biçimde { "message": "..." }.
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Çok fazla deneme yaptınız. Lütfen biraz bekleyip tekrar deneyin." },
            cancellationToken);
    };
});


// Gelen JWT'nin nasıl doğrulanacağı. Değerler JwtService'in token üretirken kullandıklarıyla aynı olmak zorunda.
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"]
    ?? throw new InvalidOperationException("Jwt:Key ayarı bulunamadı.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, // Validate -> bunları kontrol et demek
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) // kontrol edilecek doğru değerler: Issuer, Audience ve imza anahtarı.
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

// Hatanın yakalanmasını sağlar. Sırada en başta olmalı ki alttaki her şeyde çıkan hatayı yakalayabilsin
// İstek hattı yukarıdan aşağı çalışır, hata ise aşağıdan yukarı yükselir. UseExceptionHandler en üstte olursa, altındaki her şeyin (rate limiter, kimlik doğrulama, controller) fırlattığı hatayı yakalar.
app.UseExceptionHandler(_ => { });


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Hız sınırı kontrolü. Kimlik doğrulamadan önce çalışması iyi: saldırgan token kontrolüne bile ulaşamasın.
app.UseRateLimiter();

// Sıra önemli: önce kimlik doğrulama (token'dan kullanıcıyı tanı), sonra yetkilendirme (yapabilir mi?)
app.UseAuthentication();
app.UseAuthorization();

// Gelen istekleri controller'lara yönlendirir
app.MapControllers();



app.Run();

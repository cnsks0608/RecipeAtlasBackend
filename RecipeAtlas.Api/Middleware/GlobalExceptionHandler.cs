using Microsoft.AspNetCore.Diagnostics;

namespace RecipeAtlas.Api.Middleware;

// Hiçbir yerde yakalanmayan beklenmeyen hataların son durağı (veritabanı bağlantısı koptu, kodda hata çıktı gibi)
// Detayı log'a yazar, kullanıcıya sade bir 500 cevabı döner.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // Hatayı log'a yazabilmek için .NET'in hazır logger'ı istenir.
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Hatanın tüm detayı (yığın izi dahil) sadece log'a gider, kullanıcıya gösterilmez.
        _logger.LogError(exception, "Beklenmeyen hata: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { message = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin." },
            cancellationToken);

        // true: "hatayı ben işledim, başka kimse uğraşmasın" demek.
        return true;
    }
}

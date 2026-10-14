using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RecipeAtlas.Api.DTOs.Auth;
using RecipeAtlas.Api.Services.Auth;
using RecipeAtlas.Api.Exceptions;
using Microsoft.AspNetCore.RateLimiting;



namespace RecipeAtlas.Api.Controllers;

// Kayıt ve giriş endpoint'leri. İş mantığı serviste, burası sadece isteği alıp cevabı döner
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    // Gelen isteklerin biçimini kontrol eden validator'lar
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<VerifyEmailDto> _verifyEmailValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginDto> loginValidator,
        IValidator<RegisterDto> registerValidator,
        IValidator<VerifyEmailDto> verifyEmailValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _verifyEmailValidator = verifyEmailValidator;
    }

    // Kayıt isteğini alır, e-postaya doğrulama kodu gönderir. Hesap, kod doğrulanınca oluşur.
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        // Önce validator kurallarına uyulmuş mu diye bakılır, uyulmamışsa servise hiç gitmez
        var validationResult = await _registerValidator.ValidateAsync(registerDto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(new { errors });
        }

        try
        {
            await _authService.RegisterAsync(registerDto);
            return Ok(new { message = "Lütfen e-postanıza gelen doğrulama kodunu girin." });
        }
        catch (ConflictException ex)
        {
            // E-posta ya da kullanıcı adı zaten alınmış
            return Conflict(new { message = ex.Message });
        }
    }

    // E-postaya gelen kodu doğrular. Doğruysa hesabı oluşturur ve giriş yapmış sayar, JWT döner.
    [HttpPost("verify-email")]
    [EnableRateLimiting("verify")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailDto verifyEmailDto)
    {
        var validationResult = await _verifyEmailValidator.ValidateAsync(verifyEmailDto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(new { errors });
        }

        try
        {
            var token = await _authService.VerifyEmailAsync(verifyEmailDto);
            return Ok(new { token });
        }
        catch (BadRequestException ex)
        {
            // Kod yanlış, süresi dolmuş, kayıt yok ya da deneme sınırı aşılmış
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            // Doğrulama anında e-posta ya da kullanıcı adı başkası tarafından alınmış
            return Conflict(new { message = ex.Message });
        }
    }

    // Giriş yapar, başarılıysa JWT döner.
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        // Önce validator kurallarına uyulmuş mı diye bakılır uyulmamışsa servise hiç gitmez
        var validationResult = await _loginValidator.ValidateAsync(loginDto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(new { errors });
        }

        try
        {
            var token = await _authService.LoginAsync(loginDto);
            return Ok(new { token });
        }
        catch (UnauthorizedException ex)
        {
            // Kullanıcı yok, şifre yanlış ya da hesap yok sayıldı: hepsi için aynı mesaj
            return Unauthorized(new { message = ex.Message });
        }
    }

}

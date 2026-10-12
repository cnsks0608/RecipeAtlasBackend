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

    // Gelen LoginDto'nun biçimini kontrol eden validator
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthController(IAuthService authService, IValidator<LoginDto> loginValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
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

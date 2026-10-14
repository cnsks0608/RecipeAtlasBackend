using FluentValidation;
using RecipeAtlas.Api.DTOs.Auth;

namespace RecipeAtlas.Api.Validators.Auth;

// E-posta doğrulama isteğinin biçimini kontrol eder (alanlar dolu mu, kod 6 haneli rakam mı).
// Kodun doğru olup olmadığını AuthService bekleyen kayıtla karşılaştırarak kontrol edecek.
// E-postanın büyük/küçük harfine burada karışmıyoruz, servis küçük harfe çevirip arayacak.
public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
{
    public VerifyEmailDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .MaximumLength(255).WithMessage("E-posta en fazla 255 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta girin.");

        // baştan sona tam 6 tane rakam demek
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Doğrulama kodu boş olamaz.")
            .Matches(@"^\d{6}$").WithMessage("Doğrulama kodu 6 haneli bir sayı olmalı.");
    }
}

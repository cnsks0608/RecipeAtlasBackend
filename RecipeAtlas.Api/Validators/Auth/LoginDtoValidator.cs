using FluentValidation;
using RecipeAtlas.Api.DTOs.Auth;

namespace RecipeAtlas.Api.Validators.Auth;

// Giriş isteğinde sadece alanların dolu olduğunu kontrol eder
// Şifrenin doğruluğunu AuthService hash karşılaştırmasıyla kontrol edeceğiz
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("E-posta ya da kullanıcı adı boş olamaz.")
            .MaximumLength(255).WithMessage("E-posta ya da kullanıcı adı en fazla 255 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MaximumLength(64).WithMessage("Şifre en fazla 64 karakter olabilir.");
    }
}

using FluentValidation;
using RecipeAtlas.Api.DTOs.Auth;

namespace RecipeAtlas.Api.Validators.Auth;

// Kayıt isteğinin biçimini kontrol eder.
// "Bu e-posta/kullanıcı adı zaten var mı?" kontrolünü AuthService'te yapacağım
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı boş olamaz.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalı.")
            .MaximumLength(20).WithMessage("Kullanıcı adı en fazla 20 karakter olabilir.")
            .Matches("^[a-z0-9_]+$").WithMessage("Kullanıcı adı sadece küçük harf, rakam ve alt çizgi içerebilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .MaximumLength(255).WithMessage("E-posta en fazla 255 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta girin.")
            .Must(email => email == email.ToLowerInvariant()).WithMessage("E-posta sadece küçük harflerden oluşmalı.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.")
            .MaximumLength(64).WithMessage("Şifre en fazla 64 karakter olabilir.");


        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Şifreler eşleşmiyor.");
    }
}

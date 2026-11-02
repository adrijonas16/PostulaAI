using FluentValidation;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.Auth.Contracts;

public static class PasswordRules
{
    public const int MinLength = 10;

    /// <summary>Upper bound prevents hashing extremely long inputs (denial of service).</summary>
    public const int MaxLength = 128;
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Ingresa tu correo electrónico.")
            .MaximumLength(User.EmailMaxLength).WithMessage("El correo es demasiado largo.")
            .EmailAddress().WithMessage("Ingresa un correo electrónico válido.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Ingresa una contraseña.")
            .MinimumLength(PasswordRules.MinLength).WithMessage($"La contraseña debe tener al menos {PasswordRules.MinLength} caracteres.")
            .MaximumLength(PasswordRules.MaxLength).WithMessage($"La contraseña no puede superar {PasswordRules.MaxLength} caracteres.");

        RuleFor(request => request.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Ingresa tu nombre.")
            .MaximumLength(User.DisplayNameMaxLength).WithMessage($"El nombre no puede superar {User.DisplayNameMaxLength} caracteres.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Ingresa tu correo electrónico.")
            .MaximumLength(User.EmailMaxLength).WithMessage("El correo es demasiado largo.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Ingresa tu contraseña.")
            .MaximumLength(PasswordRules.MaxLength).WithMessage("La contraseña es demasiado larga.");
    }
}

using FluentValidation;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Domain.Constants;

namespace ServiceManagement.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.PhoneNumber).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.Role)
            .Must(r => string.IsNullOrWhiteSpace(r)
                || r is Domain.Constants.Roles.CommonUser or Domain.Constants.Roles.ServiceProvider)
            .WithMessage("Self-registration creates a user account. Pass ServiceProvider only if they will also offer services.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

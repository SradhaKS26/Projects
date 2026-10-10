using FluentValidation;
using ServiceManagement.Application.Providers.Dtos;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Application.Providers.Validators;

public class UpdateProviderApplicationRequestValidator : AbstractValidator<UpdateProviderApplicationRequest>
{
    public UpdateProviderApplicationRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ServiceIds)
            .NotNull()
            .Must(ids => ids.Count > 0)
            .WithMessage("Select at least one service to apply for.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Service list contains duplicates.");
    }
}

public class SetAvailabilityRequestValidator : AbstractValidator<SetAvailabilityRequest>
{
    public SetAvailabilityRequestValidator()
    {
        RuleFor(x => x.AvailabilityStatus).IsInEnum();
    }
}

public class ReviewProviderRequestValidator : AbstractValidator<ReviewProviderRequest>
{
    public ReviewProviderRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(2000);
    }
}

public class SetProviderActiveRequestValidator : AbstractValidator<SetProviderActiveRequest>
{
    public SetProviderActiveRequestValidator()
    {
        RuleFor(x => x.IsActive).NotNull();
    }
}

public class RejectProviderRequestValidator : AbstractValidator<RejectProviderRequest>
{
    public RejectProviderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required when rejecting a provider.")
            .MinimumLength(3)
            .MaximumLength(2000);
    }
}

public class SuspendProviderRequestValidator : AbstractValidator<SuspendProviderRequest>
{
    public SuspendProviderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required when suspending a provider.")
            .MinimumLength(3)
            .MaximumLength(2000);
    }
}

using FluentValidation;
using ServiceManagement.Application.Catalog.Dtos;

namespace ServiceManagement.Application.Catalog.Validators;

public class CreateServiceCategoryRequestValidator : AbstractValidator<CreateServiceCategoryRequest>
{
    public CreateServiceCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ImageUrl).MaximumLength(1024);
    }
}

public class UpdateServiceCategoryRequestValidator : AbstractValidator<UpdateServiceCategoryRequest>
{
    public UpdateServiceCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ImageUrl).MaximumLength(1024);
    }
}

public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).LessThan(10_000_000);
        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60 * 24 * 30)
            .When(x => x.EstimatedDurationMinutes.HasValue);
    }
}

public class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).LessThan(10_000_000);
        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60 * 24 * 30)
            .When(x => x.EstimatedDurationMinutes.HasValue);
    }
}

public class CreateDocumentRequirementRequestValidator : AbstractValidator<CreateDocumentRequirementRequest>
{
    public CreateDocumentRequirementRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1000);
    }
}

public class UpdateDocumentRequirementRequestValidator : AbstractValidator<UpdateDocumentRequirementRequest>
{
    public UpdateDocumentRequirementRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1000);
    }
}

using FluentValidation;
using ServiceManagement.Application.Requests.Dtos;

namespace ServiceManagement.Application.Requests.Validators;

public class PlaceServiceRequestValidator : AbstractValidator<PlaceServiceRequest>
{
    public PlaceServiceRequestValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

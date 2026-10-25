using FluentValidation;
using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Domain.Services;

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
        RuleFor(x => x.PassengerCount)
            .InclusiveBetween(FulfillmentRules.MinPassengers, FulfillmentRules.MaxPassengers)
            .When(x => x.PassengerCount.HasValue);
        RuleFor(x => x.ParcelDescription).MaximumLength(500);
        RuleFor(x => x.RecipientName).MaximumLength(150);
        RuleFor(x => x.RecipientPhone).MaximumLength(30);
        When(x => x.Destination is not null, () =>
        {
            RuleFor(x => x.Destination!).SetValidator(new PlaceAddressRequestValidator());
        });
    }
}

public class PlaceAddressRequestValidator : AbstractValidator<PlaceAddressRequest>
{
    public PlaceAddressRequestValidator()
    {
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
    }
}

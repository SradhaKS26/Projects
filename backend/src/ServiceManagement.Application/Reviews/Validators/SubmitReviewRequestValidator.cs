using FluentValidation;
using ServiceManagement.Application.Reviews.Dtos;
using ServiceManagement.Domain.Services;

namespace ServiceManagement.Application.Reviews.Validators;

public class SubmitReviewRequestValidator : AbstractValidator<SubmitReviewRequest>
{
    public SubmitReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(ReviewRules.MinRating, ReviewRules.MaxRating)
            .WithMessage($"Rating must be between {ReviewRules.MinRating} and {ReviewRules.MaxRating}.");
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

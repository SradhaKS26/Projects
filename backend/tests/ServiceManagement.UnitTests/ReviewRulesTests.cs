using FluentAssertions;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Domain.Services;

namespace ServiceManagement.UnitTests;

public class ReviewRulesTests
{
    [Fact]
    public void CanCustomerReview_allows_the_owning_customer_once_after_complete()
    {
        var customerId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        ReviewRules.CanCustomerReview(
                ServiceRequestStatus.Completed,
                customerId,
                customerId,
                providerId,
                alreadyReviewed: false)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(ServiceRequestStatus.Pending)]
    [InlineData(ServiceRequestStatus.Accepted)]
    [InlineData(ServiceRequestStatus.InProgress)]
    [InlineData(ServiceRequestStatus.Failed)]
    [InlineData(ServiceRequestStatus.Cancelled)]
    public void CanCustomerReview_rejects_jobs_that_are_not_completed(ServiceRequestStatus status)
    {
        var customerId = Guid.NewGuid();

        ReviewRules.CanCustomerReview(status, customerId, customerId, Guid.NewGuid(), alreadyReviewed: false)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanCustomerReview_rejects_a_second_review()
    {
        var customerId = Guid.NewGuid();

        ReviewRules.CanCustomerReview(
                ServiceRequestStatus.Completed,
                customerId,
                customerId,
                Guid.NewGuid(),
                alreadyReviewed: true)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanCustomerReview_rejects_anyone_other_than_the_customer()
    {
        ReviewRules.CanCustomerReview(
                ServiceRequestStatus.Completed,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                alreadyReviewed: false)
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void IsValidRating_is_one_through_five(int rating, bool expected)
    {
        ReviewRules.IsValidRating(rating).Should().Be(expected);
    }
}

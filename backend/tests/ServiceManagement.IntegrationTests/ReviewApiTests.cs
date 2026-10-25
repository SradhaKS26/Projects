using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Notifications.Dtos;
using ServiceManagement.Application.Providers.Dtos;
using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Application.Reviews.Dtos;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ReviewApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReviewApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureSeededAsync();
        _factory.Clock.UtcNow = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Customer_reviews_completed_job_and_provider_rating_updates()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin);
        var customer = await RegisterAsync("Casey", Roles.CommonUser);
        var provider = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);
        var requestId = await CompleteJobAsync(customer, provider, serviceId);

        var reviewed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            $"/api/service-requests/{requestId}/review",
            new SubmitReviewRequest(5, "On time and thorough."));

        reviewed.Status.Should().Be(ServiceRequestStatus.Completed);
        reviewed.Review.Should().NotBeNull();
        reviewed.Review!.Rating.Should().Be(5);
        reviewed.Review.Comment.Should().Be("On time and thorough.");
        reviewed.ProviderRating.Should().Be(5);
        reviewed.ProviderReviewCount.Should().Be(1);
        reviewed.Timeline.Should().NotBeNull();
        reviewed.Timeline!.Select(e => e.NewStatus).Should().Contain(ServiceRequestStatus.Completed);

        var duplicate = await SendRawAsync(
            customer,
            HttpMethod.Post,
            $"/api/service-requests/{requestId}/review",
            new SubmitReviewRequest(4, null));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var received = await GetAsync<List<ReviewDto>>(provider, "/api/reviews/received");
        received.Should().ContainSingle(r => r.ServiceRequestId == requestId && r.Rating == 5);

        var notes = await GetAsync<List<UserNotificationDto>>(provider, "/api/notifications");
        notes.Select(n => n.Title).Should().Contain("A customer rated your work");

        var profile = await GetAsync<ProviderProfileDto>(provider, "/api/providers/me");
        profile.Rating.Should().Be(5);
        profile.ReviewCount.Should().Be(1);
    }

    [Fact]
    public async Task Review_is_rejected_before_completion_and_by_non_customers()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin);
        var customer = await RegisterAsync("Casey", Roles.CommonUser);
        var provider = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);

        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            Place(serviceId),
            HttpStatusCode.Created);

        var tooSoon = await SendRawAsync(
            customer,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/review",
            new SubmitReviewRequest(5, null));
        tooSoon.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/accept", payload: null);
        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/start", payload: null);
        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/complete", payload: null);

        var providerReview = await SendRawAsync(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/review",
            new SubmitReviewRequest(5, null));
        providerReview.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var invalid = await SendRawAsync(
            customer,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/review",
            new SubmitReviewRequest(6, "too high"));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Average_rating_uses_every_completed_review()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin);
        var customerA = await RegisterAsync("Casey", Roles.CommonUser);
        var customerB = await RegisterAsync("Riley", Roles.CommonUser);
        var provider = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);

        var first = await CompleteJobAsync(customerA, provider, serviceId);
        await SendAsync<ServiceRequestDto>(
            customerA,
            HttpMethod.Post,
            $"/api/service-requests/{first}/review",
            new SubmitReviewRequest(5, null));

        await SendAsync<ProviderProfileDto>(
            provider,
            HttpMethod.Put,
            "/api/providers/me/availability",
            new SetAvailabilityRequest(AvailabilityStatus.Available));

        var second = await CompleteJobAsync(customerB, provider, serviceId);
        var reviewed = await SendAsync<ServiceRequestDto>(
            customerB,
            HttpMethod.Post,
            $"/api/service-requests/{second}/review",
            new SubmitReviewRequest(3, "Fine."));

        reviewed.ProviderRating.Should().Be(4.00m);
        reviewed.ProviderReviewCount.Should().Be(2);
    }

    private async Task<Guid> CompleteJobAsync(string customer, string provider, Guid serviceId)
    {
        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            Place(serviceId),
            HttpStatusCode.Created);
        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/accept", payload: null);
        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/start", payload: null);
        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/complete", payload: null);
        return placed.Id;
    }

    private static PlaceServiceRequest Place(Guid serviceId) => new(
        serviceId,
        "12 Market Street",
        null,
        "Austin",
        "TX",
        "78701",
        null,
        null,
        "Please arrive on time.");

    private async Task<string> RegisterApprovedAvailableProviderAsync(
        string adminToken,
        Guid serviceId,
        Guid requirementId)
    {
        var token = await RegisterAsync("Pat", Roles.ServiceProvider);
        var applied = await SendAsync<ProviderProfileDto>(
            token,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("Ready.", [serviceId]));
        (await UploadPdfAsync(token, requirementId)).EnsureSuccessStatusCode();
        await SendAsync<ProviderProfileDto>(
            adminToken,
            HttpMethod.Post,
            $"/api/providers/{applied.Id}/approve",
            new ReviewProviderRequest(null));
        await SendAsync<ProviderProfileDto>(
            token,
            HttpMethod.Put,
            "/api/providers/me/availability",
            new SetAvailabilityRequest(AvailabilityStatus.Available));
        return token;
    }

    private async Task<(Guid CategoryId, Guid ServiceId, Guid RequirementId)> SeedCatalogAsync(string adminToken)
    {
        var category = await PostAsync<ServiceCategoryDto>(
            adminToken,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Ride"), "Review tests", null));
        var service = await PostAsync<ServiceDto>(
            adminToken,
            "/api/services",
            new CreateServiceRequest(category.Id, UniqueName("Sedan"), "A bookable service type", 10m, 20));
        var requirement = await PostAsync<CategoryDocumentRequirementDto>(
            adminToken,
            $"/api/service-categories/{category.Id}/document-requirements",
            new CreateDocumentRequirementRequest("Licence", null, true, 0));
        return (category.Id, service.Id, requirement.Id);
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(30, prefix.Length + 33)];

    private async Task<string> AdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@test.local", "TestAdmin!123"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(TestJson.Options);
        return body!.Data!.AccessToken;
    }

    private async Task<string> RegisterAsync(string firstName, string role)
    {
        var email = $"{role}_{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(firstName, "User", email, "Password!123", "5550100", role));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(TestJson.Options);
        return body!.Data!.AccessToken;
    }

    private async Task<HttpResponseMessage> UploadPdfAsync(string token, Guid requirementId)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(requirementId.ToString()), "documentRequirementId");
        var file = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 test"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "doc.pdf");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/providers/me/documents") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<T> GetAsync<T>(string token, string url)
    {
        var response = await SendRawAsync(token, HttpMethod.Get, url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<T> PostAsync<T>(string token, string url, object payload)
    {
        var response = await SendRawAsync(token, HttpMethod.Post, url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<T> SendAsync<T>(
        string token,
        HttpMethod method,
        string url,
        object? payload,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        var response = await SendRawAsync(token, method, url, payload);
        response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        string token,
        HttpMethod method,
        string url,
        object? payload = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload, payload.GetType(), options: TestJson.Options);
        }

        return await _client.SendAsync(request);
    }
}

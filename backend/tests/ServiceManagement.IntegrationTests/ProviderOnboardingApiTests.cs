using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Providers.Dtos;
using ServiceManagement.Application.Providers.Interfaces;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ProviderOnboardingApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProviderOnboardingApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.EnsureSeededAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Registering_a_provider_creates_a_pending_profile()
    {
        var token = await RegisterProviderAsync();

        var profile = await GetAsync<ProviderProfileDto>(token, "/api/providers/me");
        profile.ApprovalStatus.Should().Be(ProviderApprovalStatus.Pending);
        profile.IsActive.Should().BeTrue();
        profile.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);
        profile.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task Customers_cannot_read_a_provider_profile()
    {
        var customer = await RegisterCustomerAsync();
        var response = await SendRawAsync(customer, HttpMethod.Get, "/api/providers/me");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Provider_can_apply_upload_documents_and_admin_can_approve()
    {
        var admin = await AdminTokenAsync();
        var (categoryId, serviceId, requirementId) = await SeedApplicatonCatalogAsync(admin);
        var providerToken = await RegisterProviderAsync();

        var saved = await SendAsync<ProviderProfileDto>(
            providerToken,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("I can fulfill this service.", [serviceId]));
        saved.Services.Should().ContainSingle(s => s.ServiceId == serviceId && s.Status == ProviderServiceStatus.Pending);
        saved.MissingRequiredDocuments.Should().ContainSingle(m => m.Id == requirementId);

        var upload = await UploadPdfAsync(providerToken, requirementId, "licence.pdf");
        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploaded = await upload.Content.ReadFromJsonAsync<ApiResponse<ProviderDocumentDto>>(TestJson.Options);
        uploaded!.Data.Should().NotBeNull();

        var afterUpload = await GetAsync<ProviderProfileDto>(providerToken, "/api/providers/me");
        afterUpload.MissingRequiredDocuments.Should().BeEmpty();
        afterUpload.Documents.Should().ContainSingle();

        var fileResponse = await SendRawAsync(
            providerToken,
            HttpMethod.Get,
            $"/api/provider-documents/{afterUpload.Documents[0].Id}/file");
        fileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fileResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await fileResponse.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(4);

        var stranger = await RegisterProviderAsync();
        var forbiddenFile = await SendRawAsync(
            stranger,
            HttpMethod.Get,
            $"/api/provider-documents/{afterUpload.Documents[0].Id}/file");
        forbiddenFile.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var listed = await GetAsync<List<ProviderListItemDto>>(admin, "/api/providers?approvalStatus=pending");
        listed.Should().Contain(p => p.Id == afterUpload.Id);

        var approved = await SendAsync<ProviderProfileDto>(
            admin,
            HttpMethod.Post,
            $"/api/providers/{afterUpload.Id}/approve",
            new ReviewProviderRequest(null));
        approved.ApprovalStatus.Should().Be(ProviderApprovalStatus.Approved);
        approved.Services.Should().ContainSingle(s => s.Status == ProviderServiceStatus.Approved);

        using (var scope = _factory.Services.CreateScope())
        {
            var onboarding = scope.ServiceProvider.GetRequiredService<IProviderOnboardingService>();
            (await onboarding.IsEligibleForServiceAsync(approved.Id, serviceId))
                .Should().BeFalse("availability is still Unavailable");
        }

        var available = await SendAsync<ProviderProfileDto>(
            providerToken,
            HttpMethod.Put,
            "/api/providers/me/availability",
            new SetAvailabilityRequest(AvailabilityStatus.Available));
        available.AvailabilityStatus.Should().Be(AvailabilityStatus.Available);

        using (var scope = _factory.Services.CreateScope())
        {
            var onboarding = scope.ServiceProvider.GetRequiredService<IProviderOnboardingService>();
            (await onboarding.IsEligibleForServiceAsync(approved.Id, serviceId)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task Reject_requires_a_reason_and_returns_the_provider_to_pending_on_resubmit()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, _) = await SeedApplicatonCatalogAsync(admin);
        var providerToken = await RegisterProviderAsync();

        var applied = await SendAsync<ProviderProfileDto>(
            providerToken,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("Ready to work.", [serviceId]));

        var missingReason = await SendRawAsync(
            admin,
            HttpMethod.Post,
            $"/api/providers/{applied.Id}/reject",
            new RejectProviderRequest(""));
        missingReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var rejected = await SendAsync<ProviderProfileDto>(
            admin,
            HttpMethod.Post,
            $"/api/providers/{applied.Id}/reject",
            new RejectProviderRequest("Documents are illegible."));
        rejected.ApprovalStatus.Should().Be(ProviderApprovalStatus.Rejected);
        rejected.ReviewReason.Should().Be("Documents are illegible.");

        var resubmitted = await SendAsync<ProviderProfileDto>(
            providerToken,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("Re-submitted with clearer scans.", [serviceId]));
        resubmitted.ApprovalStatus.Should().Be(ProviderApprovalStatus.Pending);
        resubmitted.ReviewReason.Should().BeNull();
    }

    [Fact]
    public async Task Unapproved_provider_cannot_set_availability()
    {
        var token = await RegisterProviderAsync();
        var response = await SendRawAsync(
            token,
            HttpMethod.Put,
            "/api/providers/me/availability",
            new SetAvailabilityRequest(AvailabilityStatus.Available));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customers_cannot_list_providers()
    {
        var customer = await RegisterCustomerAsync();
        var response = await SendRawAsync(customer, HttpMethod.Get, "/api/providers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Document_requirements_are_configured_per_category()
    {
        var admin = await AdminTokenAsync();
        var cooking = await PostAsync<ServiceCategoryDto>(
            admin,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Kitchen"), null, null));
        var driving = await PostAsync<ServiceCategoryDto>(
            admin,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Rides"), null, null));

        await PostAsync<CategoryDocumentRequirementDto>(
            admin,
            $"/api/service-categories/{cooking.Id}/document-requirements",
            new CreateDocumentRequirementRequest("Food Safety Certificate", "Hygiene training", true, 0));

        await PostAsync<CategoryDocumentRequirementDto>(
            admin,
            $"/api/service-categories/{driving.Id}/document-requirements",
            new CreateDocumentRequirementRequest("Driving Licence", null, true, 0));

        var providerToken = await RegisterProviderAsync();
        var cookingReqs = await GetAsync<List<CategoryDocumentRequirementDto>>(
            providerToken,
            $"/api/service-categories/{cooking.Id}/document-requirements");
        cookingReqs.Should().ContainSingle(r => r.Name == "Food Safety Certificate");
        cookingReqs.Should().NotContain(r => r.Name == "Driving Licence");
    }

    [Fact]
    public async Task Suspended_provider_is_not_eligible()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedApplicatonCatalogAsync(admin);
        var providerToken = await RegisterProviderAsync();

        var applied = await SendAsync<ProviderProfileDto>(
            providerToken,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("Ready.", [serviceId]));
        (await UploadPdfAsync(providerToken, requirementId, "doc.pdf")).EnsureSuccessStatusCode();

        await SendAsync<ProviderProfileDto>(
            admin,
            HttpMethod.Post,
            $"/api/providers/{applied.Id}/approve",
            new ReviewProviderRequest(null));
        await SendAsync<ProviderProfileDto>(
            providerToken,
            HttpMethod.Put,
            "/api/providers/me/availability",
            new SetAvailabilityRequest(AvailabilityStatus.Available));

        var suspended = await SendAsync<ProviderProfileDto>(
            admin,
            HttpMethod.Post,
            $"/api/providers/{applied.Id}/suspend",
            new SuspendProviderRequest("Safety review."));
        suspended.ApprovalStatus.Should().Be(ProviderApprovalStatus.Suspended);
        suspended.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);

        using var scope = _factory.Services.CreateScope();
        var onboarding = scope.ServiceProvider.GetRequiredService<IProviderOnboardingService>();
        (await onboarding.IsEligibleForServiceAsync(applied.Id, serviceId)).Should().BeFalse();
    }

    private async Task<(Guid CategoryId, Guid ServiceId, Guid RequirementId)> SeedApplicatonCatalogAsync(string adminToken)
    {
        var category = await PostAsync<ServiceCategoryDto>(
            adminToken,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Onboard"), "Onboarding tests", null));

        var service = await PostAsync<ServiceDto>(
            adminToken,
            "/api/services",
            new CreateServiceRequest(category.Id, UniqueName("Vehicle"), "A bookable service type", 10m, 20));

        var requirement = await PostAsync<CategoryDocumentRequirementDto>(
            adminToken,
            $"/api/service-categories/{category.Id}/document-requirements",
            new CreateDocumentRequirementRequest("Licence", "Required for this category", true, 0));

        return (category.Id, service.Id, requirement.Id);
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(30, prefix.Length + 33)];

    private async Task<string> AdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@test.local", "TestAdmin!123"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(TestJson.Options);
        return body!.Data!.AccessToken;
    }

    private async Task<string> RegisterProviderAsync()
    {
        var email = $"provider_{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Pat", "Provider", email, "Password!123", "5550100", Roles.ServiceProvider));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(TestJson.Options);
        return body!.Data!.AccessToken;
    }

    private async Task<string> RegisterCustomerAsync()
    {
        var email = $"cust_{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Casey", "Customer", email, "Password!123", null, Roles.CommonUser));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(TestJson.Options);
        return body!.Data!.AccessToken;
    }

    private async Task<HttpResponseMessage> UploadPdfAsync(string token, Guid requirementId, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(requirementId.ToString()), "documentRequirementId");
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4 test document");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/providers/me/documents")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<T> GetAsync<T>(string token, string url)
    {
        var response = await SendRawAsync(token, HttpMethod.Get, url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<T> PostAsync<T>(string token, string url, object payload)
    {
        var response = await SendRawAsync(token, HttpMethod.Post, url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<T> SendAsync<T>(string token, HttpMethod method, string url, object payload)
    {
        var response = await SendRawAsync(token, method, url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
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

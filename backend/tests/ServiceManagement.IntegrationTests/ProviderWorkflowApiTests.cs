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
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ProviderWorkflowApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProviderWorkflowApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureSeededAsync();
        _factory.Clock.UtcNow = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Assigned_provider_starts_and_completes_and_customer_is_notified()
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

        var accepted = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/accept",
            payload: null);
        accepted.Status.Should().Be(ServiceRequestStatus.Accepted);

        var inbox = await GetAsync<List<ServiceRequestDto>>(provider, "/api/service-requests/inbox");
        inbox.Should().BeEmpty();

        var assigned = await GetAsync<List<ServiceRequestDto>>(provider, "/api/service-requests/assigned");
        assigned.Should().ContainSingle(r => r.Id == placed.Id && r.Status == ServiceRequestStatus.Accepted);

        var started = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/start",
            payload: null);
        started.Status.Should().Be(ServiceRequestStatus.InProgress);

        var completed = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/complete",
            payload: null);
        completed.Status.Should().Be(ServiceRequestStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();

        var tooLate = await SendRawAsync(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/start");
        tooLate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var notes = await GetAsync<List<UserNotificationDto>>(customer, "/api/notifications");
        notes.Select(n => n.Title).Should().Contain("A provider accepted your request");
        notes.Select(n => n.Title).Should().Contain("Your provider started the job");
        notes.Select(n => n.Title).Should().Contain("Your request was completed");
        notes.Should().OnlyContain(n => !n.IsRead);

        var marked = await SendAsync<UserNotificationDto>(
            customer,
            HttpMethod.Post,
            $"/api/notifications/{notes[0].Id}/read",
            payload: null);
        marked.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Only_the_assigned_provider_can_start_a_job()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin);
        var customer = await RegisterAsync("Casey", Roles.CommonUser);
        var winner = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);
        var other = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);

        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            Place(serviceId),
            HttpStatusCode.Created);

        await SendAsync<ServiceRequestDto>(
            winner,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/accept",
            payload: null);

        var forbidden = await SendRawAsync(other, HttpMethod.Post, $"/api/service-requests/{placed.Id}/start");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var customerStart = await SendRawAsync(customer, HttpMethod.Post, $"/api/service-requests/{placed.Id}/start");
        customerStart.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Provider_can_fail_an_in_progress_job()
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

        await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/accept",
            payload: null);
        await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/start",
            payload: null);

        var failed = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/fail",
            payload: null);
        failed.Status.Should().Be(ServiceRequestStatus.Failed);

        var notes = await GetAsync<List<UserNotificationDto>>(customer, "/api/notifications");
        notes.Select(n => n.Title).Should().Contain("Your job could not be completed");
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
            new CreateServiceCategoryRequest(UniqueName("Ride"), "Broadcast tests", null));
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

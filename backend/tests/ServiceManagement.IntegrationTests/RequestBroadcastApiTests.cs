using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Providers.Dtos;
using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.IntegrationTests;

[Collection(ApiCollection.Name)]
public class RequestBroadcastApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RequestBroadcastApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureSeededAsync();
        _factory.Clock.UtcNow = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Customer_places_request_and_eligible_provider_accepts_first()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin);
        var customer = await RegisterAsync("Casey", Roles.CommonUser);
        var winner = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);
        var loser = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);
        var pendingProvider = await RegisterAsync("Pat", Roles.ServiceProvider);

        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            Place(serviceId),
            HttpStatusCode.Created);

        placed.Status.Should().Be(ServiceRequestStatus.Pending);
        placed.Price.Should().Be(10m);
        placed.Customer.Should().BeNull();
        placed.Provider.Should().BeNull();
        placed.ExpiresAt.Should().Be(_factory.Clock.UtcNow.AddMinutes(10));

        var inbox = await GetAsync<List<ServiceRequestDto>>(winner, "/api/service-requests/inbox");
        inbox.Should().ContainSingle(r => r.Id == placed.Id);
        inbox[0].Customer.Should().BeNull();
        inbox[0].Address.Should().BeNull();

        var pendingInbox = await GetAsync<List<ServiceRequestDto>>(pendingProvider, "/api/service-requests/inbox");
        pendingInbox.Should().BeEmpty();

        var accepted = await SendAsync<ServiceRequestDto>(
            winner,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/accept",
            payload: null);

        accepted.Status.Should().Be(ServiceRequestStatus.Accepted);
        accepted.Customer.Should().NotBeNull();
        accepted.Customer!.Email.Should().NotBeNullOrWhiteSpace();
        accepted.Provider.Should().NotBeNull();
        accepted.Address.Should().NotBeNull();

        var lost = await SendRawAsync(loser, HttpMethod.Post, $"/api/service-requests/{placed.Id}/accept");
        lost.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var customerView = await GetAsync<ServiceRequestDto>(customer, $"/api/service-requests/{placed.Id}");
        customerView.Provider.Should().NotBeNull();
        customerView.Provider!.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Unaccepted_request_expires_and_cannot_be_claimed()
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

        _factory.Clock.Advance(TimeSpan.FromMinutes(11));

        var inbox = await GetAsync<List<ServiceRequestDto>>(provider, "/api/service-requests/inbox");
        inbox.Should().BeEmpty();

        var accept = await SendRawAsync(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/accept");
        accept.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var mine = await GetAsync<List<ServiceRequestDto>>(customer, "/api/service-requests/mine");
        mine.Should().ContainSingle(r => r.Id == placed.Id && r.Status == ServiceRequestStatus.Expired);
    }

    [Fact]
    public async Task Provider_accounts_can_also_place_requests()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, _) = await SeedCatalogAsync(admin);
        var provider = await RegisterAsync("Pat", Roles.ServiceProvider);

        var placed = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            "/api/service-requests",
            Place(serviceId),
            HttpStatusCode.Created);
        placed.ServiceId.Should().Be(serviceId);
    }

    [Fact]
    public async Task Customer_can_cancel_a_pending_request()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, _) = await SeedCatalogAsync(admin);
        var customer = await RegisterAsync("Casey", Roles.CommonUser);

        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            Place(serviceId),
            HttpStatusCode.Created);

        var cancelled = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/cancel",
            payload: null);
        cancelled.Status.Should().Be(ServiceRequestStatus.Cancelled);
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

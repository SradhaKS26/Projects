using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Domain.Constants;

namespace ServiceManagement.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CatalogApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CatalogApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.EnsureSeededAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Admin_can_create_update_and_deactivate_a_category()
    {
        var token = await AdminTokenAsync();
        var name = UniqueName("Cleaning");

        var created = await PostAsync<ServiceCategoryDto>(
            token,
            "/api/service-categories",
            new CreateServiceCategoryRequest(name, "Home cleaning", null));
        created.Name.Should().Be(name);
        created.IsActive.Should().BeTrue();

        var updated = await SendAsync<ServiceCategoryDto>(
            token,
            HttpMethod.Put,
            $"/api/service-categories/{created.Id}",
            new UpdateServiceCategoryRequest($"{name} Updated", "Updated description", null, true));
        updated.Name.Should().Be($"{name} Updated");

        var deleteResponse = await SendRawAsync(token, HttpMethod.Delete, $"/api/service-categories/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDisable = await GetAsync<ServiceCategoryDto>(
            token,
            $"/api/service-categories/{created.Id}");
        afterDisable.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_category_name_is_rejected()
    {
        var token = await AdminTokenAsync();
        var name = UniqueName("Duplicate");

        await PostAsync<ServiceCategoryDto>(
            token,
            "/api/service-categories",
            new CreateServiceCategoryRequest(name, null, null));

        var response = await SendRawAsync(
            token,
            HttpMethod.Post,
            "/api/service-categories",
            new CreateServiceCategoryRequest(name.ToLowerInvariant(), null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Service_creation_validates_price_and_category()
    {
        var token = await AdminTokenAsync();
        var category = await PostAsync<ServiceCategoryDto>(
            token,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Validated"), null, null));

        var negativePrice = await SendRawAsync(
            token,
            HttpMethod.Post,
            "/api/services",
            new CreateServiceRequest(category.Id, "Bad price", null, -5m, 60));
        negativePrice.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var missingCategory = await SendRawAsync(
            token,
            HttpMethod.Post,
            "/api/services",
            new CreateServiceRequest(Guid.NewGuid(), "Orphan", null, 10m, 60));
        missingCategory.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_service_name_within_a_category_is_rejected()
    {
        var token = await AdminTokenAsync();
        var category = await PostAsync<ServiceCategoryDto>(
            token,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Repeat"), null, null));

        await PostAsync<ServiceDto>(
            token,
            "/api/services",
            new CreateServiceRequest(category.Id, "Window Cleaning", null, 20m, 60));

        var duplicate = await SendRawAsync(
            token,
            HttpMethod.Post,
            "/api/services",
            new CreateServiceRequest(category.Id, "window cleaning", null, 25m, 60));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Customers_can_browse_but_not_modify_the_catalog()
    {
        var adminToken = await AdminTokenAsync();
        var category = await PostAsync<ServiceCategoryDto>(
            adminToken,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Browsable"), null, null));

        await PostAsync<ServiceDto>(
            adminToken,
            "/api/services",
            new CreateServiceRequest(category.Id, "Visible Service", null, 30m, 45));

        var customerToken = await CustomerTokenAsync();

        var browse = await GetAsync<List<ServiceDto>>(customerToken, $"/api/services?categoryId={category.Id}");
        browse.Should().ContainSingle(s => s.Name == "Visible Service");

        var forbidden = await SendRawAsync(
            customerToken,
            HttpMethod.Post,
            "/api/services",
            new CreateServiceRequest(category.Id, "Sneaky Service", null, 10m, 30));
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Inactive_services_are_hidden_from_customers_but_visible_to_admins()
    {
        var adminToken = await AdminTokenAsync();
        var category = await PostAsync<ServiceCategoryDto>(
            adminToken,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Hidden"), null, null));

        await PostAsync<ServiceDto>(
            adminToken,
            "/api/services",
            new CreateServiceRequest(category.Id, "Retired Service", null, 15m, 30, IsActive: false));

        var customerToken = await CustomerTokenAsync();
        var customerView = await GetAsync<List<ServiceDto>>(
            customerToken,
            $"/api/services?categoryId={category.Id}&includeInactive=true");
        customerView.Should().BeEmpty();

        var adminView = await GetAsync<List<ServiceDto>>(
            adminToken,
            $"/api/services?categoryId={category.Id}&includeInactive=true");
        adminView.Should().ContainSingle(s => s.Name == "Retired Service");
    }

    [Fact]
    public async Task Deactivating_a_category_also_deactivates_its_services()
    {
        var token = await AdminTokenAsync();
        var category = await PostAsync<ServiceCategoryDto>(
            token,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Cascade"), null, null));

        var service = await PostAsync<ServiceDto>(
            token,
            "/api/services",
            new CreateServiceRequest(category.Id, "Cascading Service", null, 20m, 60));

        await SendRawAsync(token, HttpMethod.Delete, $"/api/service-categories/{category.Id}");

        var reloaded = await GetAsync<ServiceDto>(token, $"/api/services/{service.Id}");
        reloaded.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Anonymous_callers_cannot_read_the_catalog()
    {
        var response = await _client.GetAsync("/api/services");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    private async Task<string> AdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@test.local", "TestAdmin!123"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return body!.Data!.AccessToken;
    }

    private async Task<string> CustomerTokenAsync()
    {
        var email = $"catalog_customer_{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Cat", "Customer", email, "Password!123", null, Roles.CommonUser));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return body!.Data!.AccessToken;
    }

    private async Task<T> GetAsync<T>(string token, string url)
    {
        var response = await SendRawAsync(token, HttpMethod.Get, url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        return body!.Data!;
    }

    private async Task<T> PostAsync<T>(string token, string url, object payload)
    {
        var response = await SendRawAsync(token, HttpMethod.Post, url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        return body!.Data!;
    }

    private async Task<T> SendAsync<T>(string token, HttpMethod method, string url, object payload)
    {
        var response = await SendRawAsync(token, method, url, payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
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
            request.Content = JsonContent.Create(payload, payload.GetType());
        }

        return await _client.SendAsync(request);
    }
}

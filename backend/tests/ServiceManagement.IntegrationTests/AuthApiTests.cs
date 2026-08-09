using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Domain.Constants;

namespace ServiceManagement.IntegrationTests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.EnsureSeededAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_and_login_common_user_succeeds()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var register = new RegisterRequest("Sara", "Customer", email, "Password!123", null, Roles.CommonUser);

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", register);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerBody = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        registerBody!.Success.Should().BeTrue();
        registerBody.Data!.User.Roles.Should().Contain(Roles.CommonUser);
        registerBody.Data.AccessToken.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password!123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Users_list_requires_ManageUsers_permission()
    {
        var email = $"noperm_{Guid.NewGuid():N}@example.com";
        var register = new RegisterRequest("No", "Perm", email, "Password!123", null, Roles.CommonUser);
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", register);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registerBody!.Data!.AccessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_can_list_users()
    {
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@test.local", "TestAdmin!123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        loginBody!.Data!.User.Permissions.Should().Contain(Permissions.ManageUsers);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Data.AccessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_as_administrator_is_rejected()
    {
        var register = new RegisterRequest(
            "Bad",
            "Actor",
            $"bad_{Guid.NewGuid():N}@example.com",
            "Password!123",
            null,
            Roles.Administrator);

        var response = await _client.PostAsJsonAsync("/api/auth/register", register);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

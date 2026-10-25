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
public class TaxiDeliveryApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaxiDeliveryApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureSeededAsync();
        _factory.Clock.UtcNow = new DateTime(2026, 10, 25, 14, 0, 0, DateTimeKind.Utc);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Taxi_ride_requires_dropoff_and_hides_street_until_accept()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin, FulfillmentKind.Ride, "Sedan");
        var customer = await RegisterAsync("Casey", Roles.CommonUser);
        var provider = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);

        var missingDropoff = await SendRawAsync(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            PickupOnly(serviceId));
        missingDropoff.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            TaxiRide(serviceId),
            HttpStatusCode.Created);

        placed.FulfillmentKind.Should().Be(FulfillmentKind.Ride);
        placed.PassengerCount.Should().Be(2);
        placed.Destination.Should().NotBeNull();
        placed.Destination!.City.Should().Be("Dallas");
        placed.Trip.Should().NotBeNull();
        placed.Trip!.Dropoff!.AddressLine1.Should().Be("90 Elm Street");

        var inbox = await GetAsync<List<ServiceRequestDto>>(provider, "/api/service-requests/inbox");
        inbox.Should().ContainSingle(r => r.Id == placed.Id);
        inbox[0].Address.Should().BeNull();
        inbox[0].Destination.Should().BeNull();
        var inboxTrip = inbox[0].Trip;
        inboxTrip.Should().NotBeNull();
        inboxTrip!.Pickup.Should().NotBeNull();
        inboxTrip.Pickup!.City.Should().Be("Austin");
        inboxTrip.Pickup.AddressLine1.Should().BeEmpty();
        inboxTrip.Dropoff.Should().NotBeNull();
        inboxTrip.Dropoff!.City.Should().Be("Dallas");
        inboxTrip.Dropoff.AddressLine1.Should().BeEmpty();
        inboxTrip.PassengerCount.Should().Be(2);

        var accepted = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/accept",
            payload: null);
        accepted.Address!.AddressLine1.Should().Be("12 Market Street");
        accepted.Destination!.AddressLine1.Should().Be("90 Elm Street");
        accepted.Trip!.Pickup!.AddressLine1.Should().Be("12 Market Street");
    }

    [Fact]
    public async Task Delivery_requires_parcel_and_recipient_then_completes()
    {
        var admin = await AdminTokenAsync();
        var (_, serviceId, requirementId) = await SeedCatalogAsync(admin, FulfillmentKind.Parcel, "Courier");
        var customer = await RegisterAsync("Casey", Roles.CommonUser);
        var provider = await RegisterApprovedAvailableProviderAsync(admin, serviceId, requirementId);

        var missingParcel = await SendRawAsync(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            TaxiRide(serviceId));
        missingParcel.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var placed = await SendAsync<ServiceRequestDto>(
            customer,
            HttpMethod.Post,
            "/api/service-requests",
            Parcel(serviceId),
            HttpStatusCode.Created);

        placed.FulfillmentKind.Should().Be(FulfillmentKind.Parcel);
        placed.ParcelDescription.Should().Be("Documents in a sealed envelope");
        placed.RecipientName.Should().Be("Alex Receiver");

        var inbox = await GetAsync<List<ServiceRequestDto>>(provider, "/api/service-requests/inbox");
        var parcelTrip = inbox[0].Trip;
        parcelTrip.Should().NotBeNull();
        parcelTrip!.ParcelDescription.Should().Be("Documents in a sealed envelope");
        parcelTrip.RecipientName.Should().BeNull();
        parcelTrip.RecipientPhone.Should().BeNull();

        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/accept", payload: null);
        await SendAsync<ServiceRequestDto>(provider, HttpMethod.Post, $"/api/service-requests/{placed.Id}/start", payload: null);
        var completed = await SendAsync<ServiceRequestDto>(
            provider,
            HttpMethod.Post,
            $"/api/service-requests/{placed.Id}/complete",
            payload: null);
        completed.Status.Should().Be(ServiceRequestStatus.Completed);
        completed.RecipientPhone.Should().Be("5550199");
    }

    [Fact]
    public async Task Walk_delivery_does_not_need_a_licence_vehicle_delivery_does()
    {
        var admin = await AdminTokenAsync();
        var category = await PostAsync<ServiceCategoryDto>(
            admin,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName("Delivery"), "Docs", null, true, FulfillmentKind.Parcel));
        var walk = await PostAsync<ServiceDto>(
            admin,
            "/api/services",
            new CreateServiceRequest(category.Id, UniqueName("Walk"), "On foot", 8m, 20));
        var vehicle = await PostAsync<ServiceDto>(
            admin,
            "/api/services",
            new CreateServiceRequest(category.Id, UniqueName("Van"), "By vehicle", 12m, 30));
        var idCard = await PostAsync<CategoryDocumentRequirementDto>(
            admin,
            $"/api/service-categories/{category.Id}/document-requirements",
            new CreateDocumentRequirementRequest("Government ID", null, true, 0));
        var licence = await PostAsync<CategoryDocumentRequirementDto>(
            admin,
            $"/api/service-categories/{category.Id}/document-requirements",
            new CreateDocumentRequirementRequest("Driving Licence", null, true, 1, [vehicle.Id]));

        licence.ServiceIds.Should().Contain(vehicle.Id);
        var token = await RegisterAsync("Casey", Roles.CommonUser);

        var walkOnly = await SendAsync<ProviderProfileDto>(
            token,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("Nearby only.", [walk.Id]));
        walkOnly.MissingRequiredDocuments.Select(m => m.Id).Should().Equal(idCard.Id);

        var withVehicle = await SendAsync<ProviderProfileDto>(
            token,
            HttpMethod.Put,
            "/api/providers/me/application",
            new UpdateProviderApplicationRequest("I also have a bike.", [walk.Id, vehicle.Id]));
        withVehicle.MissingRequiredDocuments.Select(m => m.Id).Should().BeEquivalentTo([idCard.Id, licence.Id]);
    }

    private static PlaceServiceRequest PickupOnly(Guid serviceId) => new(
        serviceId,
        "12 Market Street",
        null,
        "Austin",
        "TX",
        "78701",
        null,
        null,
        "Now please.");

    private static PlaceServiceRequest TaxiRide(Guid serviceId) => new(
        serviceId,
        "12 Market Street",
        null,
        "Austin",
        "TX",
        "78701",
        null,
        null,
        "Two passengers.",
        new PlaceAddressRequest("90 Elm Street", null, "Dallas", "TX", "75201"),
        PassengerCount: 2);

    private static PlaceServiceRequest Parcel(Guid serviceId) => new(
        serviceId,
        "12 Market Street",
        null,
        "Austin",
        "TX",
        "78701",
        null,
        null,
        "Handle with care.",
        new PlaceAddressRequest("90 Elm Street", null, "Dallas", "TX", "75201"),
        ParcelDescription: "Documents in a sealed envelope",
        RecipientName: "Alex Receiver",
        RecipientPhone: "5550199");

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

    private async Task<(Guid CategoryId, Guid ServiceId, Guid RequirementId)> SeedCatalogAsync(
        string adminToken,
        FulfillmentKind kind,
        string serviceName)
    {
        var category = await PostAsync<ServiceCategoryDto>(
            adminToken,
            "/api/service-categories",
            new CreateServiceCategoryRequest(UniqueName(kind.ToString()), "Trip tests", null, true, kind));
        var service = await PostAsync<ServiceDto>(
            adminToken,
            "/api/services",
            new CreateServiceRequest(category.Id, UniqueName(serviceName), "A bookable service type", 10m, 20));
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

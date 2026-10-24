using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceManagement.Application.Requests.Interfaces;

namespace ServiceManagement.Infrastructure.Services;

public class RequestExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestExpiryHostedService> _logger;

    public RequestExpiryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<RequestExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var requests = scope.ServiceProvider.GetRequiredService<IServiceRequestService>();
                var expired = await requests.ExpireOverdueAsync(stoppingToken);
                if (expired > 0)
                {
                    _logger.LogInformation("Expired {Count} unaccepted service request(s).", expired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire overdue service requests.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

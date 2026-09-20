using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Api.Extensions;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Providers.Interfaces;
using ServiceManagement.Domain.Constants;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/provider-documents")]
[Authorize]
public class ProviderDocumentsController : ControllerBase
{
    private readonly IProviderOnboardingService _providers;

    public ProviderDocumentsController(IProviderOnboardingService providers)
    {
        _providers = providers;
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var file = await _providers.OpenDocumentAsync(
            User.GetUserId(),
            User.HasPermission(Permissions.ManageProviders),
            id,
            cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }
}

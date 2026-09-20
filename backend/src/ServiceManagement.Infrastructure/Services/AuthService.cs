using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Auth.Interfaces;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Infrastructure.Auth;
using ServiceManagement.Infrastructure.Options;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext db,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            throw new AppException($"Role '{request.Role}' is not configured.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AppException($"Registration failed: {string.Join(" ", errors)}");
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        if (request.Role == Roles.ServiceProvider)
        {
            _db.ServiceProviderProfiles.Add(new ServiceProviderProfile
            {
                UserId = user.Id,
                AvailabilityStatus = AvailabilityStatus.Unavailable,
                IsActive = true,
                ApprovalStatus = ProviderApprovalStatus.Pending
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAppException("This account is inactive.");
        }

        // Prefer direct password check for API auth (no cookie sign-in side effects).
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAppException("This account is temporarily locked. Try again later.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedAppException("Refresh token is required.");
        }

        var stored = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (stored is null || !stored.IsActive || !stored.User.IsActive)
        {
            throw new UnauthorizedAppException("Invalid refresh token.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(stored.User, cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles, cancellationToken);

        var (accessToken, expiresAt) = _jwtTokenService.CreateAccessToken(user, roles, permissions);
        var refreshTokenValue = _jwtTokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            expiresAt,
            new UserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.ProfileImageUrl,
                user.IsActive,
                roles.ToList(),
                permissions));
    }

    private async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var roleNames = roles.ToList();
        if (roleNames.Count == 0)
        {
            return [];
        }

        return await _db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}

using System.Security.Claims;

namespace StudentHub.Server.Services;

public class UserAuthService : IUserAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _cachedUuid;

    public UserAuthService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetUserUuid()
    {
        // If the UUID is already cached, return the cached value
        if (_cachedUuid != null) return _cachedUuid;

        // Get the current user from the HttpContext
        var user = _httpContextAccessor.HttpContext?.User;

        // If the user is not authenticated, return null
        if (user == null || !user.Identity.IsAuthenticated)
            return null;

        // Extract the uid (UUID) from the User's claims and cache it
        _cachedUuid = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        return _cachedUuid;
    }
}
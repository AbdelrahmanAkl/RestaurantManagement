using System.Security.Claims;

namespace MenuOrdering.API.Authorization;

public class TenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id)
                ? id
                : null;
        }
    }

    public string? Role =>
        _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.Role);

    public int? RestaurantId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue("RestaurantId");

            return int.TryParse(value, out var id)
                ? id
                : null;
        }
    }

    public int? BranchId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue("BranchId");

            return int.TryParse(value, out var id)
                ? id
                : null;
        }
    }

    public bool IsAdmin =>
        Role == "Admin";

    public bool IsManager =>
        Role == "Manager";

    public bool IsWaiter =>
        Role == "Waiter";

    public bool IsKitchen =>
        Role == "Kitchen";
}
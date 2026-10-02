using MenuOrdering.API.Authorization;
using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Users;
using MenuOrdering.API.Enums;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly MenuDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly TenantContext _tenantContext;

    public UsersController(
        MenuDbContext context,
        TenantContext tenantContext)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(x => x.Restaurant)
            .Include(x => x.Branch)
            .AsQueryable();

        if (_tenantContext.IsSuperAdmin)
        {
        }
        else if (_tenantContext.IsBranchScoped)
        {
            if (!_tenantContext.BranchId.HasValue ||
                !_tenantContext.RestaurantId.HasValue)
                return Forbid();

            query = query.Where(x =>
                x.RestaurantId == _tenantContext.RestaurantId.Value &&
                x.BranchId == _tenantContext.BranchId.Value);
        }
        else
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            query = query.Where(x =>
                x.RestaurantId == _tenantContext.RestaurantId.Value);
        }

        var users = await query
            .OrderBy(x => x.Id)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role.ToString(),
                RestaurantId = x.RestaurantId,
                RestaurantName = x.Restaurant != null
                    ? x.Restaurant.Name
                    : null,
                BranchId = x.BranchId,
                BranchName = x.Branch != null
                    ? x.Branch.Name
                    : null,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Restaurant)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!CanManageUser(user))
            return Forbid();

        return Ok(ToResponse(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request)
    {
        if (!Enum.IsDefined(request.Role))
            return BadRequest(new { message = "Invalid user role." });

        if (!CanCreateRole(request.Role))
            return Forbid();

        var restaurantId = request.RestaurantId;
        var branchId = request.BranchId;

        if (_tenantContext.IsSuperAdmin)
        {
        }
        else if (_tenantContext.IsAdmin)
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            restaurantId = _tenantContext.RestaurantId.Value;

            if (request.Role == UserRole.Admin ||
                request.Role == UserRole.RestaurantManager)
            {
                branchId = null;
            }
        }
        else if (_tenantContext.IsRestaurantManager)
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            restaurantId = _tenantContext.RestaurantId.Value;
        }
        else if (_tenantContext.IsBranchManager)
        {
            if (!_tenantContext.RestaurantId.HasValue ||
                !_tenantContext.BranchId.HasValue)
                return Forbid();

            restaurantId = _tenantContext.RestaurantId.Value;
            branchId = _tenantContext.BranchId.Value;
        }
        else
        {
            return Forbid();
        }

        var validation = await ValidateRestaurantAndBranch(
            request.Role,
            restaurantId,
            branchId);

        if (validation != null)
            return BadRequest(new { message = validation });

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(x => x.Email == email))
        {
            return Conflict(new
            {
                message = "Email is already registered."
            });
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role,
            RestaurantId = restaurantId,
            BranchId = branchId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _context.Entry(user)
            .Reference(x => x.Restaurant)
            .LoadAsync();

        await _context.Entry(user)
            .Reference(x => x.Branch)
            .LoadAsync();

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            ToResponse(user));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        int id,
        [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(x => x.Restaurant)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!CanManageUser(user))
            return Forbid();

        if (!Enum.IsDefined(request.Role))
            return BadRequest(new { message = "Invalid user role." });

        if (!CanCreateRole(request.Role))
            return Forbid();

        var restaurantId = request.RestaurantId;
        var branchId = request.BranchId;

        if (_tenantContext.IsSuperAdmin)
        {
        }
        else if (_tenantContext.IsAdmin ||
                 _tenantContext.IsRestaurantManager)
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            restaurantId = _tenantContext.RestaurantId.Value;
        }
        else if (_tenantContext.IsBranchManager)
        {
            if (!IsBranchRole(request.Role))
                return Forbid();

            if (!_tenantContext.RestaurantId.HasValue ||
                !_tenantContext.BranchId.HasValue)
                return Forbid();

            restaurantId = _tenantContext.RestaurantId.Value;
            branchId = _tenantContext.BranchId.Value;
        }
        else
        {
            return Forbid();
        }

        var validation = await ValidateRestaurantAndBranch(
            request.Role,
            restaurantId,
            branchId);

        if (validation != null)
            return BadRequest(new { message = validation });

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(x =>
                x.Email == email &&
                x.Id != id))
        {
            return Conflict(new
            {
                message = "Email is already registered."
            });
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Role = request.Role;
        user.RestaurantId = restaurantId;
        user.BranchId = branchId;
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        await _context.Entry(user)
            .Reference(x => x.Restaurant)
            .LoadAsync();

        await _context.Entry(user)
            .Reference(x => x.Branch)
            .LoadAsync();

        return Ok(ToResponse(user));
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(
        int id,
        [FromBody] UserRole role)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!CanManageUser(user))
            return Forbid();

        if (!Enum.IsDefined(role))
            return BadRequest(new { message = "Invalid user role." });

        if (!CanCreateRole(role))
            return Forbid();

        var restaurantId = user.RestaurantId;
        var branchId = user.BranchId;

        if (_tenantContext.IsSuperAdmin)
        {
            if (role == UserRole.SuperAdmin)
            {
                restaurantId = null;
                branchId = null;
            }
        }
        else if (_tenantContext.IsAdmin ||
                 _tenantContext.IsRestaurantManager)
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            restaurantId = _tenantContext.RestaurantId.Value;
        }
        else if (_tenantContext.IsBranchManager)
        {
            if (!IsBranchRole(role))
                return Forbid();

            restaurantId = _tenantContext.RestaurantId;
            branchId = _tenantContext.BranchId;
        }
        else
        {
            return Forbid();
        }

        var validation = await ValidateRestaurantAndBranch(
            role,
            restaurantId,
            branchId);

        if (validation != null)
            return BadRequest(new { message = validation });

        user.Role = role;
        user.RestaurantId = restaurantId;
        user.BranchId = branchId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User role updated successfully.",
            userId = user.Id,
            role = user.Role.ToString(),
            restaurantId = user.RestaurantId,
            branchId = user.BranchId
        });
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int id,
        [FromBody] bool isActive)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!CanManageUser(user))
            return Forbid();

        user.IsActive = isActive;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User status updated successfully.",
            userId = user.Id,
            isActive = user.IsActive
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!CanManageUser(user))
            return Forbid();

        if (_tenantContext.UserId == user.Id)
        {
            return BadRequest(new
            {
                message = "You cannot delete your own account."
            });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User deleted successfully.",
            userId = id
        });
    }

    private bool CanCreateRole(UserRole role)
    {
        if (_tenantContext.IsSuperAdmin)
            return true;

        if (_tenantContext.IsAdmin)
        {
            return role != UserRole.SuperAdmin;
        }

        if (_tenantContext.IsRestaurantManager)
        {
            return role == UserRole.BranchManager ||
                   role == UserRole.Waiter ||
                   role == UserRole.Kitchen ||
                   role == UserRole.Cashier;
        }

        if (_tenantContext.IsBranchManager)
        {
            return role == UserRole.Waiter ||
                   role == UserRole.Kitchen ||
                   role == UserRole.Cashier;
        }

        return false;
    }

    private bool CanManageUser(User user)
    {
        if (_tenantContext.IsSuperAdmin)
            return true;

        if (!_tenantContext.RestaurantId.HasValue)
            return false;

        if (user.RestaurantId != _tenantContext.RestaurantId.Value)
            return false;

        if (_tenantContext.IsAdmin)
        {
            return user.Role != UserRole.SuperAdmin;
        }

        if (_tenantContext.IsRestaurantManager)
        {
            return user.Role == UserRole.BranchManager ||
                   user.Role == UserRole.Waiter ||
                   user.Role == UserRole.Kitchen ||
                   user.Role == UserRole.Cashier;
        }

        if (_tenantContext.IsBranchManager)
        {
            return user.BranchId == _tenantContext.BranchId &&
                   (user.Role == UserRole.Waiter ||
                    user.Role == UserRole.Kitchen ||
                    user.Role == UserRole.Cashier);
        }

        return false;
    }

    private static bool IsBranchRole(UserRole role)
    {
        return role == UserRole.Waiter ||
               role == UserRole.Kitchen ||
               role == UserRole.Cashier;
    }

    private async Task<string?> ValidateRestaurantAndBranch(
        UserRole role,
        int? restaurantId,
        int? branchId)
    {
        if (role == UserRole.SuperAdmin)
        {
            if (restaurantId.HasValue || branchId.HasValue)
                return "SuperAdmin must not be assigned to a restaurant or branch.";

            return null;
        }

        if (!restaurantId.HasValue)
            return "RestaurantId is required for this role.";

        var restaurantExists = await _context.Restaurants
            .AnyAsync(x =>
                x.Id == restaurantId.Value &&
                x.IsActive);

        if (!restaurantExists)
            return "Restaurant not found or inactive.";

        if (role == UserRole.Admin ||
            role == UserRole.RestaurantManager)
        {
            if (branchId.HasValue)
                return "This role must not be assigned to a branch.";

            return null;
        }

        if (!branchId.HasValue)
            return "BranchId is required for this role.";

        var branchExists = await _context.Branches
            .AnyAsync(x =>
                x.Id == branchId.Value &&
                x.RestaurantId == restaurantId.Value &&
                x.IsActive);

        if (!branchExists)
        {
            return "Branch not found, inactive, or does not belong to the selected restaurant.";
        }

        return null;
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            RestaurantId = user.RestaurantId,
            RestaurantName = user.Restaurant?.Name,
            BranchId = user.BranchId,
            BranchName = user.Branch?.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}

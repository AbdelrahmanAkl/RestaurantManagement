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
[Authorize(Roles = "Admin,Manager")]
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

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(x => x.Restaurant)
            .Include(x => x.Branch)
            .AsQueryable();

        if (_tenantContext.IsManager)
        {
            if (!_tenantContext.RestaurantId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x =>
                x.RestaurantId == _tenantContext.RestaurantId.Value &&
                (x.Role == UserRole.Waiter ||
                 x.Role == UserRole.Kitchen));
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

    // GET: api/Users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Restaurant)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!CanManageUser(user))
        {
            return Forbid();
        }

        return Ok(ToResponse(user));
    }

    // POST: api/Users
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request)
    {
        if (!Enum.IsDefined(request.Role))
        {
            return BadRequest(new
            {
                message = "Invalid user role."
            });
        }

        if (_tenantContext.IsManager &&
            request.Role != UserRole.Waiter &&
            request.Role != UserRole.Kitchen)
        {
            return BadRequest(new
            {
                message = "Managers can only create Waiter and Kitchen users."
            });
        }

        var restaurantId = request.RestaurantId;
        var branchId = request.BranchId;

        if (_tenantContext.IsManager)
        {
            restaurantId = _tenantContext.RestaurantId;
        }

        if (request.Role == UserRole.Admin)
        {
            restaurantId = null;
            branchId = null;
        }
        else if (request.Role == UserRole.Manager)
        {
            branchId = null;

            if (!restaurantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "RestaurantId is required for a Manager."
                });
            }
        }
        else
        {
            if (!restaurantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "RestaurantId is required."
                });
            }

            if (!branchId.HasValue)
            {
                return BadRequest(new
                {
                    message = "BranchId is required for Waiter and Kitchen users."
                });
            }
        }

        var validationResult = await ValidateRestaurantAndBranch(
            restaurantId,
            branchId,
            request.Role);

        if (validationResult != null)
        {
            return BadRequest(new
            {
                message = validationResult
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existingUser != null)
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

    // PUT: api/Users/5
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
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!CanManageUser(user))
        {
            return Forbid();
        }

        if (!Enum.IsDefined(request.Role))
        {
            return BadRequest(new
            {
                message = "Invalid user role."
            });
        }

        if (_tenantContext.IsManager &&
            request.Role != UserRole.Waiter &&
            request.Role != UserRole.Kitchen)
        {
            return BadRequest(new
            {
                message = "Managers can only manage Waiter and Kitchen users."
            });
        }

        var restaurantId = request.RestaurantId;
        var branchId = request.BranchId;

        if (_tenantContext.IsManager)
        {
            restaurantId = _tenantContext.RestaurantId;
        }

        if (request.Role == UserRole.Admin)
        {
            restaurantId = null;
            branchId = null;
        }
        else if (request.Role == UserRole.Manager)
        {
            branchId = null;
        }
        else
        {
            if (!restaurantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "RestaurantId is required."
                });
            }

            if (!branchId.HasValue)
            {
                return BadRequest(new
                {
                    message = "BranchId is required for Waiter and Kitchen users."
                });
            }
        }

        var validationResult = await ValidateRestaurantAndBranch(
            restaurantId,
            branchId,
            request.Role);

        if (validationResult != null)
        {
            return BadRequest(new
            {
                message = validationResult
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var emailUsedByAnotherUser = await _context.Users
            .AnyAsync(x =>
                x.Email == email &&
                x.Id != id);

        if (emailUsedByAnotherUser)
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

    // PUT: api/Users/5/role
    [HttpPut("{id:int}/role")]
    public async Task<ActionResult> ChangeRole(
        int id,
        [FromBody] UserRole role)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!CanManageUser(user))
        {
            return Forbid();
        }

        if (!Enum.IsDefined(role))
        {
            return BadRequest(new
            {
                message = "Invalid user role."
            });
        }

        if (_tenantContext.IsManager &&
            role != UserRole.Waiter &&
            role != UserRole.Kitchen)
        {
            return BadRequest(new
            {
                message = "Managers can only assign Waiter and Kitchen roles."
            });
        }

        if (role == UserRole.Waiter ||
            role == UserRole.Kitchen)
        {
            if (!user.RestaurantId.HasValue ||
                !user.BranchId.HasValue)
            {
                return BadRequest(new
                {
                    message = "Waiter and Kitchen users must have a restaurant and branch."
                });
            }
        }

        if (role == UserRole.Manager)
        {
            user.BranchId = null;
        }

        if (role == UserRole.Admin)
        {
            user.RestaurantId = null;
            user.BranchId = null;
        }

        user.Role = role;

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

    // PUT: api/Users/5/status
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult> ChangeStatus(
        int id,
        [FromBody] bool isActive)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!CanManageUser(user))
        {
            return Forbid();
        }

        user.IsActive = isActive;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User status updated successfully.",
            userId = user.Id,
            isActive = user.IsActive
        });
    }

    // DELETE: api/Users/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!CanManageUser(user))
        {
            return Forbid();
        }

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

    private bool CanManageUser(User user)
    {
        if (_tenantContext.IsAdmin)
        {
            return true;
        }

        if (_tenantContext.IsManager)
        {
            return _tenantContext.RestaurantId.HasValue &&
                   user.RestaurantId == _tenantContext.RestaurantId.Value &&
                   (user.Role == UserRole.Waiter ||
                    user.Role == UserRole.Kitchen);
        }

        return false;
    }

    private async Task<string?> ValidateRestaurantAndBranch(
        int? restaurantId,
        int? branchId,
        UserRole role)
    {
        if (role == UserRole.Admin)
        {
            return null;
        }

        if (!restaurantId.HasValue)
        {
            return "RestaurantId is required.";
        }

        var restaurantExists = await _context.Restaurants
            .AnyAsync(x =>
                x.Id == restaurantId.Value &&
                x.IsActive);

        if (!restaurantExists)
        {
            return "Restaurant not found or inactive.";
        }

        if (role == UserRole.Manager)
        {
            return null;
        }

        if (!branchId.HasValue)
        {
            return "BranchId is required for Waiter and Kitchen users.";
        }

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
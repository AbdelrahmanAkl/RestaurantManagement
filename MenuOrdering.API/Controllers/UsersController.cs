using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Users;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
private readonly MenuDbContext _context;
private readonly PasswordHasher<User> _passwordHasher;


public UsersController(MenuDbContext context)
{
    _context = context;
    _passwordHasher = new PasswordHasher<User>();
}

// GET: api/Users
[HttpGet]
public async Task<ActionResult> GetUsers()
{
    var users = await _context.Users
        .AsNoTracking()
        .Select(x => new
        {
            x.Id,
            x.FullName,
            x.Email,
            Role = x.Role.ToString(),
            x.IsActive,
            x.CreatedAt
        })
        .ToListAsync();

    return Ok(users);
}

// GET: api/Users/5
[HttpGet("{id:int}")]
public async Task<ActionResult> GetUser(int id)
{
    var user = await _context.Users
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new
        {
            x.Id,
            x.FullName,
            x.Email,
            Role = x.Role.ToString(),
            x.IsActive,
            x.CreatedAt
        })
        .FirstOrDefaultAsync();

    if (user == null)
    {
        return NotFound(new
        {
            message = "User not found."
        });
    }

    return Ok(user);
}

// POST: api/Users
[HttpPost]
public async Task<ActionResult> CreateUser(
    CreateUserRequest request)
{
    var email = request.Email.Trim().ToLower();

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
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    user.PasswordHash =
        _passwordHasher.HashPassword(
            user,
            request.Password
        );

    _context.Users.Add(user);

    await _context.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetUser),
        new { id = user.Id },
        new
        {
            user.Id,
            user.FullName,
            user.Email,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        }
    );
}

// PUT: api/Users/5/role
[HttpPut("{id:int}/role")]
public async Task<ActionResult> ChangeRole(
    int id,
    [FromBody] MenuOrdering.API.Enums.UserRole role)
{
    var user = await _context.Users.FindAsync(id);

    if (user == null)
    {
        return NotFound(new
        {
            message = "User not found."
        });
    }

    user.Role = role;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "User role updated successfully.",
        userId = user.Id,
        role = user.Role.ToString()
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

    user.IsActive = isActive;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "User status updated successfully.",
        userId = user.Id,
        isActive = user.IsActive
    });
}


}

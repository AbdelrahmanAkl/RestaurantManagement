using MenuOrdering.API.Authorization;
using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Categories;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/admin/menu/categories")]
[Authorize]
public class AdminMenuCategoriesController : ControllerBase
{
    private readonly MenuDbContext _context;
    private readonly TenantContext _tenantContext;

    public AdminMenuCategoriesController(
        MenuDbContext context,
        TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(
        [FromQuery] int? restaurantId = null)
    {
        var targetRestaurantId = ResolveRestaurantId(restaurantId);

        if (!_tenantContext.IsSuperAdmin && !targetRestaurantId.HasValue)
            return Forbid();

        var query = _context.Categories
            .AsNoTracking()
            .AsQueryable();

        if (targetRestaurantId.HasValue)
        {
            query = query.Where(x =>
                x.RestaurantId == targetRestaurantId.Value);
        }

        var categories = await query
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (!_tenantContext.CanAccessRestaurant(category.RestaurantId))
            return Forbid();

        return Ok(ToResponse(category));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        [FromQuery] int? restaurantId,
        [FromBody] CreateCategoryRequest request)
    {
        if (!CanManageMenu())
            return Forbid();

        var targetRestaurantId = ResolveRestaurantId(restaurantId);

        if (!targetRestaurantId.HasValue)
        {
            return BadRequest(new
            {
                message = "RestaurantId is required for SuperAdmin."
            });
        }

        var restaurantExists = await _context.Restaurants
            .AnyAsync(x =>
                x.Id == targetRestaurantId.Value &&
                x.IsActive);

        if (!restaurantExists)
        {
            return BadRequest(new
            {
                message = "Restaurant not found or inactive."
            });
        }

        var name = request.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(x =>
                x.RestaurantId == targetRestaurantId.Value &&
                x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message = "A category with this name already exists in this restaurant."
            });
        }

        var category = new Category
        {
            RestaurantId = targetRestaurantId.Value,
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            ToResponse(category));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryRequest request)
    {
        if (!CanManageMenu())
            return Forbid();

        var category = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (!_tenantContext.CanAccessRestaurant(category.RestaurantId))
            return Forbid();

        var name = request.Name.Trim();

        var duplicate = await _context.Categories
            .AnyAsync(x =>
                x.Id != id &&
                x.RestaurantId == category.RestaurantId &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Another category with this name already exists in this restaurant."
            });
        }

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (!CanManageMenu())
            return Forbid();

        var category = await _context.Categories
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (!_tenantContext.CanAccessRestaurant(category.RestaurantId))
            return Forbid();

        if (category.MenuItems.Any())
        {
            return Conflict(new
            {
                message = "Cannot delete a category that contains menu items."
            });
        }

        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private int? ResolveRestaurantId(int? requestedRestaurantId)
    {
        if (_tenantContext.IsSuperAdmin)
            return requestedRestaurantId;

        return _tenantContext.RestaurantId;
    }

    private bool CanManageMenu()
    {
        return _tenantContext.IsSuperAdmin ||
               _tenantContext.IsAdmin ||
               _tenantContext.IsRestaurantManager;
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };
    }
}
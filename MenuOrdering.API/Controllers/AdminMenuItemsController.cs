using MenuOrdering.API.Authorization;
using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.MenuItems;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/admin/menu/items")]
[Authorize]
public class AdminMenuItemsController : ControllerBase
{
    private readonly MenuDbContext _context;
    private readonly TenantContext _tenantContext;

    public AdminMenuItemsController(
        MenuDbContext context,
        TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetMenuItems(
        [FromQuery] int? restaurantId = null,
        [FromQuery] int? categoryId = null)
    {
        var targetRestaurantId = ResolveRestaurantId(restaurantId);

        if (!_tenantContext.IsSuperAdmin && !targetRestaurantId.HasValue)
            return Forbid();

        if (categoryId.HasValue)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == categoryId.Value);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (!_tenantContext.CanAccessRestaurant(category.RestaurantId))
                return Forbid();

            if (targetRestaurantId.HasValue &&
                category.RestaurantId != targetRestaurantId.Value)
            {
                return Forbid();
            }
        }

        var query = _context.MenuItems
            .AsNoTracking()
            .AsQueryable();

        if (targetRestaurantId.HasValue)
        {
            query = query.Where(x =>
                x.Category.RestaurantId == targetRestaurantId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x =>
                x.CategoryId == categoryId.Value);
        }

        var items = await query
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.Name)
            .Select(x => new MenuItemResponse
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                ImageUrl = x.ImageUrl,
                IsAvailable = x.IsAvailable
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuItemResponse>> GetMenuItem(int id)
    {
        var item = await _context.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
            return NotFound(new { message = "Menu item not found." });

        if (!_tenantContext.CanAccessRestaurant(item.Category.RestaurantId))
            return Forbid();

        return Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<MenuItemResponse>> CreateMenuItem(
        [FromBody] CreateMenuItemRequest request)
    {
        if (!CanManageMenu())
            return Forbid();

        var category = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId);

        if (category == null)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

        if (!_tenantContext.CanAccessRestaurant(category.RestaurantId))
            return Forbid();

        var name = request.Name.Trim();

        var exists = await _context.MenuItems
            .AnyAsync(x =>
                x.CategoryId == request.CategoryId &&
                x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message = "A menu item with this name already exists in this category."
            });
        }

        var item = new MenuItem
        {
            CategoryId = request.CategoryId,
            Name = name,
            Description = request.Description?.Trim(),
            Price = request.Price,
            ImageUrl = request.ImageUrl?.Trim(),
            IsAvailable = request.IsAvailable
        };

        _context.MenuItems.Add(item);

        await _context.SaveChangesAsync();

        item.Category = category;

        return CreatedAtAction(
            nameof(GetMenuItem),
            new { id = item.Id },
            ToResponse(item));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMenuItem(
        int id,
        [FromBody] UpdateMenuItemRequest request)
    {
        if (!CanManageMenu())
            return Forbid();

        var item = await _context.MenuItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
            return NotFound(new { message = "Menu item not found." });

        if (!_tenantContext.CanAccessRestaurant(item.Category.RestaurantId))
            return Forbid();

        var targetCategory = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId);

        if (targetCategory == null)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

        if (!_tenantContext.CanAccessRestaurant(targetCategory.RestaurantId))
            return Forbid();

        if (targetCategory.RestaurantId != item.Category.RestaurantId)
        {
            return BadRequest(new
            {
                message = "A menu item cannot be moved to another restaurant."
            });
        }

        var name = request.Name.Trim();

        var duplicate = await _context.MenuItems
            .AnyAsync(x =>
                x.Id != id &&
                x.CategoryId == request.CategoryId &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A menu item with this name already exists in this category."
            });
        }

        item.CategoryId = request.CategoryId;
        item.Category = targetCategory;
        item.Name = name;
        item.Description = request.Description?.Trim();
        item.Price = request.Price;
        item.ImageUrl = request.ImageUrl?.Trim();
        item.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        if (!CanManageMenu())
            return Forbid();

        var item = await _context.MenuItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
            return NotFound(new { message = "Menu item not found." });

        if (!_tenantContext.CanAccessRestaurant(item.Category.RestaurantId))
            return Forbid();

        _context.MenuItems.Remove(item);

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

    private static MenuItemResponse ToResponse(MenuItem item)
    {
        return new MenuItemResponse
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            IsAvailable = item.IsAvailable
        };
    }
}
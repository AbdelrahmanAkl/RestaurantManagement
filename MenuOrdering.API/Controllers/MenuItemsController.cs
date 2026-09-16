using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.MenuItems;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly MenuDbContext _context;

    public MenuItemsController(MenuDbContext context)
    {
        _context = context;
    }

    // GET: api/menuitems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetMenuItems()
    {
        var items = await _context.MenuItems
            .AsNoTracking()
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

    // GET: api/menuitems/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuItemResponse>> GetMenuItem(int id)
    {
        var item = await _context.MenuItems
            .AsNoTracking()
            .Where(x => x.Id == id)
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
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return NotFound(new
            {
                message = "Menu item not found."
            });
        }

        return Ok(item);
    }

    // GET: api/menuitems/category/1
    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetMenuItemsByCategory(
        int categoryId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Id == categoryId);

        if (!categoryExists)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var items = await _context.MenuItems
            .AsNoTracking()
            .Where(x =>
                x.CategoryId == categoryId &&
                x.IsAvailable)
            .OrderBy(x => x.Name)
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

    // POST: api/menuitems
    [HttpPost]
    public async Task<ActionResult<MenuItemResponse>> CreateMenuItem(
        CreateMenuItemRequest request)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId);

        if (category == null)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

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
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            IsAvailable = request.IsAvailable
        };

        _context.MenuItems.Add(item);

        await _context.SaveChangesAsync();

        var response = new MenuItemResponse
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            CategoryName = category.Name,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            IsAvailable = item.IsAvailable
        };

        return CreatedAtAction(
            nameof(GetMenuItem),
            new { id = item.Id },
            response);
    }

    // PUT: api/menuitems/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMenuItem(
        int id,
        UpdateMenuItemRequest request)
    {
        var item = await _context.MenuItems
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
        {
            return NotFound(new
            {
                message = "Menu item not found."
            });
        }

        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Id == request.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
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
        item.Name = name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.ImageUrl = request.ImageUrl;
        item.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/menuitems/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        var item = await _context.MenuItems
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
        {
            return NotFound(new
            {
                message = "Menu item not found."
            });
        }

        _context.MenuItems.Remove(item);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
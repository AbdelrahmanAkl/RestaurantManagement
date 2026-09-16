using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Categories;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly MenuDbContext _context;

    public CategoriesController(MenuDbContext context)
    {
        _context = context;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories()
    {
        var categories = await _context.Categories
            .AsNoTracking()
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

    // GET: api/categories/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(category);
    }

    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        CreateCategoryRequest request)
    {
        var name = request.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(x => x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message = "A category with this name already exists."
            });
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            response);
    }

    // PUT: api/categories/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        UpdateCategoryRequest request)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var name = request.Name.Trim();

        var duplicate = await _context.Categories
            .AnyAsync(x =>
                x.Id != id &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Another category with this name already exists."
            });
        }

        category.Name = name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categories/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

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
}
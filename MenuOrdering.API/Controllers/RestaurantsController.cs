using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Restaurants;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly MenuDbContext _context;

    public RestaurantsController(MenuDbContext context)
    {
        _context = context;
    }

    // GET: api/restaurants
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestaurantResponse>>> GetRestaurants()
    {
        var restaurants = await _context.Restaurants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RestaurantResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Phone = x.Phone,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                BranchCount = x.Branches.Count
            })
            .ToListAsync();

        return Ok(restaurants);
    }

    // GET: api/restaurants/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RestaurantResponse>> GetRestaurant(int id)
    {
        var restaurant = await _context.Restaurants
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new RestaurantResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Phone = x.Phone,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                BranchCount = x.Branches.Count
            })
            .FirstOrDefaultAsync();

        if (restaurant == null)
        {
            return NotFound(new
            {
                message = "Restaurant not found."
            });
        }

        return Ok(restaurant);
    }

    // POST: api/restaurants
    [HttpPost]
    public async Task<ActionResult<RestaurantResponse>> CreateRestaurant(
        CreateRestaurantRequest request)
    {
        var name = request.Name.Trim();

        var exists = await _context.Restaurants
            .AnyAsync(x => x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message = "A restaurant with this name already exists."
            });
        }

        var restaurant = new Restaurant
        {
            Name = name,
            Description = request.Description?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLower(),
            IsActive = request.IsActive
        };

        _context.Restaurants.Add(restaurant);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetRestaurant),
            new { id = restaurant.Id },
            new RestaurantResponse
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Description = restaurant.Description,
                Phone = restaurant.Phone,
                Email = restaurant.Email,
                IsActive = restaurant.IsActive,
                CreatedAt = restaurant.CreatedAt,
                BranchCount = 0
            });
    }

    // PUT: api/restaurants/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRestaurant(
        int id,
        UpdateRestaurantRequest request)
    {
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(x => x.Id == id);

        if (restaurant == null)
        {
            return NotFound(new
            {
                message = "Restaurant not found."
            });
        }

        var name = request.Name.Trim();

        var duplicate = await _context.Restaurants
            .AnyAsync(x =>
                x.Id != id &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Another restaurant with this name already exists."
            });
        }

        restaurant.Name = name;
        restaurant.Description = request.Description?.Trim();
        restaurant.Phone = request.Phone?.Trim();
        restaurant.Email = request.Email?.Trim().ToLower();
        restaurant.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/restaurants/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        var restaurant = await _context.Restaurants
            .Include(x => x.Branches)
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (restaurant == null)
        {
            return NotFound(new
            {
                message = "Restaurant not found."
            });
        }

        if (restaurant.Branches.Any() ||
            restaurant.Categories.Any())
        {
            return Conflict(new
            {
                message = "Cannot delete a restaurant that has branches or categories."
            });
        }

        _context.Restaurants.Remove(restaurant);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Branches;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly MenuDbContext _context;

    public BranchesController(MenuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchResponse>>> GetBranches(
        [FromQuery] int? restaurantId)
    {
        var query = _context.Branches
            .AsNoTracking()
            .AsQueryable();

        if (restaurantId.HasValue)
        {
            query = query.Where(x =>
                x.RestaurantId == restaurantId.Value);
        }

        var branches = await query
            .OrderBy(x => x.Name)
            .Select(x => new BranchResponse
            {
                Id = x.Id,
                RestaurantId = x.RestaurantId,
                RestaurantName = x.Restaurant.Name,
                Name = x.Name,
                Address = x.Address,
                Phone = x.Phone,
                IsActive = x.IsActive,
                TableCount = x.Tables.Count
            })
            .ToListAsync();

        return Ok(branches);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BranchResponse>> GetBranch(int id)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BranchResponse
            {
                Id = x.Id,
                RestaurantId = x.RestaurantId,
                RestaurantName = x.Restaurant.Name,
                Name = x.Name,
                Address = x.Address,
                Phone = x.Phone,
                IsActive = x.IsActive,
                TableCount = x.Tables.Count
            })
            .FirstOrDefaultAsync();

        if (branch == null)
        {
            return NotFound(new
            {
                message = "Branch not found."
            });
        }

        return Ok(branch);
    }

    [HttpPost]
    public async Task<ActionResult<BranchResponse>> CreateBranch(
        CreateBranchRequest request)
    {
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(x =>
                x.Id == request.RestaurantId);

        if (restaurant == null)
        {
            return BadRequest(new
            {
                message = "Restaurant not found."
            });
        }

        if (!restaurant.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "Cannot create a branch for an inactive restaurant."
            });
        }

        var name = request.Name.Trim();

        var exists = await _context.Branches
            .AnyAsync(x =>
                x.RestaurantId == request.RestaurantId &&
                x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message =
                    "A branch with this name already exists in this restaurant."
            });
        }

        var branch = new Branch
        {
            RestaurantId = request.RestaurantId,
            Name = name,
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = request.IsActive
        };

        _context.Branches.Add(branch);

        await _context.SaveChangesAsync();

        var response = new BranchResponse
        {
            Id = branch.Id,
            RestaurantId = branch.RestaurantId,
            RestaurantName = restaurant.Name,
            Name = branch.Name,
            Address = branch.Address,
            Phone = branch.Phone,
            IsActive = branch.IsActive,
            TableCount = 0
        };

        return CreatedAtAction(
            nameof(GetBranch),
            new { id = branch.Id },
            response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBranch(
        int id,
        UpdateBranchRequest request)
    {
        var branch = await _context.Branches
            .FirstOrDefaultAsync(x => x.Id == id);

        if (branch == null)
        {
            return NotFound(new
            {
                message = "Branch not found."
            });
        }

        var name = request.Name.Trim();

        var duplicate = await _context.Branches
            .AnyAsync(x =>
                x.Id != id &&
                x.RestaurantId == branch.RestaurantId &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Another branch with this name already exists."
            });
        }

        branch.Name = name;
        branch.Address = request.Address?.Trim();
        branch.Phone = request.Phone?.Trim();
        branch.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBranch(int id)
    {
        var branch = await _context.Branches
            .Include(x => x.Tables)
            .Include(x => x.Orders)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (branch == null)
        {
            return NotFound(new
            {
                message = "Branch not found."
            });
        }

        if (branch.Tables.Any())
        {
            return Conflict(new
            {
                message =
                    "Cannot delete a branch that contains tables."
            });
        }

        if (branch.Orders.Any())
        {
            return Conflict(new
            {
                message =
                    "Cannot delete a branch that contains orders."
            });
        }

        _context.Branches.Remove(branch);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
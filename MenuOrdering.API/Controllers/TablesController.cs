using MenuOrdering.API.Authorization;
using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Tables;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController : ControllerBase
{
    private readonly MenuDbContext _context;
    private readonly TenantContext _tenantContext;

    public TablesController(
        MenuDbContext context,
        TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableResponse>>> GetTables(
        [FromQuery] int? branchId)
    {
        var query = _context.RestaurantTables
            .AsNoTracking()
            .Include(x => x.Branch)
            .AsQueryable();

        if (_tenantContext.IsSuperAdmin)
        {
            if (branchId.HasValue)
                query = query.Where(x => x.BranchId == branchId.Value);
        }
        else if (_tenantContext.IsBranchScoped)
        {
            if (!_tenantContext.BranchId.HasValue)
                return Forbid();

            query = query.Where(x =>
                x.BranchId == _tenantContext.BranchId.Value);
        }
        else
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            query = query.Where(x =>
                x.Branch.RestaurantId ==
                _tenantContext.RestaurantId.Value);

            if (branchId.HasValue)
            {
                query = query.Where(x =>
                    x.BranchId == branchId.Value);
            }
        }

        var tables = await query
            .OrderBy(x => x.Branch.Name)
            .ThenBy(x => x.TableNumber)
            .Select(x => new TableResponse
            {
                Id = x.Id,
                BranchId = x.BranchId,
                BranchName = x.Branch.Name,
                TableNumber = x.TableNumber,
                QRCode = x.QRCode,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(tables);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableResponse>> GetTable(int id)
    {
        var table = await _context.RestaurantTables
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (table == null)
            return NotFound(new { message = "Table not found." });

        if (!_tenantContext.CanAccessBranch(
                table.BranchId,
                table.Branch.RestaurantId))
        {
            return Forbid();
        }

        return Ok(ToResponse(table));
    }

    [HttpGet("code/{tableNumber}")]
    public async Task<ActionResult<TableResponse>> GetTableByNumber(
        string tableNumber)
    {
        var query = _context.RestaurantTables
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x =>
                x.TableNumber == tableNumber &&
                x.IsActive)
            .AsQueryable();

        if (_tenantContext.IsSuperAdmin)
        {
        }
        else if (_tenantContext.IsBranchScoped)
        {
            if (!_tenantContext.BranchId.HasValue)
                return Forbid();

            query = query.Where(x =>
                x.BranchId == _tenantContext.BranchId.Value);
        }
        else
        {
            if (!_tenantContext.RestaurantId.HasValue)
                return Forbid();

            query = query.Where(x =>
                x.Branch.RestaurantId ==
                _tenantContext.RestaurantId.Value);
        }

        var table = await query.FirstOrDefaultAsync();

        if (table == null)
        {
            return NotFound(new
            {
                message = "Table not found or inactive."
            });
        }

        return Ok(ToResponse(table));
    }

    [HttpPost]
    public async Task<ActionResult<TableResponse>> CreateTable(
        CreateTableRequest request)
    {
        if (!_tenantContext.IsSuperAdmin &&
            !_tenantContext.IsAdmin &&
            !_tenantContext.IsRestaurantManager &&
            !_tenantContext.IsBranchManager)
        {
            return Forbid();
        }

        var branch = await _context.Branches
            .Include(x => x.Restaurant)
            .FirstOrDefaultAsync(x => x.Id == request.BranchId);

        if (branch == null)
        {
            return BadRequest(new
            {
                message = "Branch not found."
            });
        }

        if (!_tenantContext.CanAccessBranch(
                branch.Id,
                branch.RestaurantId))
        {
            return Forbid();
        }

        if (!branch.IsActive)
        {
            return BadRequest(new
            {
                message = "Cannot create a table in an inactive branch."
            });
        }

        var tableNumber = request.TableNumber.Trim();

        var exists = await _context.RestaurantTables
            .AnyAsync(x =>
                x.BranchId == branch.Id &&
                x.TableNumber == tableNumber);

        if (exists)
        {
            return Conflict(new
            {
                message =
                    "A table with this number already exists in this branch."
            });
        }

        var table = new RestaurantTable
        {
            BranchId = branch.Id,
            TableNumber = tableNumber,
            IsActive = request.IsActive
        };

        _context.RestaurantTables.Add(table);
        await _context.SaveChangesAsync();

        table.QRCode =
            $"MENUORDERING|TABLE:{table.Id}|BRANCH:{table.BranchId}";

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTable),
            new { id = table.Id },
            ToResponse(table));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTable(
        int id,
        UpdateTableRequest request)
    {
        var table = await _context.RestaurantTables
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (table == null)
            return NotFound(new { message = "Table not found." });

        if (!_tenantContext.CanAccessBranch(
                table.BranchId,
                table.Branch.RestaurantId))
        {
            return Forbid();
        }

        if (!_tenantContext.IsSuperAdmin &&
            !_tenantContext.IsAdmin &&
            !_tenantContext.IsRestaurantManager &&
            !_tenantContext.IsBranchManager)
        {
            return Forbid();
        }

        var tableNumber = request.TableNumber.Trim();

        var duplicate = await _context.RestaurantTables
            .AnyAsync(x =>
                x.Id != id &&
                x.BranchId == table.BranchId &&
                x.TableNumber == tableNumber);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Another table with this number already exists in this branch."
            });
        }

        table.TableNumber = tableNumber;
        table.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTable(int id)
    {
        var table = await _context.RestaurantTables
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (table == null)
            return NotFound(new { message = "Table not found." });

        if (!_tenantContext.CanAccessBranch(
                table.BranchId,
                table.Branch.RestaurantId))
        {
            return Forbid();
        }

        if (!_tenantContext.IsSuperAdmin &&
            !_tenantContext.IsAdmin &&
            !_tenantContext.IsRestaurantManager &&
            !_tenantContext.IsBranchManager)
        {
            return Forbid();
        }

        _context.RestaurantTables.Remove(table);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static TableResponse ToResponse(RestaurantTable table)
    {
        return new TableResponse
        {
            Id = table.Id,
            BranchId = table.BranchId,
            BranchName = table.Branch.Name,
            TableNumber = table.TableNumber,
            QRCode = table.QRCode,
            IsActive = table.IsActive
        };
    }
}

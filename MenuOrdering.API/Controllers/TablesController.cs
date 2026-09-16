using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Tables;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly MenuDbContext _context;

    public TablesController(MenuDbContext context)
    {
        _context = context;
    }

    // GET: api/tables
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableResponse>>> GetTables()
    {
        var tables = await _context.RestaurantTables
            .AsNoTracking()
            .OrderBy(x => x.TableNumber)
            .Select(x => new TableResponse
            {
                Id = x.Id,
                TableNumber = x.TableNumber,
                QRCode = x.QRCode,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(tables);
    }

    // GET: api/tables/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableResponse>> GetTable(int id)
    {
        var table = await _context.RestaurantTables
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TableResponse
            {
                Id = x.Id,
                TableNumber = x.TableNumber,
                QRCode = x.QRCode,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        if (table == null)
        {
            return NotFound(new
            {
                message = "Table not found."
            });
        }

        return Ok(table);
    }

    // GET: api/tables/code/T01
    [HttpGet("code/{tableNumber}")]
    public async Task<ActionResult<TableResponse>> GetTableByNumber(
        string tableNumber)
    {
        var table = await _context.RestaurantTables
            .AsNoTracking()
            .Where(x =>
                x.TableNumber == tableNumber &&
                x.IsActive)
            .Select(x => new TableResponse
            {
                Id = x.Id,
                TableNumber = x.TableNumber,
                QRCode = x.QRCode,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        if (table == null)
        {
            return NotFound(new
            {
                message = "Table not found or inactive."
            });
        }

        return Ok(table);
    }

    // POST: api/tables
    [HttpPost]
    public async Task<ActionResult<TableResponse>> CreateTable(
        CreateTableRequest request)
    {
        var tableNumber = request.TableNumber.Trim();

        var exists = await _context.RestaurantTables
            .AnyAsync(x => x.TableNumber == tableNumber);

        if (exists)
        {
            return Conflict(new
            {
                message = "A table with this number already exists."
            });
        }

        var table = new RestaurantTable
        {
            TableNumber = tableNumber,
            QRCode = request.QRCode,
            IsActive = request.IsActive
        };

        _context.RestaurantTables.Add(table);

        await _context.SaveChangesAsync();

        var response = new TableResponse
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            QRCode = table.QRCode,
            IsActive = table.IsActive
        };

        return CreatedAtAction(
            nameof(GetTable),
            new { id = table.Id },
            response);
    }

    // PUT: api/tables/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTable(
        int id,
        UpdateTableRequest request)
    {
        var table = await _context.RestaurantTables
            .FirstOrDefaultAsync(x => x.Id == id);

        if (table == null)
        {
            return NotFound(new
            {
                message = "Table not found."
            });
        }

        var tableNumber = request.TableNumber.Trim();

        var duplicate = await _context.RestaurantTables
            .AnyAsync(x =>
                x.Id != id &&
                x.TableNumber == tableNumber);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Another table with this number already exists."
            });
        }

        table.TableNumber = tableNumber;
        table.QRCode = request.QRCode;
        table.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/tables/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTable(int id)
    {
        var table = await _context.RestaurantTables
            .FirstOrDefaultAsync(x => x.Id == id);

        if (table == null)
        {
            return NotFound(new
            {
                message = "Table not found."
            });
        }

        _context.RestaurantTables.Remove(table);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
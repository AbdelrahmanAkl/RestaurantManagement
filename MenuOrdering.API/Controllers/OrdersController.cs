using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Orders;
using MenuOrdering.API.Enums;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MenuDbContext _context;
    private readonly IConfiguration _configuration;

    public OrdersController(
        MenuDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // GET: api/Orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrders()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Table)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(MapToResponse));
    }

    // GET: api/Orders/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Table)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        return Ok(MapToResponse(order));
    }

    // GET: api/Orders/table/1
    [HttpGet("table/{tableId:int}")]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrdersByTable(
        int tableId)
    {
        var tableExists = await _context.RestaurantTables
            .AnyAsync(x => x.Id == tableId);

        if (!tableExists)
        {
            return NotFound(new
            {
                message = "Table not found."
            });
        }

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(x => x.TableId == tableId)
            .Include(x => x.Table)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(MapToResponse));
    }

    // POST: api/Orders
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        CreateOrderRequest request)
    {
        var table = await _context.RestaurantTables
            .FirstOrDefaultAsync(x =>
                x.Id == request.TableId &&
                x.IsActive);

        if (table == null)
        {
            return BadRequest(new
            {
                message = "The selected table does not exist or is inactive."
            });
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new
            {
                message = "An order must contain at least one item."
            });
        }

        var menuItemIds = request.Items
            .Select(x => x.MenuItemId)
            .Distinct()
            .ToList();

        var menuItems = await _context.MenuItems
            .Where(x =>
                menuItemIds.Contains(x.Id) &&
                x.IsAvailable)
            .ToListAsync();

        if (menuItems.Count != menuItemIds.Count)
        {
            var foundIds = menuItems
                .Select(x => x.Id)
                .ToHashSet();

            var unavailableItems = menuItemIds
                .Where(x => !foundIds.Contains(x))
                .ToList();

            return BadRequest(new
            {
                message = "One or more menu items do not exist or are unavailable.",
                menuItemIds = unavailableItems
            });
        }

        var taxRate = _configuration
            .GetValue<decimal>("OrderSettings:TaxRate");

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var order = new Order
            {
                TableId = table.Id,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            decimal subTotal = 0;

            foreach (var requestItem in request.Items)
            {
                var menuItem = menuItems.First(x =>
                    x.Id == requestItem.MenuItemId);

                var unitPrice = menuItem.Price;
                var totalPrice = unitPrice * requestItem.Quantity;

                var orderItem = new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = requestItem.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Notes = requestItem.Notes?.Trim()
                };

                order.OrderItems.Add(orderItem);

                subTotal += totalPrice;
            }

            var tax = Math.Round(
                subTotal * taxRate,
                2,
                MidpointRounding.AwayFromZero);

            var total = subTotal + tax;

            order.SubTotal = subTotal;
            order.Tax = tax;
            order.Total = total;

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _context.Entry(order)
                .Reference(x => x.Table)
                .LoadAsync();

            foreach (var item in order.OrderItems)
            {
                await _context.Entry(item)
                    .Reference(x => x.MenuItem)
                    .LoadAsync();
            }

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = order.Id },
                MapToResponse(order));
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An error occurred while creating the order."
                });
        }
    }

    // PATCH: api/Orders/1/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(
        int id,
        [FromBody] OrderStatus status)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        if (order.Status == OrderStatus.Completed ||
            order.Status == OrderStatus.Cancelled)
        {
            return BadRequest(new
            {
                message = "Completed or cancelled orders cannot be updated."
            });
        }

        if (!Enum.IsDefined(typeof(OrderStatus), status))
        {
            return BadRequest(new
            {
                message = "Invalid order status."
            });
        }

        order.Status = status;

        if (status == OrderStatus.Completed)
        {
            order.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/Orders/1/cancel
    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        if (order.Status == OrderStatus.Completed)
        {
            return BadRequest(new
            {
                message = "A completed order cannot be cancelled."
            });
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return BadRequest(new
            {
                message = "Order is already cancelled."
            });
        }

        order.Status = OrderStatus.Cancelled;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            TableId = order.TableId,
            TableNumber = order.Table.TableNumber,
            Status = order.Status,
            SubTotal = order.SubTotal,
            Tax = order.Tax,
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,

            Items = order.OrderItems
                .Select(x => new OrderItemResponse
                {
                    Id = x.Id,
                    MenuItemId = x.MenuItemId,
                    MenuItemName = x.MenuItem.Name,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.TotalPrice,
                    Notes = x.Notes
                })
                .ToList()
        };
    }
}
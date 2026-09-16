using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Bills;
using MenuOrdering.API.Enums;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly MenuDbContext _context;

    public BillsController(MenuDbContext context)
    {
        _context = context;
    }

    // GET: api/Bills/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BillResponse>> GetBill(int id)
    {
        var bill = await _context.Bills
            .AsNoTracking()
            .Include(x => x.Order)
                .ThenInclude(x => x.Table)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (bill == null)
        {
            return NotFound(new
            {
                message = "Bill not found."
            });
        }

        return Ok(MapToResponse(bill));
    }

    // GET: api/Bills/order/1
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<BillResponse>> GetBillByOrder(
        int orderId)
    {
        var bill = await _context.Bills
            .AsNoTracking()
            .Include(x => x.Order)
                .ThenInclude(x => x.Table)
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (bill == null)
        {
            return NotFound(new
            {
                message = "Bill has not been requested for this order."
            });
        }

        return Ok(MapToResponse(bill));
    }

    // POST: api/Bills/order/1/request
    [HttpPost("order/{orderId:int}/request")]
    public async Task<ActionResult<BillResponse>> RequestBill(
        int orderId,
        RequestBillRequest request)
    {
        var order = await _context.Orders
            .Include(x => x.Table)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return BadRequest(new
            {
                message = "Cannot request a bill for a cancelled order."
            });
        }

        // Prevent creating more than one bill for the same order.
        var existingBill = await _context.Bills
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (existingBill != null)
        {
            return Conflict(new
            {
                message = "A bill has already been requested for this order.",
                billId = existingBill.Id
            });
        }

        if (request.ServiceChargeRate < 0 ||
            request.ServiceChargeRate > 1)
        {
            return BadRequest(new
            {
                message = "Service charge rate must be between 0 and 1."
            });
        }

        var serviceCharge = Math.Round(
            order.SubTotal * request.ServiceChargeRate,
            2,
            MidpointRounding.AwayFromZero);

        var total = order.SubTotal
                    + order.Tax
                    + serviceCharge;

        var bill = new Bill
        {
            OrderId = order.Id,
            SubTotal = order.SubTotal,
            Tax = order.Tax,
            ServiceCharge = serviceCharge,
            Total = total,
            RequestedAt = DateTime.UtcNow
        };

        _context.Bills.Add(bill);

        await _context.SaveChangesAsync();

        var response = MapToResponse(bill, order);

        return CreatedAtAction(
            nameof(GetBill),
            new { id = bill.Id },
            response);
    }

    // PATCH: api/Bills/1/paid
    [HttpPatch("{id:int}/paid")]
    public async Task<IActionResult> MarkBillAsPaid(int id)
    {
        var bill = await _context.Bills
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (bill == null)
        {
            return NotFound(new
            {
                message = "Bill not found."
            });
        }

        if (bill.PaidAt != null)
        {
            return BadRequest(new
            {
                message = "Bill is already marked as paid."
            });
        }

        bill.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static BillResponse MapToResponse(Bill bill)
    {
        return new BillResponse
        {
            Id = bill.Id,
            OrderId = bill.OrderId,
            TableId = bill.Order.TableId,
            TableNumber = bill.Order.Table.TableNumber,
            SubTotal = bill.SubTotal,
            Tax = bill.Tax,
            ServiceCharge = bill.ServiceCharge,
            Total = bill.Total,
            RequestedAt = bill.RequestedAt,
            PaidAt = bill.PaidAt
        };
    }

    private static BillResponse MapToResponse(
        Bill bill,
        Order order)
    {
        return new BillResponse
        {
            Id = bill.Id,
            OrderId = bill.OrderId,
            TableId = order.TableId,
            TableNumber = order.Table.TableNumber,
            SubTotal = bill.SubTotal,
            Tax = bill.Tax,
            ServiceCharge = bill.ServiceCharge,
            Total = bill.Total,
            RequestedAt = bill.RequestedAt,
            PaidAt = bill.PaidAt
        };
    }
}
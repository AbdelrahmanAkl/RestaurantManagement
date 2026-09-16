using MenuOrdering.API.Data;
using MenuOrdering.API.DTOs.Payments;
using MenuOrdering.API.Enums;
using MenuOrdering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
private readonly MenuDbContext _context;
public PaymentsController(MenuDbContext context)
{
    _context = context;
}

[HttpGet("{id:int}")]
public async Task<ActionResult<PaymentResponse>> GetPayment(int id)
{
    var payment = await _context.Payments
        .AsNoTracking()
        .Include(x => x.Order)
            .ThenInclude(x => x.Bill)
        .FirstOrDefaultAsync(x => x.Id == id);

    if (payment == null)
    {
        return NotFound(new
        {
            message = "Payment not found."
        });
    }

    return Ok(MapToResponse(payment));
}

[HttpGet("order/{orderId:int}")]
public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetPaymentsByOrder(
    int orderId)
{
    var orderExists = await _context.Orders
        .AnyAsync(x => x.Id == orderId);

    if (!orderExists)
    {
        return NotFound(new
        {
            message = "Order not found."
        });
    }

    var payments = await _context.Payments
        .AsNoTracking()
        .Include(x => x.Order)
            .ThenInclude(x => x.Bill)
        .Where(x => x.OrderId == orderId)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    return Ok(payments.Select(MapToResponse));
}

[HttpPost("order/{orderId:int}")]
public async Task<ActionResult<PaymentResponse>> CreatePayment(
    int orderId,
    CreatePaymentRequest request)
{
    var paymentMethod = request.PaymentMethod?.Trim();

    if (string.IsNullOrWhiteSpace(paymentMethod))
    {
        return BadRequest(new
        {
            message = "Payment method is required."
        });
    }

    var order = await _context.Orders
        .Include(x => x.Bill)
        .Include(x => x.Payments)
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
            message = "Cannot create a payment for a cancelled order."
        });
    }

    if (order.Bill == null)
    {
        return BadRequest(new
        {
            message = "A bill must be requested before creating a payment."
        });
    }

    if (order.Bill.PaidAt != null)
    {
        return BadRequest(new
        {
            message = "This bill has already been paid."
        });
    }

    var existingPayment = order.Payments
        .FirstOrDefault(x =>
            x.Status == PaymentStatus.Pending ||
            x.Status == PaymentStatus.Processing);

    if (existingPayment != null)
    {
        return Conflict(new
        {
            message = "There is already an active payment for this order.",
            paymentId = existingPayment.Id
        });
    }

    var payment = new Payment
    {
        OrderId = order.Id,
        Amount = order.Bill.Total,
        Status = PaymentStatus.Pending,
        PaymentMethod = paymentMethod,
        CreatedAt = DateTime.UtcNow
    };

    _context.Payments.Add(payment);

    await _context.SaveChangesAsync();

    payment.Order = order;

    return CreatedAtAction(
        nameof(GetPayment),
        new { id = payment.Id },
        MapToResponse(payment));
}

[HttpPatch("{id:int}/status")]
public async Task<IActionResult> UpdatePaymentStatus(
    int id,
    UpdatePaymentStatusRequest request)
{
    var payment = await _context.Payments
        .Include(x => x.Order)
            .ThenInclude(x => x.Bill)
        .FirstOrDefaultAsync(x => x.Id == id);

    if (payment == null)
    {
        return NotFound(new
        {
            message = "Payment not found."
        });
    }

    if (!Enum.IsDefined(typeof(PaymentStatus), request.Status))
    {
        return BadRequest(new
        {
            message = "Invalid payment status."
        });
    }

    if (payment.Status == PaymentStatus.Refunded)
    {
        return BadRequest(new
        {
            message = "A refunded payment cannot be updated."
        });
    }

    if (payment.Status == PaymentStatus.Paid &&
        request.Status != PaymentStatus.Refunded)
    {
        return BadRequest(new
        {
            message = "A paid payment can only be refunded."
        });
    }

    if (request.Status == PaymentStatus.Refunded &&
        payment.Status != PaymentStatus.Paid)
    {
        return BadRequest(new
        {
            message = "Only a paid payment can be refunded."
        });
    }

    payment.Status = request.Status;

    if (!string.IsNullOrWhiteSpace(request.TransactionReference))
    {
        payment.TransactionReference =
            request.TransactionReference.Trim();
    }

    if (request.Status == PaymentStatus.Paid)
    {
        payment.PaidAt = DateTime.UtcNow;

        if (payment.Order.Bill != null)
        {
            payment.Order.Bill.PaidAt = DateTime.UtcNow;
        }

        if (payment.Order.Status != OrderStatus.Completed)
        {
            payment.Order.Status = OrderStatus.Completed;
            payment.Order.CompletedAt = DateTime.UtcNow;
        }
    }

    if (request.Status == PaymentStatus.Failed)
    {
        payment.PaidAt = null;
    }

    if (request.Status == PaymentStatus.Refunded)
    {
        payment.PaidAt = null;

        if (payment.Order.Bill != null)
        {
            payment.Order.Bill.PaidAt = null;
        }

        if (payment.Order.Status == OrderStatus.Completed)
        {
            payment.Order.Status = OrderStatus.Served;
            payment.Order.CompletedAt = null;
        }
    }

    await _context.SaveChangesAsync();

    return NoContent();
}

private static PaymentResponse MapToResponse(Payment payment)
{
    return new PaymentResponse
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        BillId = payment.Order.Bill?.Id ?? 0,
        Amount = payment.Amount,
        Status = payment.Status,
        PaymentMethod = payment.PaymentMethod,
        TransactionReference = payment.TransactionReference,
        CreatedAt = payment.CreatedAt,
        PaidAt = payment.PaidAt
    };
}
}

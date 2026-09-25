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

    // GET: api/Payments/1
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

        var totalPaid = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.OrderId == payment.OrderId &&
                x.Status == PaymentStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        return Ok(MapToResponse(
            payment,
            totalPaid
        ));
    }

    // GET: api/Payments/order/1
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetPaymentsByOrder(
        int orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Bill)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        if (order.Bill == null)
        {
            return BadRequest(new
            {
                message = "This order does not have a bill."
            });
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var totalPaid = payments
            .Where(x => x.Status == PaymentStatus.Paid)
            .Sum(x => x.Amount);

        var responses = payments
            .Select(payment =>
                MapToResponse(payment, totalPaid, order.Bill.Total))
            .ToList();

        return Ok(responses);
    }

    // GET: api/Payments/order/1/summary
    [HttpGet("order/{orderId:int}/summary")]
    public async Task<IActionResult> GetPaymentSummary(int orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Bill)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        if (order.Bill == null)
        {
            return BadRequest(new
            {
                message = "This order does not have a bill."
            });
        }

        var totalPaid = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.OrderId == orderId &&
                x.Status == PaymentStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var remainingAmount = Math.Max(
            0,
            order.Bill.Total - totalPaid
        );

        string paymentStatus;

        if (totalPaid <= 0)
        {
            paymentStatus = "Unpaid";
        }
        else if (remainingAmount > 0)
        {
            paymentStatus = "PartiallyPaid";
        }
        else
        {
            paymentStatus = "Paid";
        }

        return Ok(new
        {
            orderId = order.Id,
            billId = order.Bill.Id,
            billTotal = order.Bill.Total,
            totalPaid,
            remainingAmount,
            paymentStatus,
            billPaidAt = order.Bill.PaidAt
        });
    }

    // POST: api/Payments/order/1
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

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Payment amount must be greater than zero."
            });
        }

        var allowedPaymentMethods = new[]
        {
            "Cash",
            "Card",
            "Wallet",
            "Online"
        };

        if (!allowedPaymentMethods.Any(x =>
                x.Equals(
                    paymentMethod,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new
            {
                message = "Invalid payment method.",
                allowedPaymentMethods
            });
        }

        var order = await _context.Orders
            .Include(x => x.Bill)
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

        var totalPaid = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.OrderId == orderId &&
                x.Status == PaymentStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var remainingAmount = order.Bill.Total - totalPaid;

        if (remainingAmount <= 0)
        {
            return BadRequest(new
            {
                message = "This bill has already been fully paid.",
                billTotal = order.Bill.Total,
                totalPaid,
                remainingAmount = 0
            });
        }

        if (request.Amount > remainingAmount)
        {
            return BadRequest(new
            {
                message = "Payment amount cannot exceed the remaining balance.",
                billTotal = order.Bill.Total,
                totalPaid,
                remainingAmount,
                requestedAmount = request.Amount
            });
        }

        var isCashPayment =
            paymentMethod.Equals(
                "Cash",
                StringComparison.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = request.Amount,
            Status = isCashPayment
                ? PaymentStatus.Paid
                : PaymentStatus.Pending,
            PaymentMethod = paymentMethod,
            CreatedAt = now,
            PaidAt = isCashPayment
                ? now
                : null
        };

        _context.Payments.Add(payment);

        // Cash is considered paid immediately.
        if (isCashPayment)
        {
            var newTotalPaid = totalPaid + request.Amount;

            if (newTotalPaid >= order.Bill.Total)
            {
                order.Bill.PaidAt = now;
            }
        }

        await _context.SaveChangesAsync();

        // Make the newly created payment available for the response.
        var responseTotalPaid = isCashPayment
            ? totalPaid + request.Amount
            : totalPaid;

        return CreatedAtAction(
            nameof(GetPayment),
            new { id = payment.Id },
            MapToResponse(
                payment,
                responseTotalPaid,
                order.Bill.Total));
    }

    // PATCH: api/Payments/1/status
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

        if (!Enum.IsDefined(
                typeof(PaymentStatus),
                request.Status))
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

        // Once a payment is Paid, it can only be refunded.
        if (payment.Status == PaymentStatus.Paid &&
            request.Status != PaymentStatus.Refunded)
        {
            return BadRequest(new
            {
                message = "A paid payment can only be refunded."
            });
        }

        // Only a Paid payment can be refunded.
        if (request.Status == PaymentStatus.Refunded &&
            payment.Status != PaymentStatus.Paid)
        {
            return BadRequest(new
            {
                message = "Only a paid payment can be refunded."
            });
        }

        if (!string.IsNullOrWhiteSpace(
                request.TransactionReference))
        {
            payment.TransactionReference =
                request.TransactionReference.Trim();
        }

        if (request.Status == PaymentStatus.Paid)
        {
            var alreadyPaid = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.OrderId == payment.OrderId &&
                    x.Id != payment.Id &&
                    x.Status == PaymentStatus.Paid)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var billTotal = payment.Order.Bill?.Total ?? 0;

            if (alreadyPaid + payment.Amount > billTotal)
            {
                return BadRequest(new
                {
                    message = "This payment would exceed the bill total.",
                    billTotal,
                    alreadyPaid,
                    paymentAmount = payment.Amount,
                    remainingBeforePayment =
                        billTotal - alreadyPaid
                });
            }

            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
        }
        else if (request.Status == PaymentStatus.Failed)
        {
            payment.Status = PaymentStatus.Failed;
            payment.PaidAt = null;
        }
        else if (request.Status == PaymentStatus.Processing)
        {
            payment.Status = PaymentStatus.Processing;
            payment.PaidAt = null;
        }
        else if (request.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Pending;
            payment.PaidAt = null;
        }
        else if (request.Status == PaymentStatus.Refunded)
        {
            payment.Status = PaymentStatus.Refunded;
            payment.PaidAt = null;
        }

        await _context.SaveChangesAsync();

        await UpdateBillPaymentStatus(payment.OrderId);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task UpdateBillPaymentStatus(int orderId)
    {
        var order = await _context.Orders
            .Include(x => x.Bill)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order?.Bill == null)
        {
            return;
        }

        var totalPaid = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.OrderId == orderId &&
                x.Status == PaymentStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        if (totalPaid >= order.Bill.Total)
        {
            if (order.Bill.PaidAt == null)
            {
                order.Bill.PaidAt = DateTime.UtcNow;
            }
        }
        else
        {
            order.Bill.PaidAt = null;
        }
    }

    private static PaymentResponse MapToResponse(
        Payment payment,
        decimal totalPaid,
        decimal? billTotalOverride = null)
    {
        var billTotal =
            billTotalOverride ??
            payment.Order?.Bill?.Total ??
            0;

        var remainingAmount = Math.Max(
            0,
            billTotal - totalPaid);

        return new PaymentResponse
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            BillId = payment.Order?.Bill?.Id ?? 0,
            Amount = payment.Amount,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference =
                payment.TransactionReference,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt,
            BillTotal = billTotal,
            TotalPaid = totalPaid,
            RemainingAmount = remainingAmount
        };
    }
}
using System.Security.Claims;
using CreditManagement.Application.DTOs;
using CreditManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditManagement.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST /api/payments — pay a bill
    [HttpPost]
    public async Task<IActionResult> PayBill([FromBody] PayBillRequestDto request)
    {
        var userId = GetUserId();
        var payment = await _paymentService.PayBillAsync(userId, request);
        return CreatedAtAction(nameof(GetPaymentDetails), new { paymentId = payment.Id }, payment);
    }

    // GET /api/payments — get payment history for the authenticated user
    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();
        var payments = await _paymentService.GetHistoryAsync(userId);
        return Ok(payments);
    }

    // GET /api/payments/{paymentId} — get payment details with bill and card info
    [HttpGet("{paymentId}")]
    public async Task<IActionResult> GetPaymentDetails(Guid paymentId)
    {
        var userId = GetUserId();
        var payment = await _paymentService.GetPaymentDetailsAsync(userId, paymentId);
        return Ok(payment);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst("UserId")!.Value);
}

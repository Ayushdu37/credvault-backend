using System.Security.Claims;
using CreditManagement.Application.DTOs;
using CreditManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditManagement.API.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly IBillService _billService;

    public BillsController(IBillService billService)
    {
        _billService = billService;
    }

    // POST /api/bills/{cardId} — generate a new bill for a card
    [HttpPost("{cardId}")]
    public async Task<IActionResult> GenerateBill(Guid cardId, [FromBody] GenerateBillRequestDto request)
    {
        var userId = GetUserId();
        var bill = await _billService.GenerateBillAsync(userId, cardId, request);
        return CreatedAtAction(nameof(GetBillDetails), new { billId = bill.Id }, bill);
    }

    // GET /api/bills — list all bills for the authenticated user
    [HttpGet]
    public async Task<IActionResult> GetBills()
    {
        var userId = GetUserId();
        var bills = await _billService.GetBillsByUserIdAsync(userId);
        return Ok(bills);
    }

    // GET /api/bills/{billId} — get bill details with payments and card info
    [HttpGet("{billId}")]
    public async Task<IActionResult> GetBillDetails(Guid billId)
    {
        var userId = GetUserId();
        var bill = await _billService.GetBillDetailsAsync(userId, billId);
        return Ok(bill);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst("UserId")!.Value);
}

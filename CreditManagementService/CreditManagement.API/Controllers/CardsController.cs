using System.Security.Claims;
using CreditManagement.Application.DTOs;
using CreditManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditManagement.API.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService)
    {
        _cardService = cardService;
    }

    // POST /api/cards — register a new credit card
    [HttpPost]
    public async Task<IActionResult> AddCard([FromBody] AddCardRequestDto request)
    {
        var userId = GetUserId();
        var result = await _cardService.AddCardAsync(userId, request);
        return CreatedAtAction(nameof(GetCardDetails), new { cardId = result.Id }, result);
    }

    // GET /api/cards — list all cards for the authenticated user
    [HttpGet]
    public async Task<IActionResult> GetCards()
    {
        var userId = GetUserId();
        var cards = await _cardService.GetCardsByUserIdAsync(userId);
        return Ok(cards);
    }

    // GET /api/cards/{cardId} — get card details with recent bills
    [HttpGet("{cardId}")]
    public async Task<IActionResult> GetCardDetails(Guid cardId)
    {
        var userId = GetUserId();
        var card = await _cardService.GetCardDetailsAsync(userId, cardId);
        return Ok(card);
    }

    // PUT /api/cards/{cardId} — update card holder name and credit limit
    [HttpPut("{cardId}")]
    public async Task<IActionResult> UpdateCard(Guid cardId, [FromBody] UpdateCardRequestDto request)
    {
        var userId = GetUserId();
        var card = await _cardService.UpdateCardAsync(userId, cardId, request);
        return Ok(card);
    }

    // DELETE /api/cards/{cardId} — remove a card
    [HttpDelete("{cardId}")]
    public async Task<IActionResult> DeleteCard(Guid cardId)
    {
        var userId = GetUserId();
        await _cardService.DeleteCardAsync(userId, cardId);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst("UserId")!.Value);
}

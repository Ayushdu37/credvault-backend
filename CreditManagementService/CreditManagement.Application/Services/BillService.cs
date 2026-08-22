using CreditManagement.Application.DTOs;
using CreditManagement.Application.Exceptions;
using CreditManagement.Application.Interfaces;
using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Enums;
using CreditManagement.Domain.Interfaces;

namespace CreditManagement.Application.Services;

// Orchestrates Bill/Statement generation, validation, and status tracking
public class BillService : IBillService
{
    private readonly IBillRepository _billRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IPaymentRepository _paymentRepository;

    public BillService(
        IBillRepository billRepository,
        ICardRepository cardRepository,
        IPaymentRepository paymentRepository)
    {
        _billRepository = billRepository;
        _cardRepository = cardRepository;
        _paymentRepository = paymentRepository;
    }

    // ── 1. GENERATE BILL FOR A CARD ──
    public async Task<BillResponseDto> GenerateBillAsync(Guid userId, Guid cardId, GenerateBillRequestDto request)
    {
        // 1. Verify card exists and is owned by the authenticated user
        var card = await _cardRepository.GetByIdAsync(cardId)
            ?? throw new NotFoundException($"Card with ID '{cardId}' not found.");

        if (card.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to generate bills for this card.");
        }

        // 2. Validate billing cycle dates
        if (request.BillingCycleStart >= request.BillingCycleEnd)
        {
            throw new CustomValidationException("BillingCycleStart must be earlier than BillingCycleEnd.");
        }

        // 3. Compute Minimum Due: defaults to 5% of total amount (rounded to 2 decimal places)
        decimal minDue = request.MinimumDue ?? Math.Round(request.TotalAmount * 0.05m, 2);

        if (minDue > request.TotalAmount)
        {
            throw new CustomValidationException("Minimum due cannot be greater than the total bill amount.");
        }

        // 4. Compute Due Date: defaults to cycle end date + 18 days
        DateOnly dueDate = request.DueDate ?? request.BillingCycleEnd.AddDays(18);

        // 5. Create new Bill entity
        var bill = new Bill
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            BillingCycleStart = request.BillingCycleStart,
            BillingCycleEnd = request.BillingCycleEnd,
            TotalAmount = request.TotalAmount,
            MinimumDue = minDue,
            DueDate = dueDate,
            Status = request.TotalAmount == 0 ? BillStatus.Paid : BillStatus.Unpaid,
            GeneratedAt = DateTime.UtcNow
        };

        // 6. Update card's running outstanding balance
        card.OutstandingAmount += request.TotalAmount;

        await _billRepository.AddAsync(bill);
        await _cardRepository.UpdateAsync(card);

        return MapToResponseDto(bill);
    }

    // ── 2. GET ALL BILLS FOR LOGGED-IN USER ──
    public async Task<IEnumerable<BillResponseDto>> GetBillsByUserIdAsync(Guid userId)
    {
        var bills = await _billRepository.GetByUserIdAsync(userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Auto-evaluate overdue status if due date has passed and bill remains unpaid
        return bills.Select(b =>
        {
            var status = (b.Status == BillStatus.Unpaid && today > b.DueDate)
                ? BillStatus.Overdue
                : b.Status;

            return new BillResponseDto
            {
                Id = b.Id,
                CardId = b.CardId,
                BillingCycleStart = b.BillingCycleStart,
                BillingCycleEnd = b.BillingCycleEnd,
                TotalAmount = b.TotalAmount,
                MinimumDue = b.MinimumDue,
                DueDate = b.DueDate,
                Status = status,
                GeneratedAt = b.GeneratedAt
            };
        });
    }

    // ── 3. GET BILL DETAILS BY ID (WITH PAYMENTS & CARD SUMMARY) ──
    public async Task<BillDetailsDto> GetBillDetailsAsync(Guid userId, Guid billId)
    {
        var bill = await _billRepository.GetByIdAsync(billId)
            ?? throw new NotFoundException($"Bill with ID '{billId}' not found.");

        // Security check: verify the authenticated user owns the card for this bill
        if (bill.Card.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to view this bill.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var status = (bill.Status == BillStatus.Unpaid && today > bill.DueDate)
            ? BillStatus.Overdue
            : bill.Status;

        return new BillDetailsDto
        {
            Id = bill.Id,
            CardId = bill.CardId,
            BillingCycleStart = bill.BillingCycleStart,
            BillingCycleEnd = bill.BillingCycleEnd,
            TotalAmount = bill.TotalAmount,
            MinimumDue = bill.MinimumDue,
            DueDate = bill.DueDate,
            Status = status,
            GeneratedAt = bill.GeneratedAt,
            CardHolderName = bill.Card.CardHolderName,
            CardNumberMasked = bill.Card.CardNumberMasked,
            Issuer = bill.Card.Issuer,
            Payments = bill.Payments.Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                BillId = p.BillId,
                UserId = p.UserId,
                Amount = p.Amount,
                TransactionReference = p.TransactionReference,
                PaymentStatus = p.PaymentStatus,
                PaymentDate = p.PaymentDate
            })
        };
    }

    // ── HELPER: DTO MAPPING ──
    private static BillResponseDto MapToResponseDto(Bill bill) => new()
    {
        Id = bill.Id,
        CardId = bill.CardId,
        BillingCycleStart = bill.BillingCycleStart,
        BillingCycleEnd = bill.BillingCycleEnd,
        TotalAmount = bill.TotalAmount,
        MinimumDue = bill.MinimumDue,
        DueDate = bill.DueDate,
        Status = bill.Status,
        GeneratedAt = bill.GeneratedAt
    };
}

using CreditManagement.Application.DTOs;
using CreditManagement.Application.Exceptions;
using CreditManagement.Application.Interfaces;
using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Enums;
using CreditManagement.Domain.Interfaces;

namespace CreditManagement.Application.Services;

// Orchestrates payment processing, duplicate guard checks, and account balance reconciliation
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBillRepository _billRepository;
    private readonly ICardRepository _cardRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBillRepository billRepository,
        ICardRepository cardRepository)
    {
        _paymentRepository = paymentRepository;
        _billRepository = billRepository;
        _cardRepository = cardRepository;
    }

    // ── 1. PAY A BILL (IDEMPOTENT & RECONCILED) ──
    public async Task<PaymentResponseDto> PayBillAsync(Guid userId, PayBillRequestDto request)
    {
        // 1. Idempotency Check: prevent duplicate payments using client transaction reference
        var existingPayment = await _paymentRepository.GetByTransactionReferenceAsync(request.TransactionReference);
        if (existingPayment is not null)
        {
            throw new ConflictException($"A payment with Transaction Reference '{request.TransactionReference}' already exists.");
        }

        // 2. Fetch bill and verify ownership
        var bill = await _billRepository.GetByIdAsync(request.BillId)
            ?? throw new NotFoundException($"Bill with ID '{request.BillId}' not found.");

        if (bill.Card.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to pay this bill.");
        }

        // 3. Prevent payments on already settled bills
        if (bill.Status == BillStatus.Paid)
        {
            throw new CustomValidationException("This bill is already fully paid.");
        }

        // 4. Calculate remaining balance on the bill
        var totalPaid = bill.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        var remainingDue = bill.TotalAmount - totalPaid;

        if (request.Amount > remainingDue)
        {
            throw new CustomValidationException($"Payment amount (₹{request.Amount}) exceeds remaining unpaid balance (₹{remainingDue}).");
        }

        // 5. Create Payment record
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BillId = bill.Id,
            UserId = userId,
            Amount = request.Amount,
            TransactionReference = request.TransactionReference.Trim(),
            PaymentStatus = PaymentStatus.Success,
            PaymentDate = DateTime.UtcNow
        };

        // 6. Check if this payment completes the full bill amount
        if (totalPaid + request.Amount >= bill.TotalAmount)
        {
            bill.Status = BillStatus.Paid;
        }

        // 7. Reduce card's running outstanding balance
        bill.Card.OutstandingAmount = Math.Max(0, bill.Card.OutstandingAmount - request.Amount);

        // 8. Persist changes across repositories
        await _paymentRepository.AddAsync(payment);
        await _billRepository.UpdateAsync(bill);
        await _cardRepository.UpdateAsync(bill.Card);

        return MapToResponseDto(payment);
    }

    // ── 2. GET PAYMENT HISTORY (SORTED NEWEST FIRST) ──
    public async Task<IEnumerable<PaymentResponseDto>> GetHistoryAsync(Guid userId)
    {
        var payments = await _paymentRepository.GetByUserIdAsync(userId);

        return payments
            .OrderByDescending(p => p.PaymentDate)
            .Select(MapToResponseDto);
    }

    // ── 3. GET PAYMENT DETAILS BY ID ──
    public async Task<PaymentDetailsDto> GetPaymentDetailsAsync(Guid userId, Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId)
            ?? throw new NotFoundException($"Payment with ID '{paymentId}' not found.");

        if (payment.UserId != userId)
        {
            throw new UnauthorizedException("You do not have permission to view this payment.");
        }

        var totalPaid = payment.Bill.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        var remainingDue = Math.Max(0, payment.Bill.TotalAmount - totalPaid);

        return new PaymentDetailsDto
        {
            Id = payment.Id,
            BillId = payment.BillId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            TransactionReference = payment.TransactionReference,
            PaymentStatus = payment.PaymentStatus,
            PaymentDate = payment.PaymentDate,
            BillTotalAmount = payment.Bill.TotalAmount,
            BillRemainingDue = remainingDue,
            CardNumberMasked = payment.Bill.Card.CardNumberMasked,
            Issuer = payment.Bill.Card.Issuer
        };
    }

    // ── HELPER: DTO MAPPING ──
    private static PaymentResponseDto MapToResponseDto(Payment payment) => new()
    {
        Id = payment.Id,
        BillId = payment.BillId,
        UserId = payment.UserId,
        Amount = payment.Amount,
        TransactionReference = payment.TransactionReference,
        PaymentStatus = payment.PaymentStatus,
        PaymentDate = payment.PaymentDate
    };
}

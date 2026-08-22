using CreditManagement.Application.DTOs;
using CreditManagement.Application.Exceptions;
using CreditManagement.Application.Services;
using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Enums;
using CreditManagement.Domain.Interfaces;
using Moq;

namespace CreditManagement.Tests;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IBillRepository> _billRepo = new();
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly PaymentService _paymentService;
    private readonly Guid _userId = Guid.NewGuid();

    public PaymentServiceTests()
    {
        _paymentService = new PaymentService(_paymentRepo.Object, _billRepo.Object, _cardRepo.Object);
    }

    private Bill CreateUnpaidBill(decimal totalAmount, decimal alreadyPaid = 0)
    {
        var card = new Card { Id = Guid.NewGuid(), UserId = _userId, OutstandingAmount = totalAmount };
        var bill = new Bill
        {
            Id = Guid.NewGuid(), CardId = card.Id, Card = card,
            TotalAmount = totalAmount, Status = BillStatus.Unpaid,
            Payments = new List<Payment>()
        };
        if (alreadyPaid > 0)
        {
            bill.Payments.Add(new Payment { Amount = alreadyPaid, PaymentStatus = PaymentStatus.Success });
        }
        return bill;
    }

    // ── PASS TESTS ──

    [Fact]
    public async Task PayBill_WithFullAmount_ReturnSuccessStatus()
    {
        var bill = CreateUnpaidBill(5000);
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        var request = new PayBillRequestDto { BillId = bill.Id, Amount = 5000, TransactionReference = "TXN-001" };
        var result = await _paymentService.PayBillAsync(_userId, request);

        Assert.Equal(PaymentStatus.Success, result.PaymentStatus);
        Assert.Equal(5000, result.Amount);
    }

    [Fact]
    public async Task PayBill_WithFullAmount_SetsBillStatusToPaid()
    {
        var bill = CreateUnpaidBill(5000);
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        await _paymentService.PayBillAsync(_userId, new PayBillRequestDto { BillId = bill.Id, Amount = 5000, TransactionReference = "TXN-002" });

        Assert.Equal(BillStatus.Paid, bill.Status);
    }

    [Fact]
    public async Task PayBill_PartialPayment_BillRemainsUnpaid()
    {
        var bill = CreateUnpaidBill(10000);
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        await _paymentService.PayBillAsync(_userId, new PayBillRequestDto { BillId = bill.Id, Amount = 3000, TransactionReference = "TXN-003" });

        Assert.Equal(BillStatus.Unpaid, bill.Status); // Still unpaid — only partial
    }

    [Fact]
    public async Task PayBill_DecreasesCardOutstandingAmount()
    {
        var bill = CreateUnpaidBill(5000);
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        await _paymentService.PayBillAsync(_userId, new PayBillRequestDto { BillId = bill.Id, Amount = 2000, TransactionReference = "TXN-004" });

        Assert.Equal(3000m, bill.Card.OutstandingAmount); // 5000 - 2000
    }

    [Fact]
    public async Task GetHistory_ReturnsPaymentsSortedByDate()
    {
        var payments = new List<Payment>
        {
            new() { Id = Guid.NewGuid(), UserId = _userId, Amount = 1000, PaymentDate = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = _userId, Amount = 2000, PaymentDate = DateTime.UtcNow }
        };
        _paymentRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(payments);

        var result = (await _paymentService.GetHistoryAsync(_userId)).ToList();

        Assert.Equal(2000, result.First().Amount); // Newest first
    }

    // ── FAIL TESTS ──

    [Fact]
    public async Task PayBill_WithDuplicateTransactionRef_ThrowsConflict()
    {
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync("TXN-DUPLICATE"))
            .ReturnsAsync(new Payment { TransactionReference = "TXN-DUPLICATE" });

        var request = new PayBillRequestDto { BillId = Guid.NewGuid(), Amount = 1000, TransactionReference = "TXN-DUPLICATE" };

        await Assert.ThrowsAsync<ConflictException>(() => _paymentService.PayBillAsync(_userId, request));
    }

    [Fact]
    public async Task PayBill_ForNonExistentBill_ThrowsNotFound()
    {
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Bill?)null);

        var request = new PayBillRequestDto { BillId = Guid.NewGuid(), Amount = 1000, TransactionReference = "TXN-005" };

        await Assert.ThrowsAsync<NotFoundException>(() => _paymentService.PayBillAsync(_userId, request));
    }

    [Fact]
    public async Task PayBill_ForOtherUsersBill_ThrowsUnauthorized()
    {
        var bill = new Bill
        {
            Id = Guid.NewGuid(), Status = BillStatus.Unpaid, TotalAmount = 5000,
            Card = new Card { UserId = Guid.NewGuid() }, // Different user
            Payments = new List<Payment>()
        };
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        var request = new PayBillRequestDto { BillId = bill.Id, Amount = 1000, TransactionReference = "TXN-006" };

        await Assert.ThrowsAsync<UnauthorizedException>(() => _paymentService.PayBillAsync(_userId, request));
    }

    [Fact]
    public async Task PayBill_OnAlreadyPaidBill_ThrowsValidation()
    {
        var bill = CreateUnpaidBill(5000);
        bill.Status = BillStatus.Paid; // Already paid
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        var request = new PayBillRequestDto { BillId = bill.Id, Amount = 1000, TransactionReference = "TXN-007" };

        await Assert.ThrowsAsync<CustomValidationException>(() => _paymentService.PayBillAsync(_userId, request));
    }

    [Fact]
    public async Task PayBill_AmountExceedsRemaining_ThrowsValidation()
    {
        var bill = CreateUnpaidBill(5000, alreadyPaid: 4000); // Only 1000 remaining
        _paymentRepo.Setup(r => r.GetByTransactionReferenceAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        _billRepo.Setup(r => r.GetByIdAsync(bill.Id)).ReturnsAsync(bill);

        var request = new PayBillRequestDto { BillId = bill.Id, Amount = 2000, TransactionReference = "TXN-008" };

        await Assert.ThrowsAsync<CustomValidationException>(() => _paymentService.PayBillAsync(_userId, request));
    }
}

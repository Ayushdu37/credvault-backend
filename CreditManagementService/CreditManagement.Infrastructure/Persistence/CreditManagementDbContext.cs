using CreditManagement.Domain.Entities;
using CreditManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CreditManagement.Infrastructure.Persistence;

// DbContext for the CreditManagementDb — manages Cards, Bills, and Payments tables
public class CreditManagementDbContext : DbContext
{
    public CreditManagementDbContext(DbContextOptions<CreditManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Card> Cards { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Card Table ──
        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("Cards");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasDefaultValueSql("NEWID()");

            // UserId is a soft reference — no SQL FK to IdentityServiceDb (separate microservice DB)
            entity.Property(c => c.UserId).IsRequired();
            entity.HasIndex(c => c.UserId); // Index for fast lookups by user

            entity.Property(c => c.CardHolderName).IsRequired().HasMaxLength(150);
            entity.Property(c => c.CardNumberMasked).IsRequired().HasMaxLength(25);

            // Unique hash for duplicate card detection — never store raw card numbers
            entity.Property(c => c.CardNumberHash).IsRequired();
            entity.HasIndex(c => c.CardNumberHash).IsUnique();

            entity.Property(c => c.ExpiryMonth).IsRequired();
            entity.Property(c => c.ExpiryYear).IsRequired();
            entity.Property(c => c.Issuer).IsRequired().HasMaxLength(50);

            entity.Property(c => c.CreditLimit).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(c => c.OutstandingAmount).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Bill Table ──
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.ToTable("Bills");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).HasDefaultValueSql("NEWID()");

            entity.Property(b => b.BillingCycleStart).IsRequired();
            entity.Property(b => b.BillingCycleEnd).IsRequired();
            entity.Property(b => b.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(b => b.MinimumDue).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(b => b.DueDate).IsRequired();

            // Store enum as string in DB for readability (e.g., "Unpaid" instead of 1)
            entity.Property(b => b.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(BillStatus.Unpaid);

            entity.Property(b => b.GeneratedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Relationship: Bill belongs to Card — cascade delete removes bills when card is deleted
            entity.HasOne(b => b.Card)
                .WithMany(c => c.Bills)
                .HasForeignKey(b => b.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Payment Table ──
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasDefaultValueSql("NEWID()");

            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Amount).IsRequired().HasColumnType("decimal(18,2)");

            // Unique transaction reference — DB-level idempotency guard against duplicate payments
            entity.Property(p => p.TransactionReference).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.TransactionReference).IsUnique();

            entity.Property(p => p.PaymentStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(p => p.PaymentDate).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Relationship: Payment belongs to Bill (no cascade — we don't want deleting a bill to wipe payment history)
            entity.HasOne(p => p.Bill)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

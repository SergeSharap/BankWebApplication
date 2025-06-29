using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure.Data;

public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration for Client
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        });

        // Configuration for Transaction hierarchy (TPH - Table Per Hierarchy)
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Type).IsRequired().HasConversion<string>();
            entity.Property(e => e.DateTime).IsRequired();
            
            // Discriminator for TPH
            entity.HasDiscriminator<TransactionType>(e => e.Type)
                .HasValue<CreditTransaction>(TransactionType.Credit)
                .HasValue<DebitTransaction>(TransactionType.Debit)
                .HasValue<RevertTransaction>(TransactionType.Revert);
            
            // Index for querying by ClientId
            entity.HasIndex(e => e.ClientId).HasDatabaseName("IX_Transactions_ClientId");
        });

        // Configuration for RevertTransaction specific properties
        modelBuilder.Entity<RevertTransaction>(entity =>
        {
            entity.Property(e => e.RevertedTransactionId).IsRequired(false);
            
            // Foreign key relationship - RevertTransaction references another Transaction
            entity.HasOne<Transaction>()
                .WithMany()
                .HasForeignKey(e => e.RevertedTransactionId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
            
            // Index for querying by RevertedTransactionId (for idempotency check)
            entity.HasIndex(e => e.RevertedTransactionId).HasDatabaseName("IX_RevertTransactions_RevertedTransactionId");
        });
    }
} 
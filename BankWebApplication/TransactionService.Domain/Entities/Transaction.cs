namespace TransactionService.Domain.Entities;

public enum TransactionType
{
    Credit,
    Debit,
    Revert
}

public abstract class Transaction : ITransaction
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; }
    public TransactionType Type { get; set; }
}

public class CreditTransaction : Transaction
{
    public CreditTransaction()
    {
        Type = TransactionType.Credit;
    }
}

public class DebitTransaction : Transaction
{
    public DebitTransaction()
    {
        Type = TransactionType.Debit;
    }
}

public class RevertTransaction : Transaction
{
    public RevertTransaction()
    {
        Type = TransactionType.Revert;
    }

    public Guid? RevertedTransactionId { get; set; }
} 
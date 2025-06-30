namespace TransactionService.Domain.Exceptions
{
    public sealed class DuplicateTransactionException : Exception
    {
        public Guid TransactionId { get; }
        public DuplicateTransactionException(Guid id) : base($"Transaction {id} already exists")
        {
            TransactionId = id;
        }
    }
}

using System.Data;
using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<T?> GetTransactionByIdAsync<T>(Guid id) where T : Transaction;
    Task<RevertTransaction?> GetRevertTransactionByRevertedIdAsync(Guid revertedTransactionId);
    Task AddTransactionAsync(Transaction transaction);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel level = IsolationLevel.ReadCommitted);
    Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel level = IsolationLevel.ReadCommitted);
} 
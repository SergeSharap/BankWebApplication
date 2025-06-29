using System.Data;
using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client> GetOrCreateAsync(Guid id);
    Task<decimal> AdjustBalanceAsync(Guid clientId, decimal amount);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel level);
    Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel level);
} 
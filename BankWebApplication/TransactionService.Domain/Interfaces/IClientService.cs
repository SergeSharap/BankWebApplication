using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Interfaces;

public interface IClientService
{
    Task<Client?> GetClientAsync(Guid clientId);
    Task<decimal> GetBalanceAsync(Guid clientId);
    Task<bool> HasSufficientFunds(Guid clientId, decimal amount);
    Task<decimal> AdjustBalanceAsync(Guid clientId, decimal amount);
} 
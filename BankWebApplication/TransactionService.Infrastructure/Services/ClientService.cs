using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;

    public ClientService(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<decimal> GetBalanceAsync(Guid clientId)
    {
        var client = await _clientRepository.GetByIdAsync(clientId);
        return client?.Balance ?? 0m;
    }

    public async Task<Client?> GetClientAsync(Guid clientId)
    {
        return await _clientRepository.GetByIdAsync(clientId);
    }

    public async Task<bool> HasSufficientFunds(Guid clientId, decimal amount)
    {
        var client = await _clientRepository.GetByIdAsync(clientId);
        return (client?.Balance ?? 0m) >= amount;
    }

    public async Task<decimal> AdjustBalanceAsync(Guid clientId, decimal amount)
    {
        return await _clientRepository.ExecuteInTransactionAsync(async () =>
        {
            return await _clientRepository.AdjustBalanceAsync(clientId, amount);
        }, System.Data.IsolationLevel.Serializable);
    }
} 
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Data;

namespace TransactionService.Infrastructure.Repositories;

public class ClientRepository : BaseRepository, IClientRepository
{
    public ClientRepository(BankDbContext context) : base(context)
    {
    }

    public async Task<Client?> GetByIdAsync(Guid id)
    {
        return await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Client> GetOrCreateAsync(Guid id)
    {
        var client = await GetByIdAsync(id);
        
        if (client is null)
        {
            client = new Client
            {
                Id = id,
                Balance = 0
            };
            _context.Clients.Add(client);
        }

        return client;
    }

    public async Task<decimal> AdjustBalanceAsync(Guid clientId, decimal amount)
    {
        var client = await GetOrCreateAsync(clientId);
        client.Balance += amount;
        await _context.SaveChangesAsync();
        
        return client.Balance;
    }
} 
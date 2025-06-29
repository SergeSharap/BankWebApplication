using Microsoft.EntityFrameworkCore;
using System.Data;
using TransactionService.Infrastructure.Data;

namespace TransactionService.Infrastructure.Repositories;

public abstract class BaseRepository
{
    protected readonly BankDbContext _context;

    protected BaseRepository(BankDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel level)
    {
        // Check if there's already an active transaction
        var existingTransaction = _context.Database.CurrentTransaction;
        
        if (existingTransaction != null)
        {
            // If transaction already exists, just execute the operation
            var result = await operation();
            await _context.SaveChangesAsync();
            return result;
        }

        // If no transaction exists, create a new one
        using var transaction = await _context.Database.BeginTransactionAsync(level);
        try
        {
            var result = await operation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel level)
    {
        // Check if there's already an active transaction
        var existingTransaction = _context.Database.CurrentTransaction;
        
        if (existingTransaction != null)
        {
            // If transaction already exists, just execute the operation
            await operation();
            await _context.SaveChangesAsync();
            return;
        }

        // If no transaction exists, create a new one
        using var transaction = await _context.Database.BeginTransactionAsync(level);
        try
        {
            await operation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
} 
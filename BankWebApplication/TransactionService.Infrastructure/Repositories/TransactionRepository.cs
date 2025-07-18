using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Exceptions;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Data;
using TransactionService.Infrastructure.Helpers;

namespace TransactionService.Infrastructure.Repositories;

public class TransactionRepository : BaseRepository, ITransactionRepository
{
    public TransactionRepository(BankDbContext context) : base(context)
    {
    }

    public async Task<T?> GetTransactionByIdAsync<T>(Guid id) where T : Transaction
    {
        return await _context.Transactions.OfType<T>().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<RevertTransaction?> GetRevertTransactionByRevertedIdAsync(Guid revertedTransactionId)
    {
        return await _context.Transactions.OfType<RevertTransaction>()
            .FirstOrDefaultAsync(t => t.RevertedTransactionId == revertedTransactionId);
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        try
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (SqlExceptionHelper.IsPkOrUniqueViolation(ex))
        {
            throw new DuplicateTransactionException(transaction.Id);
        }
    }
} 
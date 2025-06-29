using Microsoft.Extensions.Logging;
using TransactionService.Application.DTOs;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Services;

public class TransactionApplicationService : ITransactionApplicationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IClientService _balanceService;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<TransactionApplicationService> _logger;

    public TransactionApplicationService(
        ITransactionRepository transactionRepository,
        IClientService balanceService,
        IDateTimeProvider timeProvider,
        ILogger<TransactionApplicationService> logger)
    {
        _transactionRepository = transactionRepository;
        _balanceService = balanceService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TransactionResponse> CreditAsync(TransactionRequest request)
    {
        var existingTransaction = await _transactionRepository.GetTransactionByIdAsync<Transaction>(request.Id);
        if (existingTransaction is not null)
        {
            return await CreateIdempotentResponse(existingTransaction);
        }

        return await _transactionRepository.ExecuteInTransactionAsync(async () =>
        {
            var transaction = new CreditTransaction
            {
                Id = request.Id,
                ClientId = request.ClientId,
                Amount = request.Amount,
                DateTime = _timeProvider.UtcNow
            };

            await _transactionRepository.AddTransactionAsync(transaction);

            var newBalance = await _balanceService.AdjustBalanceAsync(request.ClientId, request.Amount);

            _logger.LogInformation("Credit transaction {TransactionId} processed successfully. New balance: {Balance}", request.Id, newBalance);

            return new TransactionResponse
            {
                InsertDateTime = transaction.DateTime,
                ClientBalance = newBalance
            };
        }, System.Data.IsolationLevel.Serializable);
    }

    public async Task<TransactionResponse> DebitAsync(TransactionRequest request)
    {
        var existingTransaction = await _transactionRepository.GetTransactionByIdAsync<Transaction>(request.Id);
        if (existingTransaction is not null)
        {
            return await CreateIdempotentResponse(existingTransaction);
        }

        return await _transactionRepository.ExecuteInTransactionAsync(async () =>
        {
            var hasFunds = await _balanceService.HasSufficientFunds(request.ClientId, request.Amount);
            if (!hasFunds)
            {
                var currentBalance = await _balanceService.GetBalanceAsync(request.ClientId);
                throw new InvalidOperationException(
                    $"Insufficient funds for debit operation. Current balance: {currentBalance}, amount requested: {request.Amount}");
            }

            var transaction = new DebitTransaction
            {
                Id = request.Id,
                ClientId = request.ClientId,
                Amount = request.Amount,
                DateTime = _timeProvider.UtcNow
            };

            await _transactionRepository.AddTransactionAsync(transaction);

            var newBalance = await _balanceService.AdjustBalanceAsync(request.ClientId, -request.Amount);

            _logger.LogInformation("Debit transaction {TransactionId} processed successfully. New balance: {Balance}", request.Id, newBalance);

            return new TransactionResponse
            {
                InsertDateTime = transaction.DateTime,
                ClientBalance = newBalance
            };
        }, System.Data.IsolationLevel.Serializable);
    }

    public async Task<RevertResponse> RevertAsync(Guid transactionId)
    {
        // Check idempotency
        var existingRevert = await _transactionRepository.GetRevertTransactionByRevertedIdAsync(transactionId);
        if (existingRevert is not null)
        {
            _logger.LogInformation("Revert for transaction {TransactionId} already exists, returning existing result", transactionId);
            var balance = await _balanceService.GetBalanceAsync(existingRevert.ClientId);
            return new RevertResponse
            {
                RevertDateTime = existingRevert.DateTime,
                ClientBalance = balance
            };
        }

        return await _transactionRepository.ExecuteInTransactionAsync(async () =>
        {
            var transactionToRevert = await _transactionRepository.GetTransactionByIdAsync<Transaction>(transactionId);
            
            if (transactionToRevert is null)
            {
                throw new KeyNotFoundException($"Transaction {transactionId} not found");
            }
            else if (transactionToRevert.Type == TransactionType.Revert)
            {
                throw new InvalidOperationException($"Transaction {transactionId} is already a revert transaction");
            }

            // Check sufficient funds to revert credit transaction (when we need to debit money back)
            if (transactionToRevert.Type == TransactionType.Credit)
            {
                var hasFunds = await _balanceService.HasSufficientFunds(transactionToRevert.ClientId, transactionToRevert.Amount);
                if (!hasFunds)
                {
                    var currentBalance = await _balanceService.GetBalanceAsync(transactionToRevert.ClientId);
                    throw new InvalidOperationException(
                        $"Insufficient funds to revert credit transaction. Current balance: {currentBalance}, amount to revert: {transactionToRevert.Amount}");
                }
            }

            var revertTransaction = new RevertTransaction
            {
                Id = Guid.NewGuid(),
                ClientId = transactionToRevert.ClientId,
                Amount = transactionToRevert.Type == TransactionType.Credit ? -transactionToRevert.Amount : transactionToRevert.Amount,
                DateTime = _timeProvider.UtcNow,
                RevertedTransactionId = transactionId
            };

            await _transactionRepository.AddTransactionAsync(revertTransaction);

            var newBalance = await _balanceService.AdjustBalanceAsync(transactionToRevert.ClientId, revertTransaction.Amount);

            _logger.LogInformation("Revert for transaction {TransactionId} processed successfully. New balance: {Balance}", transactionId, newBalance);

            return new RevertResponse
            {
                RevertDateTime = revertTransaction.DateTime,
                ClientBalance = newBalance
            };
        }, System.Data.IsolationLevel.Serializable);
    }

    public async Task<BalanceResponse> GetBalanceAsync(Guid clientId)
    {
        var client = await _balanceService.GetClientAsync(clientId);
        if (client is null)
        {
            _logger.LogWarning("Client {ClientId} not found", clientId);
            throw new KeyNotFoundException($"Client {clientId} not found");
        }

        return new BalanceResponse
        {
            BalanceDateTime = _timeProvider.UtcNow,
            ClientBalance = client.Balance
        };
    }

    private async Task<TransactionResponse> CreateIdempotentResponse(Transaction existingTransaction)
    {
        _logger.LogInformation("Transaction {TransactionId} already exists, returning existing result", existingTransaction.Id);
        var balance = await _balanceService.GetBalanceAsync(existingTransaction.ClientId);
        return new TransactionResponse
        {
            InsertDateTime = existingTransaction.DateTime,
            ClientBalance = balance
        };
    }
} 
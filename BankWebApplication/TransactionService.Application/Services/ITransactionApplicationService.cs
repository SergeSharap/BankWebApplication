using TransactionService.Application.DTOs;

namespace TransactionService.Application.Services;

public interface ITransactionApplicationService
{
    Task<TransactionResponse> CreditAsync(TransactionRequest request);
    Task<TransactionResponse> DebitAsync(TransactionRequest request);
    Task<RevertResponse> RevertAsync(Guid transactionId);
    Task<BalanceResponse> GetBalanceAsync(Guid clientId);
} 
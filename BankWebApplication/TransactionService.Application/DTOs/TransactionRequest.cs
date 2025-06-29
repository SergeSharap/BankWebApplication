namespace TransactionService.Application.DTOs;

public class TransactionRequest
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Amount { get; set; }
} 
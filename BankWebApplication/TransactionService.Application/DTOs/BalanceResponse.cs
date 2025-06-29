namespace TransactionService.Application.DTOs
{
    public class BalanceResponse
    {
        public DateTime BalanceDateTime { get; set; }
        public decimal ClientBalance { get; set; }
    }
}

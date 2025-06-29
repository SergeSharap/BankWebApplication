namespace TransactionService.Application.DTOs
{
    public class RevertResponse
    {
        public DateTime RevertDateTime { get; set; }
        public decimal ClientBalance { get; set; }
    }
}

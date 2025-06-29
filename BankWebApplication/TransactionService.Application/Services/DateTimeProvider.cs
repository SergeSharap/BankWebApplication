namespace TransactionService.Application.Services;

/// <summary>
/// Default implementation of IDateTimeProvider using system DateTime
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;
} 
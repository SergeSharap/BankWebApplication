using FluentValidation;
using TransactionService.Application.DTOs;
using TransactionService.Application.Services;

namespace TransactionService.Application.Validators;

public class TransactionRequestValidator : AbstractValidator<TransactionRequest>
{
    public TransactionRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("Client ID is required");

        RuleFor(x => x.DateTime)
            .NotEmpty()
            .WithMessage("Transaction date is required")
            .Must(date => date <= dateTimeProvider.UtcNow)
            .WithMessage("Transaction date cannot be in the future");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive");
    }
} 
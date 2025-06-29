using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TransactionService.Application.DTOs;
using TransactionService.Application.Services;

namespace TransactionService.API.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionApplicationService _transactionService;
    private readonly ILogger<TransactionController> _logger;
    private readonly IValidator<TransactionRequest> _validator;

    public TransactionController(
        ITransactionApplicationService transactionService, 
        ILogger<TransactionController> logger,
        IValidator<TransactionRequest> validator)
    {
        _transactionService = transactionService;
        _logger = logger;
        _validator = validator;
    }

    [HttpPost("credit")]
    public async Task<ActionResult<TransactionResponse>> Credit(TransactionRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);
        
        var response = await _transactionService.CreditAsync(request);
        return Ok(response);
    }

    [HttpPost("debit")]
    public async Task<ActionResult<TransactionResponse>> Debit(TransactionRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var response = await _transactionService.DebitAsync(request);
        return Ok(response);
    }

    [HttpPost("revert")]
    public async Task<ActionResult<RevertResponse>> Revert(Guid id)
    {
        var response = await _transactionService.RevertAsync(id);
        return Ok(response);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalance(Guid id)
    {
        var response = await _transactionService.GetBalanceAsync(id);
        return Ok(response);
    }
} 
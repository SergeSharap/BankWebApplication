using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Data;
using TransactionService.Application.DTOs;
using TransactionService.Application.Services;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Tests.Application;

[TestFixture]
public class TransactionApplicationServiceTests
{
    private Mock<ITransactionRepository> _mockRepository;
    private Mock<IClientService> _mockBalanceService;
    private Mock<ILogger<TransactionApplicationService>> _mockLogger;
    private Mock<IDateTimeProvider> _mockTimeProvider;
    private TransactionApplicationService _service;
    private readonly DateTime _testDateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockBalanceService = new Mock<IClientService>();
        _mockLogger = new Mock<ILogger<TransactionApplicationService>>();
        _mockTimeProvider = new Mock<IDateTimeProvider>();
        
        _mockTimeProvider.Setup(t => t.UtcNow).Returns(_testDateTime);
        
        _service = new TransactionApplicationService(
            _mockRepository.Object, 
            _mockBalanceService.Object,
            _mockTimeProvider.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task CreditAsync_NewTransaction_ShouldCreateAndReturnBalance()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = DateTime.UtcNow.AddDays(-1),
            Amount = 100.00m
        };

        _mockRepository.Setup(r => r.GetTransactionByIdAsync<CreditTransaction>(request.Id))
            .ReturnsAsync((CreditTransaction?)null);
        
        _mockRepository.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<TransactionResponse>>>(), It.IsAny<IsolationLevel>()))
            .Returns((Func<Task<TransactionResponse>> func, IsolationLevel level) => func());
        
        _mockBalanceService.Setup(b => b.AdjustBalanceAsync(request.ClientId, request.Amount))
            .ReturnsAsync(100.00m);

        _mockRepository.Setup(r => r.AddTransactionAsync(It.IsAny<CreditTransaction>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreditAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ClientBalance, Is.EqualTo(100.00m));
            Assert.That(result.InsertDateTime, Is.EqualTo(_testDateTime));
            _mockRepository.Verify(r => r.AddTransactionAsync(It.Is<CreditTransaction>(t => 
                t.Type == TransactionType.Credit && 
                t.DateTime == _testDateTime)), Times.Once);
        });
    }

    [Test]
    public async Task CreditAsync_ExistingTransaction_ShouldReturnExistingResult()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = DateTime.UtcNow.AddDays(-1),
            Amount = 100.00m
        };

        var existingTransaction = new CreditTransaction
        {
            Id = request.Id,
            ClientId = request.ClientId,
            Amount = request.Amount,
            DateTime = DateTime.UtcNow.AddHours(-1)
        };

        _mockRepository.Setup(r => r.GetTransactionByIdAsync<Transaction>(request.Id))
            .ReturnsAsync(existingTransaction);
        _mockBalanceService.Setup(b => b.GetBalanceAsync(request.ClientId))
            .ReturnsAsync(100.00m);

        // Act
        var result = await _service.CreditAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.InsertDateTime, Is.EqualTo(existingTransaction.DateTime));
            _mockRepository.Verify(r => r.AddTransactionAsync(It.IsAny<CreditTransaction>()), Times.Never);
        });
    }

    [Test]
    public void DebitAsync_InsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = DateTime.UtcNow.AddDays(-1),
            Amount = 100.00m
        };

        _mockRepository.Setup(r => r.GetTransactionByIdAsync<DebitTransaction>(request.Id))
            .ReturnsAsync((DebitTransaction?)null);
        
        _mockRepository.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<TransactionResponse>>>(), It.IsAny<IsolationLevel>()))
            .Returns((Func<Task<TransactionResponse>> func, IsolationLevel level) => func());
        
        _mockBalanceService.Setup(b => b.HasSufficientFunds(request.ClientId, request.Amount))
            .ReturnsAsync(false);
        _mockBalanceService.Setup(b => b.GetBalanceAsync(request.ClientId))
            .ReturnsAsync(50.00m);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DebitAsync(request));
        
        Assert.That(exception.Message, Does.Contain("Insufficient funds"));
    }

    [Test]
    public async Task DebitAsync_SufficientFunds_ShouldProcessTransaction()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = DateTime.UtcNow.AddDays(-1),
            Amount = 50.00m
        };

        _mockRepository.Setup(r => r.GetTransactionByIdAsync<DebitTransaction>(request.Id))
            .ReturnsAsync((DebitTransaction?)null);
        
        _mockRepository.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<TransactionResponse>>>(), It.IsAny<IsolationLevel>()))
            .Returns((Func<Task<TransactionResponse>> func, IsolationLevel level) => func());
        
        _mockBalanceService.Setup(b => b.HasSufficientFunds(request.ClientId, request.Amount))
            .ReturnsAsync(true);
        _mockBalanceService.Setup(b => b.GetBalanceAsync(request.ClientId))
            .ReturnsAsync(100.00m);
        _mockBalanceService.Setup(b => b.AdjustBalanceAsync(request.ClientId, -request.Amount))
            .ReturnsAsync(100.00m);
        _mockRepository.Setup(r => r.AddTransactionAsync(It.IsAny<DebitTransaction>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DebitAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ClientBalance, Is.EqualTo(100.00m));
            Assert.That(result.InsertDateTime, Is.EqualTo(_testDateTime));
            _mockRepository.Verify(r => r.AddTransactionAsync(It.Is<DebitTransaction>(t => 
                t.Type == TransactionType.Debit && 
                t.DateTime == _testDateTime)), Times.Once);
        });
    }

    [Test]
    public void RevertAsync_TransactionNotFound_ShouldThrowException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetRevertTransactionByRevertedIdAsync(transactionId))
            .ReturnsAsync((RevertTransaction?)null);
        _mockRepository.Setup(r => r.GetTransactionByIdAsync<Transaction>(transactionId))
            .ReturnsAsync((Transaction?)null);
        
        _mockRepository.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<RevertResponse>>>(), It.IsAny<IsolationLevel>()))
            .Returns((Func<Task<RevertResponse>> func, IsolationLevel level) => func());

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.RevertAsync(transactionId));
        
        Assert.That(exception.Message, Does.Contain("not found"));
    }

    [Test]
    public async Task RevertAsync_ExistingRevert_ShouldReturnExistingResult()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var existingRevert = new RevertTransaction
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            Amount = -100.00m,
            DateTime = DateTime.UtcNow.AddHours(-1),
            RevertedTransactionId = transactionId
        };

        _mockRepository.Setup(r => r.GetRevertTransactionByRevertedIdAsync(transactionId))
            .ReturnsAsync(existingRevert);
        _mockBalanceService.Setup(b => b.GetBalanceAsync(existingRevert.ClientId))
            .ReturnsAsync(50.00m);

        // Act
        var result = await _service.RevertAsync(transactionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.RevertDateTime, Is.EqualTo(existingRevert.DateTime));
            Assert.That(result.ClientBalance, Is.EqualTo(50.00m));
            _mockRepository.Verify(r => r.AddTransactionAsync(It.IsAny<RevertTransaction>()), Times.Never);
        });
    }

    [Test]
    public async Task RevertAsync_CreditTransaction_ShouldCreateRevertTransaction()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transactionToRevert = new CreditTransaction
        {
            Id = transactionId,
            ClientId = Guid.NewGuid(),
            Amount = 100.00m,
            DateTime = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository.Setup(r => r.GetRevertTransactionByRevertedIdAsync(transactionId))
            .ReturnsAsync((RevertTransaction?)null);
        _mockRepository.Setup(r => r.GetTransactionByIdAsync<Transaction>(transactionId))
            .ReturnsAsync(transactionToRevert);
        
        _mockRepository.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<RevertResponse>>>(), It.IsAny<IsolationLevel>()))
            .Returns((Func<Task<RevertResponse>> func, IsolationLevel level) => func());
        
        _mockBalanceService.Setup(b => b.HasSufficientFunds(transactionToRevert.ClientId, transactionToRevert.Amount))
            .ReturnsAsync(true);
        _mockBalanceService.Setup(b => b.AdjustBalanceAsync(transactionToRevert.ClientId, -transactionToRevert.Amount))
            .ReturnsAsync(0.00m);
        _mockRepository.Setup(r => r.AddTransactionAsync(It.IsAny<RevertTransaction>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RevertAsync(transactionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.RevertDateTime, Is.EqualTo(_testDateTime));
            _mockRepository.Verify(r => r.AddTransactionAsync(It.Is<RevertTransaction>(t => 
                t.Type == TransactionType.Revert && 
                t.Amount == -transactionToRevert.Amount &&
                t.RevertedTransactionId == transactionId)), Times.Once);
        });
    }

    [Test]
    public async Task GetBalanceAsync_ShouldReturnBalance()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = new Client { Id = clientId, Balance = 250.00m };
        
        _mockBalanceService.Setup(b => b.GetClientAsync(clientId))
            .ReturnsAsync(client);

        // Act
        var result = await _service.GetBalanceAsync(clientId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ClientBalance, Is.EqualTo(250.00m));
            Assert.That(result.BalanceDateTime, Is.EqualTo(_testDateTime));
        });
    }

    [Test]
    public void GetBalanceAsync_ClientNotFound_ShouldThrowException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        
        _mockBalanceService.Setup(b => b.GetClientAsync(clientId))
            .ReturnsAsync((Client?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetBalanceAsync(clientId));
        
        Assert.That(exception.Message, Does.Contain("not found"));
    }
} 
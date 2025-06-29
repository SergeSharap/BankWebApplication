using FluentValidation.TestHelper;
using Moq;
using NUnit.Framework;
using TransactionService.Application.DTOs;
using TransactionService.Application.Services;
using TransactionService.Application.Validators;

namespace TransactionService.Tests.Application;

[TestFixture]
public class TransactionRequestValidatorTests
{
    private Mock<IDateTimeProvider> _mockDateTimeProvider;
    private TransactionRequestValidator _validator;
    private readonly DateTime _testDateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void Setup()
    {
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockDateTimeProvider.Setup(dp => dp.UtcNow).Returns(_testDateTime);
        
        _validator = new TransactionRequestValidator(_mockDateTimeProvider.Object);
    }

    [Test]
    public void ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = _testDateTime.AddDays(-1), // Past date
            Amount = 100.00m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void EmptyId_ShouldFailValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.Empty,
            ClientId = Guid.NewGuid(),
            DateTime = _testDateTime.AddDays(-1),
            Amount = 100.00m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void EmptyClientId_ShouldFailValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.Empty,
            DateTime = _testDateTime.AddDays(-1),
            Amount = 100.00m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Test]
    public void FutureDate_ShouldFailValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = _testDateTime.AddDays(1), // Future date
            Amount = 100.00m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateTime);
    }

    [Test]
    public void ZeroAmount_ShouldFailValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = _testDateTime.AddDays(-1),
            Amount = 0m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void NegativeAmount_ShouldFailValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = _testDateTime.AddDays(-1),
            Amount = -100.00m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void CurrentDate_ShouldPassValidation()
    {
        // Arrange
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DateTime = _testDateTime, // Current date
            Amount = 100.00m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateTime);
    }
} 
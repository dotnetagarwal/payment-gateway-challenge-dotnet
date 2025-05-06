using Moq;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Services.Tests;

public class PaymentsServiceTests
{
    private readonly Mock<IBankClient> _bankClientMock = new();
    private readonly Mock<IPaymentsRepository> _repoMock = new();
    private readonly IPaymentsService _sut;

    public PaymentsServiceTests()
    {
        _sut= new PaymentsService(_bankClientMock.Object, _repoMock.Object);
    }

    private PaymentRequest ReturnsSampleBankRequest(string cardNumber = "4111111111111111") =>
        new PaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1234,
            Cvv = 321
        };

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankResponseIsNull_ReturnsDeclined_AndDoesNotSave()
    {
        // Arrange
        _bankClientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync((BankResponse?)null);

        var req = ReturnsSampleBankRequest("4000000000000003"); 

        // Act
        var result = await _sut.ProcessPaymentAsync(req);

        // Assert
        Assert.Equal(PaymentStatus.Declined, result.Status);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PaymentResponse>()), Times.Never);
    }

    [Theory]
    [InlineData(true, "411111111111111", PaymentStatus.Authorized)]
    [InlineData(false, "411111111111112", PaymentStatus.Declined)]
    public async Task ProcessPaymentAsync_WithBankResponse_SavesAndReturnsCorrectStatus(bool isAuth, string cardNumber, PaymentStatus expected)
    {
        // Arrange
        _bankClientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync(new BankResponse { IsAuthorized = isAuth });

        var req = ReturnsSampleBankRequest(cardNumber);

        // Act
        var result = await _sut.ProcessPaymentAsync(req);

        // Assert
        Assert.Equal(expected, result.Status);
        _repoMock.Verify(r => r.AddAsync(It.Is<PaymentResponse>(
            p => p.Status == expected &&
                 p.CardNumberLastFour == req.CardNumber.Substring(req.CardNumber.Length - 4) &&
                 p.Amount == req.Amount &&
                 p.Currency == req.Currency
        )), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAsync_AfterAddingToDictionary_ReturnsTheSame()
    {
        // Arrange
        var repo = new PaymentsRepository();
        var sut = new PaymentsService(_bankClientMock.Object, repo);
        var payment = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "9999",
            ExpiryMonth = 7,
            ExpiryYear = 2028,
            Currency = "GBP",
            Amount = 1500
        };
        await repo.AddAsync(payment);

        // Act
        var result = await sut.GetPaymentAsync(payment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.Id);
        Assert.Equal(payment.Status, result.Status);
        Assert.Equal(payment.CardNumberLastFour, result.CardNumberLastFour);
        Assert.Equal(payment.Currency, result.Currency);
        Assert.Equal(payment.Amount, result.Amount);
    }
}


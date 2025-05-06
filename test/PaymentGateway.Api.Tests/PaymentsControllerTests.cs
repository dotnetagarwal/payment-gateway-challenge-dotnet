using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Services;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Exceptions;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_InvalidModel_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        // Send an empty object: required fields missing
        var response = await client.PostAsJsonAsync("/api/Payments", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ExpiryInPast_ReturnsBadRequestWithExpiryMessage()
    {
        var client = _factory.CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = 1,
            ExpiryYear = DateTime.UtcNow.Year - 1,  // last year
            Currency = "USD",
            Amount = 100,
            Cvv = 123
        };

        var response = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Equal(ErrorMessages.InvalidCardExpiryDate, errorMessage);
    }

    [Fact]
    public async Task Post_IncorrectCurrency_ReturnsBadRequestWithCurrencyMessage()
    {
        var client = _factory.CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = 1,
            ExpiryYear = DateTime.UtcNow.Year + 1, 
            Currency = "INR",
            Amount = 100,
            Cvv = 123
        };

        var response = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.True(problemDetails!.Errors.ContainsKey("Currency"));
        Assert.Contains(ErrorMessages.NonSupportedCurrency, problemDetails.Errors["Currency"]);
    }

    [Fact]
    public async Task Post_IncorrectCardNumber_ReturnsBadRequestWithCardNumberMessage()
    {
        var client = _factory.CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "411111",
            ExpiryMonth = 1,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 100,
            Cvv = 123
        };

        var response = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorMessage = await response.Content.ReadAsStringAsync();

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.True(problemDetails!.Errors.ContainsKey("CardNumber"));
        Assert.Contains(ErrorMessages.IncorrectCardNumberLength, problemDetails.Errors["CardNumber"]);
    }

    [Theory]
    [InlineData(true, "411111111111111", PaymentStatus.Authorized)]
    [InlineData(false, "411111111111112", PaymentStatus.Declined)]
    public async Task Post_ValidRequest_WithBankResponse_ReturnsOk(bool isAuthorized, string cardNumber, PaymentStatus expectedPaymentStatus)
    {
        var client = GetCustomizedFactoryClient(isAuthorized);

        var request = new PaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 250,
            Cvv = 321
        };

        var postResponse = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var actualPaymentResponse = await postResponse.Content.ReadFromJsonAsync<PaymentResponse>(new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        Assert.NotNull(actualPaymentResponse);
        Assert.Equal(request.CardNumber.Substring(request.CardNumber.Length - 4), actualPaymentResponse!.CardNumberLastFour);
        Assert.Equal(expectedPaymentStatus, actualPaymentResponse.Status);
        Assert.Equal(request.Amount, actualPaymentResponse.Amount);
        Assert.Equal(request.Currency, actualPaymentResponse.Currency);
    }

    [Fact]
    public async Task Post_WhenBankReturns503_ReturnsServiceUnavailable()
    {
        var bankClientMock = GetBankClientServiceUnavailableMock();
        var client = GetWebHostBuilderClient(bankClientMock);

        var request = new PaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 100,
            Cvv = 123
        };

        var response = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(ErrorMessages.BankCurrentlyUnavailable, content); 
    }

    [Fact]
    public async Task Get_NonexistentId_ReturnsNotFound()
    {
        var client = GetCustomizedFactoryClient();

        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingId_ReturnsOk()
    {
        var client = GetCustomizedFactoryClient();

        var request = new PaymentRequest
        {
            CardNumber = "411111111111111",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 250,
            Cvv = 321
        };

        var postResponse = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var postPaymentResponse = await postResponse.Content.ReadFromJsonAsync<PaymentResponse>(new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/Payments/{postPaymentResponse!.Id}");
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var getPaymentResponse = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(postPaymentResponse.CardNumberLastFour, getPaymentResponse!.CardNumberLastFour);
        Assert.Equal(postPaymentResponse.Status, getPaymentResponse.Status);
        Assert.Equal(postPaymentResponse.Amount, getPaymentResponse.Amount);
        Assert.Equal(postPaymentResponse.Currency, getPaymentResponse.Currency);
        Assert.Equal(postPaymentResponse.Id, getPaymentResponse.Id);
    }

    private HttpClient GetCustomizedFactoryClient(bool isAuthorized=true)
    {
        Mock<IBankClient> bankClientMock = GetBankClientMock(isAuthorized);
        return GetWebHostBuilderClient(bankClientMock);
    }

    private HttpClient GetWebHostBuilderClient(Mock<IBankClient> bankClientMock)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBankClient>();
                services.RemoveAll<IPaymentsRepository>();
                services.RemoveAll<IPaymentsService>();

                services.AddSingleton(bankClientMock.Object);

                services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
                services.AddTransient<IPaymentsService, PaymentsService>();
            });
        }).CreateClient();
    }

    private static Mock<IBankClient> GetBankClientMock(bool isAuthorized)
    {
        var mockBankResponse = new BankResponse { IsAuthorized = isAuthorized };

        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(b => b.PostPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync(mockBankResponse);
        return mockBankClient;
    }
    private static Mock<IBankClient> GetBankClientServiceUnavailableMock()
    {
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(b => b.PostPaymentAsync(It.IsAny<BankRequest>()))
            .ThrowsAsync(new ServiceUnavailableException());
        return mockBankClient;
    }
}

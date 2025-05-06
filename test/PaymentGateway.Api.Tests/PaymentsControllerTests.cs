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
        Assert.Equal("Card expiry date must be in the future.", errorMessage);
    }

    [Fact]
    public async Task Get_NonexistentId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IPaymentsService>();
        serviceMock
            .Setup(s => s.GetPaymentAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PaymentResponse?)null);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentsService>();
                services.AddSingleton(serviceMock.Object);
            });
        }).CreateClient();

        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(true, "411111111111111", PaymentStatus.Authorized)]
    [InlineData(false, "411111111111112", PaymentStatus.Declined)]
    public async Task Post_ValidRequest_WithBankResponse_ReturnsOk(bool isAuthorized, string cardNumber, PaymentStatus expectedPaymentStatus)
    {
        HttpClient client = GetCustomizedFactoryInstance(isAuthorized);

        var request = new PaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 250,
            Cvv = 321
        };

        // Act
        var postResponse = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
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
    public async Task Get_ExistingId_ReturnsOk()
    {
        HttpClient client = GetCustomizedFactoryInstance();

        var request = new PaymentRequest
        {
            CardNumber = "411111111111111",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 250,
            Cvv = 321
        };

        // Act
        var postResponse = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
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

    private HttpClient GetCustomizedFactoryInstance(bool isAuthorized=true)
    {
        var fakeBankResponse = new BankResponse { IsAuthorized = isAuthorized };

        var bankClientMock = new Mock<IBankClient>();
        bankClientMock
            .Setup(b => b.PostPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync(fakeBankResponse);

        var client = _factory.WithWebHostBuilder(builder =>
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
        return client;
    }
}

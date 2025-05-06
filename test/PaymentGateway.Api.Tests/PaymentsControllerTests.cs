using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

using Moq;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Services;

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

        // Optionally inspect model errors
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("CardNumber", problem!.Errors.Keys);
        Assert.Contains("ExpiryMonth", problem.Errors.Keys);
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
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal("Card expiry date must be in the future.", text.Trim('"'));
    }

    [Fact]
    public async Task Post_ValidRequest_InvokesServiceAndReturnsOk()
    {
        // Arrange: stub IPaymentsService
        var fakeResponse = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "1111",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 250
        };

        var serviceMock = new Mock<IPaymentsService>();
        serviceMock
            .Setup(s => s.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(fakeResponse);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // replace the real service with our mock
                services.RemoveAll<IPaymentsService>();
                services.AddSingleton(serviceMock.Object);
            });
        }).CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 250,
            Cvv = 321
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal(fakeResponse, payload);

        // verify controller called through to service
        serviceMock.Verify(s => s.ProcessPaymentAsync(request), Times.Once);
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

    [Fact]
    public async Task Get_ExistingId_ReturnsOkAndResponse()
    {
        var expected = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Declined,
            CardNumberLastFour = "2222",
            ExpiryMonth = 11,
            ExpiryYear = 2029,
            Currency = "EUR",
            Amount = 500
        };

        var serviceMock = new Mock<IPaymentsService>();
        serviceMock
            .Setup(s => s.GetPaymentAsync(expected.Id))
            .ReturnsAsync(expected);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentsService>();
                services.AddSingleton(serviceMock.Object);
            });
        }).CreateClient();

        var response = await client.GetAsync($"/api/Payments/{expected.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal(expected, payload);
    }
}

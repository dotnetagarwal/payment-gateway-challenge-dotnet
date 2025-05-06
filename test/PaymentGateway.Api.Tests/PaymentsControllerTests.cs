//using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.VisualStudio.TestPlatform.TestHost;
//using PaymentGateway.Api.Models.Responses;
//using PaymentGateway.Api.Models;
//using System.Net;

//public class PaymentsControllerTests :
//        IClassFixture<WebApplicationFactory<Program>>
//{
//    private readonly WebApplicationFactory<Program> _factory;

//    public PaymentsControllerTests(WebApplicationFactory<Program> factory)
//    {
//        _factory = factory;
//    }

//    [Fact]
//    public async Task GetPayment_ReturnsOk_WhenExists()
//    {
//        // Arrange
//        var repo = new InMemoryPaymentRepository();
//        var existing = new PaymentResponse
//        {
//            Id = Guid.NewGuid(),
//            Status = PaymentStatus.Authorized,
//            CardNumberLastFour = "1234",
//            ExpiryMonth = 12,
//            ExpiryYear = 2030,
//            Currency = "GBP",
//            Amount = 1500
//        };
//        repo.SavePaymentAsync(existing).GetAwaiter().GetResult();

//        var client = _factory
//            .WithWebHostBuilder(builder =>
//                builder.ConfigureServices(services =>
//                {
//                    services.RemoveAll<IPaymentRepository>();
//                    services.AddSingleton<IPaymentRepository>(repo);
//                }))
//            .CreateClient();

//        // Act
//        var response = await client.GetAsync($"/payments/{existing.Id}");
//        var payload = await response.Content.ReadFromJsonAsync<PaymentResponse>();

//        // Assert
//        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//        Assert.NotNull(payload);
//        Assert.Equal(existing.Id, payload!.Id);
//        Assert.Equal("1234", payload.CardNumberLastFour);
//    }
//}
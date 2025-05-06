namespace PaymentGateway.Api.Services;

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

public interface IBankClient
{
    /// <summary>
    /// Sends the payment request to the bank simulator and returns the parsed response.
    /// Throws ServiceUnavailableException if the simulator returns 503.
    /// Returns null if network error or empty body.
    /// </summary>
    Task<BankResponse?> PostPaymentAsync(BankRequest request);
}

public class BankClient : IBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient,
                      ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BankResponse?> PostPaymentAsync(BankRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, Api.Constants.ContentType);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(Constants.PaymentsRequestUri, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error posting to bank");
            return null;
        }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError("Bank simulator returned 503 Service Unavailable");
            throw new ServiceUnavailableException();
        }

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return null;

        return JsonSerializer.Deserialize<BankResponse>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
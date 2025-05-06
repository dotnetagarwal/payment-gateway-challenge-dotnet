namespace PaymentGateway.Api.Models.Requests;
using System.Text.Json.Serialization;

public class BankRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; }

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("cvv")]
    public int Cvv { get; set; }

    public BankRequest(PaymentRequest request)
    {
        CardNumber = request.CardNumber;
        ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}";
        Currency = request.Currency;
        Amount = request.Amount;
        Cvv = request.Cvv;
    }
}

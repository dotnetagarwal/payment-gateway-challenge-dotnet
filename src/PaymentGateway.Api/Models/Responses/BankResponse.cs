
using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Responses;

public class BankResponse
{
    [JsonPropertyName("authorized")]
    public bool IsAuthorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; } = string.Empty;

}

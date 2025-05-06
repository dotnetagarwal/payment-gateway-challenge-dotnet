using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Validations;
using PaymentGateway.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentsService _paymentService;

    public PaymentsController(IPaymentsService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        if (!ModelState.IsValid)
        { 
          return BadRequest(ModelState);
        }

        if (!ExpiryDateValidator.IsExpiryDateValid(request.ExpiryMonth, request.ExpiryYear))
        {
            return BadRequest(ErrorMessages.InvalidCardExpiryDate);
        }

        var response = await _paymentService.ProcessPaymentAsync(request);
        return Ok(response);
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentService.GetPaymentAsync(id);
        if (payment == null)
        {
            return NotFound();
        }
           
        return new OkObjectResult(payment);
    }
}
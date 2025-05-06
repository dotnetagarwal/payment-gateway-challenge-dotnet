namespace PaymentGateway.Api.Validations
{
    public class ExpiryDateValidator
    {
        public static bool IsExpiryDateValid(int expiryMonth, int expiryYear)
        {
            try
            {
                // Last day of the expiry month
                var expiryDate = new DateTime(expiryYear, expiryMonth, 1).AddMonths(1).AddDays(-1);

                return expiryDate >= DateTime.UtcNow.Date;
            }
            catch
            {
                return false; // Invalid date (e.g., bad month/year)
            }
        }
    }
}
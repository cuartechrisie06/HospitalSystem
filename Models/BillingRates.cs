namespace HospitalSystem.Models
{
    public static class BillingRates
    {
        public const decimal ConsultationFee = 500m;

        // Standard VAT rate (%) applied to new bills; each bill can override it under Adjustments.
        public const decimal DefaultVatRate = 12m;
    }
}

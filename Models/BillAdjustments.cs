namespace HospitalSystem.Models
{
    // Everything applied on top of a bill's itemized charges: discounts, tax and HMO coverage.
    public class BillAdjustments
    {
        public decimal DiscountValue { get; set; }          // percent or peso amount, see DiscountIsPercent
        public bool DiscountIsPercent { get; set; } = true;
        public string DiscountReason { get; set; }

        public DiscountEligibility Eligibility { get; set; } = DiscountEligibility.None;
        public string EligibilityIdNo { get; set; }         // OSCA / PWD ID, required to claim the discount

        public decimal VatRate { get; set; }                // percent, e.g. 12 for 12%

        public string HmoProvider { get; set; }
        public string HmoLoaNo { get; set; }                // HMO letter of authorization / approval no.
        public decimal HmoCoverage { get; set; }            // amount the HMO approved to shoulder

        public bool IsVatExempt => Eligibility != DiscountEligibility.None;

        public string EligibilityLabel =>
            Eligibility == DiscountEligibility.SeniorCitizen ? "Senior citizen"
            : Eligibility == DiscountEligibility.PWD ? "PWD"
            : "None";

        public BillAdjustments Clone()
        {
            return (BillAdjustments)MemberwiseClone();
        }
    }
}

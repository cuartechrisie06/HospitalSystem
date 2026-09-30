namespace HospitalSystem.Models
{
    // Statutory discount a patient qualifies for (RA 9994 senior citizens, RA 10754 PWD):
    // 20% off and VAT-exempt.
    public enum DiscountEligibility
    {
        None,
        SeniorCitizen,
        PWD
    }
}

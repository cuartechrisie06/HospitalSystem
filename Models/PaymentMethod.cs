namespace HospitalSystem.Models
{
    public enum PaymentMethod
    {
        Cash,
        Card,
        HMO,            // settlement from the HMO against its approved coverage (was "Insurance")
        EWallet,
        BankTransfer
    }
}

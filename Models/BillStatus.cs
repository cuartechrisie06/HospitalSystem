namespace HospitalSystem.Models
{
    public enum BillStatus
    {
        Unpaid,
        PartiallyPaid,
        Paid,
        Cancelled   // bills can no longer be cancelled; kept so bills cancelled earlier still load
    }
}

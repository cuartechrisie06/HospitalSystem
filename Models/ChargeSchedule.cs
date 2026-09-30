namespace HospitalSystem.Models
{
    // A pre-set service/charge that is billed automatically when a patient is admitted.
    public class ChargeSchedule
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public BillCategory Category { get; set; }
        public decimal UnitPrice { get; set; }
        public string Ward { get; set; }      // null = applies to every ward
        public bool PerDay { get; set; }      // true = billed per day of stay, false = one-time
        public bool IsActive { get; set; } = true;

        public string WardLabel => string.IsNullOrEmpty(Ward) ? "All wards" : Ward;

        public string Basis => PerDay ? "Per day" : "One-time";

        public bool AppliesTo(string ward)
        {
            return IsActive && (string.IsNullOrEmpty(Ward) || Ward == ward);
        }

        public BillItem ToBillItem(int daysStayed)
        {
            return new BillItem
            {
                Description = Description,
                Category = Category,
                Quantity = PerDay ? daysStayed : 1,
                UnitPrice = UnitPrice,
                PerDay = PerDay
            };
        }

        public override string ToString()
        {
            return Description + " (" + WardLabel + ", " + Basis + ")";
        }
    }
}

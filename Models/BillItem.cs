using System;

namespace HospitalSystem.Models
{
    public class BillItem
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public string Description { get; set; }
        public BillCategory Category { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }

        public decimal CalculateAmount()
        {
            Amount = Quantity * UnitPrice;
            return Amount;
        }

        public void UpdateQuantity(int qty)
        {
            if (qty < 1)
                throw new InvalidOperationException("Quantity must be at least 1.");

            Quantity = qty;
            CalculateAmount();
        }

        public void UpdateUnitPrice(decimal price)
        {
            if (price < 0)
                throw new InvalidOperationException("Unit price cannot be negative.");

            UnitPrice = price;
            CalculateAmount();
        }

        public override string ToString()
        {
            return Description + " x" + Quantity + " = " + Amount.ToString("N2");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalSystem.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? AdmissionId { get; set; }
        public int? AppointmentId { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public BillStatus Status { get; set; } = BillStatus.Unpaid;
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<BillItem> Items { get; } = new List<BillItem>();
        public List<Payment> Payments { get; } = new List<Payment>();

        public string BillNo
        {
            get { return "B-" + Id.ToString("D4"); }
        }

        // The methods below only change the object; HospitalData persists the change.

        public void AddItem(BillItem item)
        {
            if (Status == BillStatus.Cancelled)
                throw new InvalidOperationException("Cannot add items to a cancelled bill.");

            item.BillId = Id;
            item.CalculateAmount();
            Items.Add(item);
            CalculateTotal();
            CalculateBalance();
            UpdateStatus();
        }

        public void RemoveItem(int itemId)
        {
            if (Status == BillStatus.Cancelled)
                throw new InvalidOperationException("Cannot remove items from a cancelled bill.");

            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new InvalidOperationException("Item not found on this bill.");
            if (Items.Where(i => i != item).Sum(i => i.Amount) < AmountPaid)
                throw new InvalidOperationException("Removing this item would make the total less than what has already been paid.");

            Items.Remove(item);
            CalculateTotal();
            CalculateBalance();
            UpdateStatus();
        }

        public decimal CalculateTotal()
        {
            TotalAmount = Items.Sum(i => i.Amount);
            return TotalAmount;
        }

        public decimal CalculateBalance()
        {
            Balance = TotalAmount - AmountPaid;
            return Balance;
        }

        public void ApplyPayment(Payment payment)
        {
            if (Status == BillStatus.Cancelled)
                throw new InvalidOperationException("Cannot pay a cancelled bill.");
            if (!payment.IsValid())
                throw new InvalidOperationException("Payment amount must be greater than zero.");
            if (payment.Amount > Balance)
                throw new InvalidOperationException("Payment of " + payment.Amount.ToString("N2") +
                    " is more than the remaining balance of " + Balance.ToString("N2") + ".");

            payment.BillId = Id;
            Payments.Add(payment);
            AmountPaid += payment.Amount;
            CalculateBalance();
            UpdateStatus();
        }

        public void UpdateStatus()
        {
            if (Status == BillStatus.Cancelled) return;

            if (IsFullyPaid())
                Status = BillStatus.Paid;
            else if (AmountPaid > 0)
                Status = BillStatus.PartiallyPaid;
            else
                Status = BillStatus.Unpaid;
        }

        public void MarkAsPaid()
        {
            if (!IsFullyPaid())
                throw new InvalidOperationException("The bill still has a balance of " + Balance.ToString("N2") + ".");

            Status = BillStatus.Paid;
        }

        public void MarkAsCancelled()
        {
            if (Status == BillStatus.Cancelled)
                throw new InvalidOperationException("This bill is already cancelled.");
            if (AmountPaid > 0)
                throw new InvalidOperationException("A bill with recorded payments cannot be cancelled.");

            Status = BillStatus.Cancelled;
        }

        // A bill with no items yet is not considered paid.
        public bool IsFullyPaid()
        {
            return TotalAmount > 0 && Balance <= 0;
        }

        public override string ToString()
        {
            return BillNo + " - " + TotalAmount.ToString("N2") + " (" + Status + ")";
        }
    }
}

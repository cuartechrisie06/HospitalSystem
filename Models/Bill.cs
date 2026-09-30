using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalSystem.Models
{
    public class Bill
    {
        // RA 9994 (senior citizens) / RA 10754 (PWD): 20% discount, VAT-exempt.
        public const decimal StatutoryDiscountRate = 20m;

        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? AdmissionId { get; set; }
        public int? AppointmentId { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }     // amount due from the patient, after all adjustments
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public BillStatus Status { get; set; } = BillStatus.Unpaid;
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public BillAdjustments Adjustments { get; set; } = new BillAdjustments();

        // Breakdown, recalculated by CalculateTotal() from the items and Adjustments.
        public decimal Subtotal { get; private set; }                 // gross itemized charges
        public decimal DiscountAmount { get; private set; }
        public decimal StatutoryDiscountAmount { get; private set; }  // senior citizen / PWD
        public decimal VatAmount { get; private set; }
        public decimal NetAmount { get; private set; }                // after discounts and VAT, before HMO
        public decimal HmoAmount { get; private set; }                // HMO coverage actually applied
        public bool DiscountWaived { get; private set; }              // general discount set aside for the statutory one
        public bool StatutoryDiscountWaived { get; private set; }     // statutory discount set aside for a bigger general one

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
            if (Compute(Items.Where(i => i != item).Sum(i => i.Amount), Adjustments).Due < AmountPaid)
                throw new InvalidOperationException("Removing this item would make the amount due less than what has already been paid.");

            Items.Remove(item);
            CalculateTotal();
            CalculateBalance();
            UpdateStatus();
        }

        // Replaces the bill's discounts, tax and HMO coverage. Nothing changes if validation fails.
        public void ApplyAdjustments(BillAdjustments adj)
        {
            if (Status == BillStatus.Cancelled)
                throw new InvalidOperationException("Cannot adjust a cancelled bill.");
            if (adj.DiscountValue < 0)
                throw new InvalidOperationException("Discount cannot be negative.");
            if (adj.DiscountIsPercent && adj.DiscountValue > 100)
                throw new InvalidOperationException("Discount cannot be more than 100%.");
            if (adj.DiscountValue > 0 && string.IsNullOrWhiteSpace(adj.DiscountReason))
                throw new InvalidOperationException("Please enter the reason for the discount.");
            if (adj.Eligibility != DiscountEligibility.None && string.IsNullOrWhiteSpace(adj.EligibilityIdNo))
                throw new InvalidOperationException("Please enter the " + adj.EligibilityLabel.ToLower() + " ID number to apply the discount.");
            if (adj.VatRate < 0 || adj.VatRate > 100)
                throw new InvalidOperationException("VAT rate must be between 0 and 100%.");
            if (adj.HmoCoverage < 0)
                throw new InvalidOperationException("HMO coverage cannot be negative.");
            if (adj.HmoCoverage > 0 && string.IsNullOrWhiteSpace(adj.HmoProvider))
                throw new InvalidOperationException("Please enter the HMO provider.");

            var totals = Compute(Items.Sum(i => i.Amount), adj);
            if (adj.HmoCoverage > totals.Net)
                throw new InvalidOperationException("HMO coverage of " + adj.HmoCoverage.ToString("N2") +
                    " is more than the net amount of " + totals.Net.ToString("N2") + ".");
            if (totals.Due < AmountPaid)
                throw new InvalidOperationException("These adjustments would bring the amount due to " + totals.Due.ToString("N2") +
                    ", below the " + AmountPaid.ToString("N2") + " already paid.");

            Adjustments = adj.Clone();
            CalculateTotal();
            CalculateBalance();
            UpdateStatus();
        }

        // Brings per-day charges (room, nursing...) in line with the length of stay.
        // Bills created before items carried a per-day flag fall back to their room charge.
        // Returns the items whose quantity changed.
        public List<BillItem> SyncPerDayItems(int daysStayed)
        {
            var changed = new List<BillItem>();
            if (Status == BillStatus.Cancelled) return changed;

            var perDay = Items.Where(i => i.PerDay).ToList();
            if (perDay.Count == 0)
                perDay = Items.Where(i => i.Category == BillCategory.Room).Take(1).ToList();

            foreach (var item in perDay.Where(i => i.Quantity != daysStayed))
            {
                item.UpdateQuantity(daysStayed);
                changed.Add(item);
            }

            if (changed.Count > 0)
            {
                CalculateTotal();
                CalculateBalance();
                UpdateStatus();
            }
            return changed;
        }

        private struct Totals
        {
            public decimal Subtotal, Discount, Statutory, Vat, Net, Hmo, Due;
            public bool DiscountWaived, StatutoryWaived;
        }

        // Subtotal - discount (general OR senior/PWD, whichever is higher) + VAT (unless exempt) = net;
        // net - HMO coverage = amount due from the patient. Item prices are VAT-exclusive.
        private static Totals Compute(decimal subtotal, BillAdjustments adj)
        {
            var t = new Totals { Subtotal = subtotal };

            t.Discount = adj.DiscountIsPercent
                ? Round(subtotal * adj.DiscountValue / 100m)
                : Math.Min(adj.DiscountValue, subtotal);
            t.Statutory = adj.Eligibility != DiscountEligibility.None
                ? Round(subtotal * StatutoryDiscountRate / 100m)
                : 0m;

            // The statutory discount can't be combined with other discounts; the higher one applies.
            if (t.Discount > 0 && t.Statutory > 0)
            {
                if (t.Statutory >= t.Discount) { t.Discount = 0; t.DiscountWaived = true; }
                else { t.Statutory = 0; t.StatutoryWaived = true; }
            }

            decimal taxable = subtotal - t.Discount - t.Statutory;
            t.Vat = adj.IsVatExempt ? 0m : Round(taxable * adj.VatRate / 100m);
            t.Net = taxable + t.Vat;
            // Coverage is validated when set, but items removed later could leave it above the net.
            t.Hmo = Math.Min(adj.HmoCoverage, t.Net);
            t.Due = t.Net - t.Hmo;
            return t;
        }

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public decimal CalculateTotal()
        {
            var t = Compute(Items.Sum(i => i.Amount), Adjustments);
            Subtotal = t.Subtotal;
            DiscountAmount = t.Discount;
            StatutoryDiscountAmount = t.Statutory;
            VatAmount = t.Vat;
            NetAmount = t.Net;
            HmoAmount = t.Hmo;
            DiscountWaived = t.DiscountWaived;
            StatutoryDiscountWaived = t.StatutoryWaived;
            TotalAmount = t.Due;
            return TotalAmount;
        }

        public decimal CalculateBalance()
        {
            Balance = TotalAmount - AmountPaid;
            return Balance;
        }

        // Category subtotals followed by every adjustment, ready to display or print.
        // Deductions are negative.
        public List<BreakdownLine> GetBreakdown()
        {
            var lines = new List<BreakdownLine>();
            var adj = Adjustments;

            foreach (var group in Items.GroupBy(i => i.Category).OrderBy(g => g.Key))
                lines.Add(new BreakdownLine(CategoryLabel(group.Key) + " (" + group.Count() + " item" + (group.Count() == 1 ? "" : "s") + ")",
                    group.Sum(i => i.Amount)));

            lines.Add(new BreakdownLine("Gross charges", Subtotal, BreakdownLineKind.Subtotal));

            if (adj.DiscountValue > 0)
            {
                string label = "Less: Discount" + (adj.DiscountIsPercent ? " " + adj.DiscountValue.ToString("0.##") + "%" : "")
                    + " - " + adj.DiscountReason;
                if (DiscountWaived)
                    label += " (not applied: " + adj.EligibilityLabel.ToLower() + " discount is higher)";
                lines.Add(new BreakdownLine(label, -DiscountAmount));
            }

            if (adj.Eligibility != DiscountEligibility.None)
            {
                string label = "Less: " + adj.EligibilityLabel + " discount " + StatutoryDiscountRate.ToString("0") + "% (ID " + adj.EligibilityIdNo + ")";
                if (StatutoryDiscountWaived)
                    label += " (not applied: other discount is higher)";
                lines.Add(new BreakdownLine(label, -StatutoryDiscountAmount));
            }

            if (adj.IsVatExempt)
                lines.Add(new BreakdownLine("VAT: exempt (" + adj.EligibilityLabel.ToLower() + ")", 0m));
            else if (adj.VatRate > 0)
                lines.Add(new BreakdownLine("Add: VAT " + adj.VatRate.ToString("0.##") + "%", VatAmount));

            lines.Add(new BreakdownLine("Net amount", NetAmount, BreakdownLineKind.Subtotal));

            if (adj.HmoCoverage > 0)
            {
                string label = "Less: HMO coverage - " + adj.HmoProvider +
                    (string.IsNullOrWhiteSpace(adj.HmoLoaNo) ? "" : " (LOA " + adj.HmoLoaNo + ")");
                if (HmoAmount < adj.HmoCoverage)
                    label += " (capped from " + adj.HmoCoverage.ToString("N2") + ")";
                lines.Add(new BreakdownLine(label, -HmoAmount));
            }

            lines.Add(new BreakdownLine("Amount due from patient", TotalAmount, BreakdownLineKind.Total));
            lines.Add(new BreakdownLine("Less: Payments received", -AmountPaid));
            lines.Add(new BreakdownLine("Balance", Balance, BreakdownLineKind.Total));
            return lines;
        }

        public static string CategoryLabel(BillCategory category)
        {
            switch (category)
            {
                case BillCategory.Room: return "Room & board";
                case BillCategory.Consultation: return "Professional / consultation fees";
                case BillCategory.Procedure: return "Procedures";
                case BillCategory.Medicine: return "Medicines";
                case BillCategory.Laboratory: return "Laboratory & diagnostics";
                default: return "Other charges";
            }
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

        // A bill with no items yet is not considered paid. One fully covered by
        // discounts/HMO (nothing due from the patient) is.
        public bool IsFullyPaid()
        {
            return Subtotal > 0 && Balance <= 0;
        }

        public override string ToString()
        {
            return BillNo + " - " + TotalAmount.ToString("N2") + " (" + Status + ")";
        }
    }

    public enum BreakdownLineKind
    {
        Line,
        Subtotal,
        Total
    }

    public class BreakdownLine
    {
        public string Label { get; }
        public decimal Amount { get; }
        public BreakdownLineKind Kind { get; }

        public BreakdownLine(string label, decimal amount, BreakdownLineKind kind = BreakdownLineKind.Line)
        {
            Label = label;
            Amount = amount;
            Kind = kind;
        }
    }
}

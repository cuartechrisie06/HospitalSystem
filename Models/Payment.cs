using System;
using System.Linq;

namespace HospitalSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReferenceNo { get; set; }   // OR / transaction number
        public string ReceivedBy { get; set; }

        // Cash
        public decimal? AmountTendered { get; set; }

        // Card: only the last 4 digits are ever kept, never the full card number.
        public string CardType { get; set; }
        public string CardLast4 { get; set; }
        public string ApprovalCode { get; set; }

        // HMO
        public string HmoProvider { get; set; }
        public string HmoLoaNo { get; set; }

        public decimal Change => AmountTendered.HasValue ? AmountTendered.Value - Amount : 0m;

        public bool IsHmo => Method == PaymentMethod.HMO;

        public bool IsValid()
        {
            return Amount > 0 && Enum.IsDefined(typeof(PaymentMethod), Method);
        }

        // Method-specific checks; null when the payment is complete.
        public string ValidationError()
        {
            switch (Method)
            {
                case PaymentMethod.Cash:
                    if (AmountTendered.HasValue && AmountTendered.Value < Amount)
                        return "Cash tendered (" + AmountTendered.Value.ToString("N2") + ") is less than the payment amount (" + Amount.ToString("N2") + ").";
                    break;
                case PaymentMethod.Card:
                    if (string.IsNullOrWhiteSpace(CardType))
                        return "Please select the card type.";
                    if (CardLast4 == null || CardLast4.Length != 4 || !CardLast4.All(char.IsDigit))
                        return "Please enter the last 4 digits of the card.";
                    break;
                case PaymentMethod.HMO:
                    if (string.IsNullOrWhiteSpace(HmoProvider))
                        return "Please enter the HMO provider.";
                    break;
            }
            return null;
        }

        public string MethodLabel
        {
            get
            {
                switch (Method)
                {
                    case PaymentMethod.EWallet: return "E-wallet";
                    case PaymentMethod.BankTransfer: return "Bank transfer";
                    default: return Method.ToString();
                }
            }
        }

        // One-line summary of the method-specific details, for grids and statements.
        public string Details
        {
            get
            {
                switch (Method)
                {
                    case PaymentMethod.Cash:
                        return AmountTendered.HasValue
                            ? "Tendered " + AmountTendered.Value.ToString("N2") + ", change " + Change.ToString("N2")
                            : "";
                    case PaymentMethod.Card:
                        return (CardType + " ****" + CardLast4 +
                            (string.IsNullOrWhiteSpace(ApprovalCode) ? "" : ", approval " + ApprovalCode)).Trim();
                    case PaymentMethod.HMO:
                        return HmoProvider + (string.IsNullOrWhiteSpace(HmoLoaNo) ? "" : ", LOA " + HmoLoaNo);
                    default:
                        return "";
                }
            }
        }

        public override string ToString()
        {
            return PaymentDate.ToString("yyyy-MM-dd") + " " + MethodLabel + " " + Amount.ToString("N2");
        }
    }
}

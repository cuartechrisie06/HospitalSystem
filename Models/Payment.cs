using System;

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

        public bool IsValid()
        {
            return Amount > 0 && Enum.IsDefined(typeof(PaymentMethod), Method);
        }

        public override string ToString()
        {
            return PaymentDate.ToString("yyyy-MM-dd") + " " + Method + " " + Amount.ToString("N2");
        }
    }
}

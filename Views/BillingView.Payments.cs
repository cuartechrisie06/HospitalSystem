using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Services;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // Recording payments on the selected bill: method-specific fields, change due and saving.
    public partial class BillingView
    {
        private void PaymentAmount_ValueChanged(object sender, EventArgs e) => UpdateChange();

        // Only digits in the card's last-4 box.
        private void TxtCardLast4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                e.Handled = true;
        }

        private PaymentMethod SelectedMethod =>
            cmbMethod.SelectedItem is PaymentMethod m ? m : PaymentMethod.Cash;

        private void ShowMethodPanel()
        {
            var method = SelectedMethod;
            pnlCash.Visible = method == PaymentMethod.Cash;
            pnlCard.Visible = method == PaymentMethod.Card;
            pnlHmo.Visible = method == PaymentMethod.HMO;
        }

        private void CmbMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowMethodPanel();
            PrefillPayment(GetSelectedBill());
        }

        // Suggest paying off whichever side the method settles: the HMO share or the patient share.
        private void PrefillPayment(Bill bill)
        {
            decimal owed = bill == null ? 0 : SelectedMethod == PaymentMethod.HMO ? bill.HmoBalance : bill.Balance;
            numPayAmount.Value = Math.Min(Math.Max(owed, 0), numPayAmount.Maximum);
            numTendered.Value = numPayAmount.Value;
            txtPayHmoProvider.Text = bill != null ? bill.Adjustments.HmoProvider ?? "" : "";
            txtPayHmoLoa.Text = bill != null ? bill.Adjustments.HmoLoaNo ?? "" : "";
            UpdateChange();
        }

        private void UpdateChange()
        {
            decimal change = numTendered.Value - numPayAmount.Value;
            lblChange.Text = change >= 0 ? "Change: " + Money(change) : "Short by " + Money(-change);
            lblChange.ForeColor = change >= 0 ? Color.Black : Color.FromArgb(220, 38, 38);
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null) return;

            var method = SelectedMethod;
            var payment = new Payment
            {
                Amount = numPayAmount.Value,
                Method = method,
                ReferenceNo = string.IsNullOrWhiteSpace(txtReference.Text) ? null : txtReference.Text.Trim()
            };
            if (method == PaymentMethod.Cash)
                payment.AmountTendered = numTendered.Value;
            else if (method == PaymentMethod.Card)
            {
                payment.CardType = cmbCardType.SelectedItem as string;
                payment.CardLast4 = txtCardLast4.Text.Trim();
                payment.ApprovalCode = txtApprovalCode.Text.Trim();
            }
            else if (method == PaymentMethod.HMO)
            {
                payment.HmoProvider = txtPayHmoProvider.Text.Trim();
                payment.HmoLoaNo = txtPayHmoLoa.Text.Trim();
            }

            try
            {
                billingService.RecordPayment(bill, payment);

                txtReference.Clear();
                txtCardLast4.Clear();
                txtApprovalCode.Clear();

                string msg = payment.MethodLabel + " payment of " + Money(payment.Amount) + " recorded.";
                if (method == PaymentMethod.Cash && payment.Change > 0)
                    msg += "\nChange: " + Money(payment.Change);
                msg += "\n\nPatient balance: " + Money(bill.Balance);
                if (bill.HmoAmount > 0 || bill.HmoPaid > 0)
                    msg += "\nHMO outstanding: " + Money(bill.HmoBalance);
                if (bill.IsFullyPaid())
                    msg += "\n\nThe bill is fully paid.";
                MessageBox.Show(msg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBills(bill.Id);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Record Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

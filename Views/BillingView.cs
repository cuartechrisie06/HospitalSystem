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
    public partial class BillingView : UserControl
    {
        private const string AllStatuses = "All";
        private const string WithBalance = "Outstanding";

        private bool loading;
        private Patient selectedPatient;
        private Button btnSelectPatient;
        private Label lblSelectedPatient;

        private readonly IUserSession session;
        private readonly IPermissionGuard permissionGuard;
        private readonly IActivityLogger activityLog;
        private readonly IPatientService patientService;
        private readonly IBedService bedService;
        private readonly IAppointmentService appointmentService;
        private readonly IAdmissionService admissionService;
        private readonly IChargeScheduleService chargeScheduleService;
        private readonly IBillingService billingService;

        // Used by the Designer to render the layout; the app uses the constructor below.
        public BillingView()
        {
            // Every control and event is set up in InitializeComponent() (Designer format),
            // so the Designer shows the complete screen. Lists that come from enums are
            // filled here instead, since designer code can't enumerate them.
            InitializeComponent();
        }

        public BillingView(IUserSession session, IPermissionGuard permissionGuard, IActivityLogger activityLog,
            IPatientService patientService, IBedService bedService, IAppointmentService appointmentService,
            IAdmissionService admissionService, IChargeScheduleService chargeScheduleService,
            IBillingService billingService) : this()
        {
            this.session = session;
            this.permissionGuard = permissionGuard;
            this.activityLog = activityLog;
            this.patientService = patientService;
            this.bedService = bedService;
            this.appointmentService = appointmentService;
            this.admissionService = admissionService;
            this.chargeScheduleService = chargeScheduleService;
            this.billingService = billingService;

            ConfigurePatientPicker();
            cmbCategory.DataSource = Enum.GetValues(typeof(BillCategory));
            cmbEligibility.DataSource = Enum.GetValues(typeof(DiscountEligibility));
            cmbMethod.DataSource = Enum.GetValues(typeof(PaymentMethod));
            cmbDiscountType.SelectedIndex = 0;
            cmbCardType.SelectedIndex = 0;
            ShowMethodPanel();

            ApplyPermissions();
            LoadPatients();
            cmbFilter.SelectedIndex = 0;   // "All"; loads the bills list
            LoadBills();
        }

        // Nurses view bills, print statements and add charges for what the patient used;
        // creating bills, removing charges, discounts/VAT/HMO, payments and the charge schedule are
        // administrator work. Hidden here, and refused by BillingService if reached anyway.
        private void ApplyPermissions()
        {
            createPanel.Visible = permissionGuard.Can(Permission.CreateBills);
            lblNurseNotice.Visible = !createPanel.Visible;   // says what this role can do here
            btnSchedule.Visible = permissionGuard.Can(Permission.ManageChargeSchedule);
            btnAddItem.Visible = permissionGuard.Can(Permission.AddBillCharges);
            btnRemoveItem.Visible = permissionGuard.Can(Permission.RemoveBillCharges);
            if (!permissionGuard.Can(Permission.AdjustBills))
                tabsSummary.TabPages.Remove(adjustmentsTab);

            bool pays = permissionGuard.Can(Permission.RecordPayments);
            paymentEntry.Visible = pays;
            lblPaymentsByAdmin.Visible = !pays;
        }

        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            using (var dlg = new ChargeScheduleForm(bedService, chargeScheduleService))
                dlg.ShowDialog(this);
        }

        private void CmbFilter_SelectedIndexChanged(object sender, EventArgs e) => LoadBills();

        // Keep the bill form compact. The searchable table opens only when staff choose a patient.
        private void ConfigurePatientPicker()
        {
            cmbPatient.Visible = false;
            createPanel.Height = 170;

            btnSelectPatient = new Button
            {
                Text = "Select Patient...",
                Location = new Point(15, 65),
                Size = new Size(120, 24)
            };
            btnSelectPatient.Click += BtnSelectPatient_Click;

            lblSelectedPatient = new Label
            {
                Text = "No patient selected",
                Location = new Point(15, 90),
                Size = new Size(250, 20),
                ForeColor = Color.FromArgb(107, 114, 128)
            };

            chkAutoCharges.Location = new Point(15, 122);
            lblNotes.Location = new Point(280, 125);
            txtNotes.Location = new Point(325, 122);
            btnCreate.Location = new Point(650, 119);
            createPanel.Controls.Add(btnSelectPatient);
            createPanel.Controls.Add(lblSelectedPatient);
        }

        private static string Money(decimal value) => value.ToString("N2");

        // -------------------- Loading --------------------
        private void LoadPatients()
        {
            selectedPatient = null;
            lblSelectedPatient.Text = "No patient selected";
            LoadPatientLinks(0);
        }

        private void BtnSelectPatient_Click(object sender, EventArgs e)
        {
            using (var picker = new PatientPickerDialog(patientService.Active(), "Select Patient for Billing"))
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                selectedPatient = picker.SelectedPatient;
                lblSelectedPatient.Text = selectedPatient.FullName + " (" + selectedPatient.PatientNo + ")";
                lblSelectedPatient.ForeColor = Color.FromArgb(5, 150, 105);
                LoadPatientLinks(selectedPatient.Id);
            }
        }

        private void LoadPatientLinks(int patientId)
        {

            var admissions = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "(none)") };
            admissions.AddRange(admissionService.All()
                .Where(a => a.PatientId == patientId && a.Status != AppointmentStatus.Cancelled)
                .OrderByDescending(a => a.AdmittedOn)
                .Select(a => new KeyValuePair<int, string>(a.Id, a.ToString())));
            cmbAdmission.DataSource = admissions;
            cmbAdmission.DisplayMember = "Value";
            cmbAdmission.ValueMember = "Key";

            var appointments = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "(none)") };
            appointments.AddRange(appointmentService.All()
                .Where(a => a.PatientId == patientId && a.Status != AppointmentStatus.Cancelled)
                .OrderByDescending(a => a.ScheduledOn)
                .Select(a => new KeyValuePair<int, string>(a.Id, a.ToString())));
            cmbAppointment.DataSource = appointments;
            cmbAppointment.DisplayMember = "Value";
            cmbAppointment.ValueMember = "Key";
        }

        private void LoadBills(int selectBillId = 0)
        {
            if (selectBillId == 0)
            {
                var current = GetSelectedBill();
                if (current != null) selectBillId = current.Id;
            }

            string filter = cmbFilter.SelectedItem as string ?? AllStatuses;

            loading = true;
            gridBills.DataSource = billingService.All()
                .Where(b => filter == AllStatuses
                    || (filter == WithBalance ? b.Status != BillStatus.Cancelled && b.TotalOutstanding > 0
                                              : b.Status.ToString() == filter))
                .OrderByDescending(b => b.BillDate)
                .Select(b => new
                {
                    b.Id,
                    b.BillNo,
                    Patient = patientService.NameOf(b.PatientId),
                    For = b.AdmissionId.HasValue ? "ADM-" + b.AdmissionId.Value.ToString("D4")
                        : b.AppointmentId.HasValue ? "A-" + b.AppointmentId.Value.ToString("D4")
                        : "-",
                    Date = b.BillDate.ToString("yyyy-MM-dd"),
                    Gross = Money(b.Subtotal),
                    Due = Money(b.TotalAmount),
                    Paid = Money(b.AmountPaid),
                    PatientBalance = Money(b.Balance),
                    HmoBalance = Money(b.HmoBalance),
                    Status = b.Status.ToString(),
                    b.Notes
                }).ToList();

            if (gridBills.Columns["Id"] != null)
                gridBills.Columns["Id"].Visible = false;

            foreach (DataGridViewRow row in gridBills.Rows)
            {
                if (Convert.ToInt32(row.Cells["Id"].Value) == selectBillId)
                {
                    gridBills.CurrentCell = row.Cells["BillNo"];
                    break;
                }
            }
            loading = false;

            decimal patientsOwe = billingService.OutstandingBalance(), hmosOwe = billingService.OutstandingHmo();
            // Hospital-wide totals are for administrators; everyone sees each bill's own balance.
            lblBills.Text = !permissionGuard.Can(Permission.ViewFinancials) ? "Bills" :
                "Bills    Outstanding - patients: " + Money(patientsOwe) + "   HMO: " + Money(hmosOwe) +
                            "   Total: " + Money(patientsOwe + hmosOwe);
            LoadDetail();
        }

        private Bill GetSelectedBill()
        {
            if (gridBills.CurrentRow == null) return null;
            return billingService.Get(Convert.ToInt32(gridBills.CurrentRow.Cells["Id"].Value));
        }

        private void GridBills_SelectionChanged(object sender, EventArgs e)
        {
            if (!loading) LoadDetail();
        }

        private void LoadDetail()
        {
            var bill = GetSelectedBill();

            // Itemized charges, grouped by category (room, medicine, laboratory, procedures...).
            gridItems.DataSource = bill == null ? null : bill.Items
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Id)
                .Select(i => new
            {
                i.Id,
                i.Description,
                Category = Bill.CategoryLabel(i.Category),
                Qty = i.Quantity,
                UnitPrice = Money(i.UnitPrice),
                Amount = Money(i.Amount)
            }).ToList();
            if (gridItems.Columns["Id"] != null)
                gridItems.Columns["Id"].Visible = false;

            gridPayments.DataSource = bill == null ? null : bill.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    Date = p.PaymentDate.ToString("yyyy-MM-dd HH:mm"),
                    Amount = Money(p.Amount),
                    Method = p.MethodLabel,
                    p.Details,
                    Reference = p.ReferenceNo,
                    p.ReceivedBy
                }).ToList();

            lblItems.Text = bill == null
                ? "Bill Items (select a bill)"
                : "Bill Items - " + bill.BillNo + "    Gross: " + Money(bill.Subtotal) + "    Due: " + Money(bill.TotalAmount);
            lblPayments.Text = bill == null
                ? "Payments"
                : "Payments - " + bill.BillNo + "    Outstanding: " + Money(bill.TotalOutstanding);
            LoadBalanceSummary(bill);

            // Pre-fill with the remaining balance: the common case is paying it in full.
            PrefillPayment(bill);

            bool editable = bill != null && bill.Status != BillStatus.Cancelled;
            btnAddItem.Enabled = editable;
            btnRemoveItem.Enabled = editable;
            btnPay.Enabled = editable && bill.TotalOutstanding > 0;
            btnApplyAdjustments.Enabled = editable;
            btnPrintStatement.Enabled = bill != null;

            LoadBreakdown(bill);
            LoadAdjustments(bill);
        }

        private void LoadBalanceSummary(Bill bill)
        {
            if (bill == null)
            {
                lblPatientBalance.Text = "Select a bill to see its balance.";
                lblHmoBalance.Text = lblPatientOutstanding.Text = "";
                return;
            }

            lblPatientBalance.Text = "Patient balance: " + Money(bill.Balance) +
                "    (due " + Money(bill.TotalAmount) + ", paid " + Money(bill.TotalAmount - bill.Balance) + ")";
            lblHmoBalance.Text = bill.HmoAmount > 0 || bill.HmoPaid > 0
                ? "HMO outstanding (" + bill.Adjustments.HmoProvider + "): " + Money(bill.HmoBalance) +
                  "    (covered " + Money(bill.HmoAmount) + ", received " + Money(bill.HmoAmount - bill.HmoBalance) + ")"
                : "No HMO coverage on this bill.";

            // The patient may owe on other bills too (e.g. an earlier admission).
            decimal allBills = billingService.PatientOutstanding(bill.PatientId);
            int count = billingService.PatientBillsWithBalance(bill.PatientId);
            lblPatientOutstanding.Text = allBills > bill.Balance
                ? patientService.NameOf(bill.PatientId) + " owes " + Money(allBills) + " across " + count + " bills."
                : "";
        }

        private void LoadBreakdown(Bill bill)
        {
            gridBreakdown.Rows.Clear();
            if (bill == null) return;

            foreach (var line in bill.GetBreakdown())
            {
                int i = gridBreakdown.Rows.Add(line.Label, FormatAmount(line.Amount));
                var row = gridBreakdown.Rows[i];
                if (line.Kind != BreakdownLineKind.Line)
                    row.DefaultCellStyle.Font = breakdownBold;
                if (line.Kind == BreakdownLineKind.Total)
                    row.DefaultCellStyle.BackColor = row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
                else if (line.Kind == BreakdownLineKind.Subtotal)
                    row.DefaultCellStyle.BackColor = row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(249, 250, 251);
            }
            gridBreakdown.ClearSelection();
        }

        // Deductions in parentheses, accounting style.
        private static string FormatAmount(decimal value) =>
            value < 0 ? "(" + Money(-value) + ")" : Money(value);

        private void LoadAdjustments(Bill bill)
        {
            var adj = bill != null ? bill.Adjustments : new BillAdjustments { VatRate = BillingRates.DefaultVatRate };

            numDiscount.Value = Math.Min(adj.DiscountValue, numDiscount.Maximum);
            cmbDiscountType.SelectedIndex = adj.DiscountIsPercent ? 0 : 1;
            txtDiscountReason.Text = adj.DiscountReason ?? "";
            cmbEligibility.SelectedItem = adj.Eligibility;
            txtEligibilityId.Text = adj.EligibilityIdNo ?? "";
            numVatRate.Value = Math.Min(adj.VatRate, numVatRate.Maximum);
            cmbHmoProvider.Text = adj.HmoProvider ?? "";
            txtHmoLoa.Text = adj.HmoLoaNo ?? "";
            numHmoCoverage.Value = Math.Min(adj.HmoCoverage, numHmoCoverage.Maximum);

            // Nudge staff when the patient's age qualifies them but no discount is applied yet.
            var patient = bill != null ? patientService.Get(bill.PatientId) : null;
            lblEligibilityHint.Text = patient != null && patient.Age >= 60 && adj.Eligibility == DiscountEligibility.None
                ? "Patient is " + patient.Age + ": eligible for the senior citizen discount (ask for the OSCA ID)."
                : "";
        }

        // -------------------- Actions --------------------
        private void BtnApplyAdjustments_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null) return;

            var adj = new BillAdjustments
            {
                DiscountValue = numDiscount.Value,
                DiscountIsPercent = cmbDiscountType.SelectedIndex == 0,
                DiscountReason = txtDiscountReason.Text.Trim(),
                Eligibility = (DiscountEligibility)cmbEligibility.SelectedItem,
                EligibilityIdNo = txtEligibilityId.Text.Trim(),
                VatRate = numVatRate.Value,
                HmoProvider = cmbHmoProvider.Text.Trim(),
                HmoLoaNo = txtHmoLoa.Text.Trim(),
                HmoCoverage = numHmoCoverage.Value
            };

            try
            {
                billingService.UpdateAdjustments(bill, adj);
                LoadBills(bill.Id);
                tabsSummary.SelectedIndex = 0;   // show the updated breakdown
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Apply Adjustments", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnPrintStatement_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null) return;

            using (var printer = new BillStatementPrinter(session, activityLog, patientService, bedService, admissionService, bill))
                printer.ShowPreview(FindForm());
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            var patient = selectedPatient;
            if (patient == null)
            {
                MessageBox.Show("Please select a patient.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int admissionId = cmbAdmission.SelectedValue != null ? Convert.ToInt32(cmbAdmission.SelectedValue) : 0;
            int appointmentId = cmbAppointment.SelectedValue != null ? Convert.ToInt32(cmbAppointment.SelectedValue) : 0;

            // One open bill per admission / appointment, so charges are never billed twice.
            var existing = admissionId > 0 ? billingService.OpenForAdmission(admissionId) : null;
            if (existing == null && appointmentId > 0)
                existing = billingService.OpenForAppointment(appointmentId);
            if (existing != null)
            {
                MessageBox.Show("That admission / appointment is already billed on " + existing.BillNo +
                    ". Add items to that bill instead.", "Already Billed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadBills(existing.Id);
                return;
            }

            var admission = admissionId > 0 ? admissionService.All().FirstOrDefault(a => a.Id == admissionId) : null;
            var appointment = appointmentId > 0 ? appointmentService.All().FirstOrDefault(a => a.Id == appointmentId) : null;

            var items = chkAutoCharges.Checked
                ? billingService.DefaultChargesFor(admission, appointment)
                : new List<BillItem>();

            var bill = billingService.Create(new Bill
            {
                PatientId = patient.Id,
                AdmissionId = admission != null ? admission.Id : (int?)null,
                AppointmentId = appointment != null ? appointment.Id : (int?)null,
                Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim()
            }, items);

            string msg = "Bill " + bill.BillNo + " created. Total: " + Money(bill.TotalAmount) + ".";
            if (admission != null && admission.IsActive && chkAutoCharges.Checked)
                msg += "\n\nThe patient is still admitted, so per-day charges cover the " +
                       admission.CalculateDaysStayed() + " day(s) so far and will be updated on discharge.";
            MessageBox.Show(msg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            txtNotes.Clear();
            cmbFilter.SelectedIndex = 0;   // make sure the new bill is visible
            LoadBills(bill.Id);
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null) return;

            if (string.IsNullOrWhiteSpace(txtItemDesc.Text))
            {
                MessageBox.Show("Please enter a description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                billingService.AddItem(bill, new BillItem
                {
                    Description = txtItemDesc.Text.Trim(),
                    Category = (BillCategory)cmbCategory.SelectedItem,
                    Quantity = (int)numQty.Value,
                    UnitPrice = numUnitPrice.Value
                });

                txtItemDesc.Clear();
                numQty.Value = 1;
                numUnitPrice.Value = 0;
                LoadBills(bill.Id);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Add Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null || gridItems.CurrentRow == null) return;
            int itemId = Convert.ToInt32(gridItems.CurrentRow.Cells["Id"].Value);

            if (MessageBox.Show("Remove this item from " + bill.BillNo + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                billingService.RemoveItem(bill, itemId);
                LoadBills(bill.Id);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Remove Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
}

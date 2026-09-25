using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    public class BillingView : UserControl
    {
        private const string AllStatuses = "All";

        private ComboBox cmbPatient, cmbAdmission, cmbAppointment, cmbFilter;
        private CheckBox chkAutoCharges;
        private TextBox txtNotes;
        private Button btnCreate, btnCancelBill;
        private Label lblBills;
        private DataGridView gridBills;

        private Label lblItems;
        private TextBox txtItemDesc;
        private ComboBox cmbCategory;
        private NumericUpDown numQty, numUnitPrice;
        private Button btnAddItem, btnRemoveItem;
        private DataGridView gridItems;

        private Label lblPayments;
        private NumericUpDown numPayAmount;
        private ComboBox cmbMethod;
        private TextBox txtReference;
        private Button btnPay;
        private DataGridView gridPayments;

        private bool loading;

        public BillingView()
        {
            InitializeComponent();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                LoadPatients();
                LoadBills();
            }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Padding = new Padding(10);

            // ===== Top form: create bill =====
            Panel formPanel = new Panel();
            formPanel.Dock = DockStyle.Top;
            formPanel.Height = 145;
            formPanel.BackColor = Color.White;
            formPanel.Padding = new Padding(15);
            formPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(formPanel);

            Label title = new Label();
            title.Text = "Create Bill";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.Location = new Point(15, 10);
            title.AutoSize = true;
            formPanel.Controls.Add(title);

            formPanel.Controls.Add(MakeLabel("Patient", 15, 45));
            cmbPatient = new ComboBox();
            cmbPatient.Location = new Point(15, 65);
            cmbPatient.Size = new Size(250, 28);
            cmbPatient.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPatient.SelectedIndexChanged += CmbPatient_SelectedIndexChanged;
            formPanel.Controls.Add(cmbPatient);

            formPanel.Controls.Add(MakeLabel("Admission (optional)", 280, 45));
            cmbAdmission = new ComboBox();
            cmbAdmission.Location = new Point(280, 65);
            cmbAdmission.Size = new Size(240, 28);
            cmbAdmission.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbAdmission);

            formPanel.Controls.Add(MakeLabel("Appointment (optional)", 535, 45));
            cmbAppointment = new ComboBox();
            cmbAppointment.Location = new Point(535, 65);
            cmbAppointment.Size = new Size(240, 28);
            cmbAppointment.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbAppointment);

            chkAutoCharges = new CheckBox();
            chkAutoCharges.Text = "Add room / consultation charges";
            chkAutoCharges.Checked = true;
            chkAutoCharges.Location = new Point(15, 102);
            chkAutoCharges.AutoSize = true;
            formPanel.Controls.Add(chkAutoCharges);

            formPanel.Controls.Add(MakeLabel("Notes", 280, 105));
            txtNotes = new TextBox();
            txtNotes.Location = new Point(325, 102);
            txtNotes.Size = new Size(310, 28);
            formPanel.Controls.Add(txtNotes);

            btnCreate = new Button();
            btnCreate.Text = "Create Bill";
            btnCreate.Location = new Point(650, 99);
            btnCreate.Size = new Size(125, 30);
            btnCreate.BackColor = Color.FromArgb(37, 99, 235);
            btnCreate.ForeColor = Color.White;
            btnCreate.FlatStyle = FlatStyle.Flat;
            btnCreate.Click += BtnCreate_Click;
            formPanel.Controls.Add(btnCreate);

            // ===== Bills list =====
            lblBills = new Label();
            lblBills.Text = "Bills";
            lblBills.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblBills.Dock = DockStyle.Top;
            lblBills.Height = 30;
            lblBills.TextAlign = ContentAlignment.BottomLeft;
            this.Controls.Add(lblBills);

            Panel btnPanel = new Panel();
            btnPanel.Dock = DockStyle.Top;
            btnPanel.Height = 40;
            this.Controls.Add(btnPanel);

            btnCancelBill = new Button();
            btnCancelBill.Text = "Cancel Selected Bill";
            btnCancelBill.Location = new Point(0, 5);
            btnCancelBill.Size = new Size(150, 30);
            btnCancelBill.Click += BtnCancelBill_Click;
            btnPanel.Controls.Add(btnCancelBill);

            btnPanel.Controls.Add(MakeLabel("Show:", 170, 12));
            cmbFilter = new ComboBox();
            cmbFilter.Location = new Point(212, 8);
            cmbFilter.Size = new Size(130, 28);
            cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilter.Items.Add(AllStatuses);
            foreach (var st in Enum.GetNames(typeof(BillStatus)))
                cmbFilter.Items.Add(st);
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => LoadBills();
            btnPanel.Controls.Add(cmbFilter);

            gridBills = MakeGrid();
            gridBills.Dock = DockStyle.Top;
            gridBills.Height = 190;
            gridBills.SelectionChanged += GridBills_SelectionChanged;
            this.Controls.Add(gridBills);

            // ===== Detail: items (left) + payments (right) =====
            TableLayoutPanel detail = new TableLayoutPanel();
            detail.Dock = DockStyle.Fill;
            detail.ColumnCount = 2;
            detail.RowCount = 1;
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detail.Padding = new Padding(0, 10, 0, 0);
            this.Controls.Add(detail);
            detail.BringToFront();

            detail.Controls.Add(BuildItemsPanel(), 0, 0);
            detail.Controls.Add(BuildPaymentsPanel(), 1, 0);
        }

        private Panel BuildItemsPanel()
        {
            Panel panel = MakeCard(new Padding(0, 0, 5, 0));

            lblItems = MakeCardHeader("Bill Items");
            panel.Controls.Add(lblItems);

            Panel entry = new Panel();
            entry.Dock = DockStyle.Top;
            entry.Height = 90;
            panel.Controls.Add(entry);

            entry.Controls.Add(MakeLabel("Description", 5, 0));
            txtItemDesc = new TextBox();
            txtItemDesc.Location = new Point(5, 18);
            txtItemDesc.Size = new Size(200, 28);
            entry.Controls.Add(txtItemDesc);

            entry.Controls.Add(MakeLabel("Category", 210, 0));
            cmbCategory = new ComboBox();
            cmbCategory.Location = new Point(210, 18);
            cmbCategory.Size = new Size(110, 28);
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.DataSource = Enum.GetValues(typeof(BillCategory));
            entry.Controls.Add(cmbCategory);

            entry.Controls.Add(MakeLabel("Qty", 325, 0));
            numQty = new NumericUpDown();
            numQty.Location = new Point(325, 18);
            numQty.Size = new Size(55, 28);
            numQty.Minimum = 1;
            numQty.Maximum = 1000;
            entry.Controls.Add(numQty);

            entry.Controls.Add(MakeLabel("Unit Price", 385, 0));
            numUnitPrice = new NumericUpDown();
            numUnitPrice.Location = new Point(385, 18);
            numUnitPrice.Size = new Size(100, 28);
            numUnitPrice.DecimalPlaces = 2;
            numUnitPrice.Maximum = 10000000;
            numUnitPrice.ThousandsSeparator = true;
            entry.Controls.Add(numUnitPrice);

            btnAddItem = new Button();
            btnAddItem.Text = "Add Item";
            btnAddItem.Location = new Point(5, 52);
            btnAddItem.Size = new Size(100, 30);
            btnAddItem.Click += BtnAddItem_Click;
            entry.Controls.Add(btnAddItem);

            btnRemoveItem = new Button();
            btnRemoveItem.Text = "Remove Selected";
            btnRemoveItem.Location = new Point(110, 52);
            btnRemoveItem.Size = new Size(130, 30);
            btnRemoveItem.Click += BtnRemoveItem_Click;
            entry.Controls.Add(btnRemoveItem);

            gridItems = MakeGrid();
            gridItems.Dock = DockStyle.Fill;
            panel.Controls.Add(gridItems);
            gridItems.BringToFront();

            return panel;
        }

        private Panel BuildPaymentsPanel()
        {
            Panel panel = MakeCard(new Padding(5, 0, 0, 0));

            lblPayments = MakeCardHeader("Payments");
            panel.Controls.Add(lblPayments);

            Panel entry = new Panel();
            entry.Dock = DockStyle.Top;
            entry.Height = 90;
            panel.Controls.Add(entry);

            entry.Controls.Add(MakeLabel("Amount", 5, 0));
            numPayAmount = new NumericUpDown();
            numPayAmount.Location = new Point(5, 18);
            numPayAmount.Size = new Size(110, 28);
            numPayAmount.DecimalPlaces = 2;
            numPayAmount.Maximum = 10000000;
            numPayAmount.ThousandsSeparator = true;
            entry.Controls.Add(numPayAmount);

            entry.Controls.Add(MakeLabel("Method", 120, 0));
            cmbMethod = new ComboBox();
            cmbMethod.Location = new Point(120, 18);
            cmbMethod.Size = new Size(110, 28);
            cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMethod.DataSource = Enum.GetValues(typeof(PaymentMethod));
            entry.Controls.Add(cmbMethod);

            entry.Controls.Add(MakeLabel("Reference / OR No.", 235, 0));
            txtReference = new TextBox();
            txtReference.Location = new Point(235, 18);
            txtReference.Size = new Size(150, 28);
            entry.Controls.Add(txtReference);

            btnPay = new Button();
            btnPay.Text = "Record Payment";
            btnPay.Location = new Point(5, 52);
            btnPay.Size = new Size(130, 30);
            btnPay.BackColor = Color.FromArgb(5, 150, 105);
            btnPay.ForeColor = Color.White;
            btnPay.FlatStyle = FlatStyle.Flat;
            btnPay.Click += BtnPay_Click;
            entry.Controls.Add(btnPay);

            gridPayments = MakeGrid();
            gridPayments.Dock = DockStyle.Fill;
            panel.Controls.Add(gridPayments);
            gridPayments.BringToFront();

            return panel;
        }

        // -------------------- Small UI helpers --------------------
        private static Label MakeLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            return lbl;
        }

        private static Panel MakeCard(Padding margin)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = margin;
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Padding = new Padding(10);
            return panel;
        }

        private static Label MakeCardHeader(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbl.Dock = DockStyle.Top;
            lbl.Height = 28;
            return lbl;
        }

        private static DataGridView MakeGrid()
        {
            DataGridView grid = new DataGridView();
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            return grid;
        }

        private static string Money(decimal value) => value.ToString("N2");

        // -------------------- Loading --------------------
        private void LoadPatients()
        {
            cmbPatient.DataSource = HospitalData.ActivePatients();
            cmbPatient.DisplayMember = "ToString";
            cmbPatient.ValueMember = "Id";
        }

        private void CmbPatient_SelectedIndexChanged(object sender, EventArgs e)
        {
            var patient = cmbPatient.SelectedItem as Patient;
            int patientId = patient != null ? patient.Id : 0;

            var admissions = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "(none)") };
            admissions.AddRange(HospitalData.Admissions
                .Where(a => a.PatientId == patientId && a.Status != "Cancelled")
                .OrderByDescending(a => a.AdmittedOn)
                .Select(a => new KeyValuePair<int, string>(a.Id, a.ToString())));
            cmbAdmission.DataSource = admissions;
            cmbAdmission.DisplayMember = "Value";
            cmbAdmission.ValueMember = "Key";

            var appointments = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "(none)") };
            appointments.AddRange(HospitalData.Appointments
                .Where(a => a.PatientId == patientId && a.Status != "Cancelled")
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
            gridBills.DataSource = HospitalData.Bills
                .Where(b => filter == AllStatuses || b.Status.ToString() == filter)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new
                {
                    b.Id,
                    b.BillNo,
                    Patient = HospitalData.PatientName(b.PatientId),
                    For = b.AdmissionId.HasValue ? "ADM-" + b.AdmissionId.Value.ToString("D4")
                        : b.AppointmentId.HasValue ? "A-" + b.AppointmentId.Value.ToString("D4")
                        : "-",
                    Date = b.BillDate.ToString("yyyy-MM-dd"),
                    Total = Money(b.TotalAmount),
                    Paid = Money(b.AmountPaid),
                    Balance = Money(b.Balance),
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

            lblBills.Text = "Bills    (Outstanding balance: " + Money(HospitalData.OutstandingBalance()) + ")";
            LoadDetail();
        }

        private Bill GetSelectedBill()
        {
            if (gridBills.CurrentRow == null) return null;
            return HospitalData.GetBill(Convert.ToInt32(gridBills.CurrentRow.Cells["Id"].Value));
        }

        private void GridBills_SelectionChanged(object sender, EventArgs e)
        {
            if (!loading) LoadDetail();
        }

        private void LoadDetail()
        {
            var bill = GetSelectedBill();

            gridItems.DataSource = bill == null ? null : bill.Items.Select(i => new
            {
                i.Id,
                i.Description,
                Category = i.Category.ToString(),
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
                    Method = p.Method.ToString(),
                    Reference = p.ReferenceNo,
                    p.ReceivedBy
                }).ToList();

            lblItems.Text = bill == null
                ? "Bill Items (select a bill)"
                : "Bill Items - " + bill.BillNo + "    Total: " + Money(bill.TotalAmount);
            lblPayments.Text = bill == null
                ? "Payments"
                : "Payments    Paid: " + Money(bill.AmountPaid) + "    Balance: " + Money(bill.Balance);

            // Pre-fill with the remaining balance: the common case is paying it in full.
            numPayAmount.Value = bill == null ? 0 : Math.Min(Math.Max(bill.Balance, 0), numPayAmount.Maximum);

            bool editable = bill != null && bill.Status != BillStatus.Cancelled;
            btnAddItem.Enabled = editable;
            btnRemoveItem.Enabled = editable;
            btnPay.Enabled = editable && bill.Balance > 0;
            btnCancelBill.Enabled = editable;
        }

        // -------------------- Actions --------------------
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            var patient = cmbPatient.SelectedItem as Patient;
            if (patient == null)
            {
                MessageBox.Show("Please select a patient.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int admissionId = cmbAdmission.SelectedValue != null ? Convert.ToInt32(cmbAdmission.SelectedValue) : 0;
            int appointmentId = cmbAppointment.SelectedValue != null ? Convert.ToInt32(cmbAppointment.SelectedValue) : 0;

            // One open bill per admission / appointment, so charges are never billed twice.
            var existing = admissionId > 0 ? HospitalData.OpenBillForAdmission(admissionId) : null;
            if (existing == null && appointmentId > 0)
                existing = HospitalData.OpenBillForAppointment(appointmentId);
            if (existing != null)
            {
                MessageBox.Show("That admission / appointment is already billed on " + existing.BillNo +
                    ". Add items to that bill instead, or cancel it first.", "Already Billed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadBills(existing.Id);
                return;
            }

            var admission = admissionId > 0 ? HospitalData.Admissions.FirstOrDefault(a => a.Id == admissionId) : null;
            var appointment = appointmentId > 0 ? HospitalData.Appointments.FirstOrDefault(a => a.Id == appointmentId) : null;

            var items = chkAutoCharges.Checked
                ? HospitalData.DefaultChargesFor(admission, appointment)
                : new List<BillItem>();

            var bill = HospitalData.CreateBill(new Bill
            {
                PatientId = patient.Id,
                AdmissionId = admission != null ? admission.Id : (int?)null,
                AppointmentId = appointment != null ? appointment.Id : (int?)null,
                Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim()
            }, items);

            string msg = "Bill " + bill.BillNo + " created. Total: " + Money(bill.TotalAmount) + ".";
            if (admission != null && admission.IsActive && chkAutoCharges.Checked)
                msg += "\n\nThe patient is still admitted, so room charges cover the " +
                       admission.CalculateDaysStayed() + " day(s) so far.";
            MessageBox.Show(msg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            txtNotes.Clear();
            cmbFilter.SelectedIndex = 0;   // make sure the new bill is visible
            LoadBills(bill.Id);
        }

        private void BtnCancelBill_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null) return;

            if (MessageBox.Show("Cancel bill " + bill.BillNo + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                HospitalData.CancelBill(bill);
                LoadBills(bill.Id);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Cancel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                HospitalData.AddBillItem(bill, new BillItem
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
                HospitalData.RemoveBillItem(bill, itemId);
                LoadBills(bill.Id);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Remove Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            var bill = GetSelectedBill();
            if (bill == null) return;

            try
            {
                HospitalData.RecordPayment(bill, new Payment
                {
                    Amount = numPayAmount.Value,
                    Method = (PaymentMethod)cmbMethod.SelectedItem,
                    ReferenceNo = string.IsNullOrWhiteSpace(txtReference.Text) ? null : txtReference.Text.Trim()
                });

                txtReference.Clear();
                MessageBox.Show("Payment recorded. Remaining balance: " + Money(bill.Balance) +
                    (bill.IsFullyPaid() ? " (fully paid)." : "."), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBills(bill.Id);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Record Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

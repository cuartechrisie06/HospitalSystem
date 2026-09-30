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
        private const string WithBalance = "Outstanding";

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
        private Label lblPatientBalance, lblHmoBalance, lblPatientOutstanding;
        private Panel pnlCash, pnlCard, pnlHmo;
        private NumericUpDown numTendered;
        private Label lblChange;
        private ComboBox cmbCardType;
        private TextBox txtCardLast4, txtApprovalCode, txtPayHmoProvider, txtPayHmoLoa;

        private TabControl tabsSummary;
        private DataGridView gridBreakdown;
        private Button btnPrintStatement;
        private readonly Font breakdownBold = new Font("Segoe UI", 9F, FontStyle.Bold);

        private NumericUpDown numDiscount, numVatRate, numHmoCoverage;
        private ComboBox cmbDiscountType, cmbEligibility, cmbHmoProvider;
        private TextBox txtDiscountReason, txtEligibilityId, txtHmoLoa;
        private Label lblEligibilityHint;
        private Button btnApplyAdjustments;

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
            chkAutoCharges.Text = "Add scheduled charges";
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
            cmbFilter.Items.Add(WithBalance);
            foreach (var st in Enum.GetNames(typeof(BillStatus)))
                cmbFilter.Items.Add(st);
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => LoadBills();
            btnPanel.Controls.Add(cmbFilter);

            Button btnSchedule = new Button();
            btnSchedule.Text = "Admission Charge Schedule...";
            btnSchedule.Location = new Point(360, 5);
            btnSchedule.Size = new Size(190, 30);
            btnSchedule.Click += (s, e) =>
            {
                using (var dlg = new ChargeScheduleForm())
                    dlg.ShowDialog(this);
            };
            btnPanel.Controls.Add(btnSchedule);

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
            detail.Controls.Add(BuildSummaryTabs(), 1, 0);
        }

        // Right-hand side: breakdown of the selected bill, its adjustments, and payments.
        private Control BuildSummaryTabs()
        {
            tabsSummary = new TabControl();
            tabsSummary.Dock = DockStyle.Fill;
            tabsSummary.Margin = new Padding(5, 0, 0, 0);

            TabPage breakdownPage = new TabPage("Breakdown");
            breakdownPage.Controls.Add(BuildBreakdownPanel());
            tabsSummary.TabPages.Add(breakdownPage);

            TabPage adjustmentsPage = new TabPage("Discounts / Tax / HMO");
            adjustmentsPage.Controls.Add(BuildAdjustmentsPanel());
            tabsSummary.TabPages.Add(adjustmentsPage);

            TabPage paymentsPage = new TabPage("Payments");
            Panel payments = BuildPaymentsPanel();
            payments.Margin = new Padding(0);
            paymentsPage.Controls.Add(payments);
            tabsSummary.TabPages.Add(paymentsPage);

            return tabsSummary;
        }

        private Panel BuildBreakdownPanel()
        {
            Panel panel = MakeCard(new Padding(0));
            panel.Dock = DockStyle.Fill;

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 40;
            panel.Controls.Add(bottom);

            btnPrintStatement = new Button();
            btnPrintStatement.Text = "Print Statement...";
            btnPrintStatement.Location = new Point(0, 6);
            btnPrintStatement.Size = new Size(140, 30);
            btnPrintStatement.Click += BtnPrintStatement_Click;
            bottom.Controls.Add(btnPrintStatement);

            gridBreakdown = MakeGrid();
            gridBreakdown.Dock = DockStyle.Fill;
            gridBreakdown.ColumnHeadersVisible = false;
            gridBreakdown.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridBreakdown.CellBorderStyle = DataGridViewCellBorderStyle.None;
            gridBreakdown.DefaultCellStyle.SelectionBackColor = Color.White;
            gridBreakdown.DefaultCellStyle.SelectionForeColor = Color.Black;
            var colLabel = new DataGridViewTextBoxColumn { Name = "Label", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            colLabel.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            var colAmount = new DataGridViewTextBoxColumn { Name = "Amount", Width = 110 };
            colAmount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridBreakdown.Columns.Add(colLabel);
            gridBreakdown.Columns.Add(colAmount);
            gridBreakdown.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            panel.Controls.Add(gridBreakdown);
            gridBreakdown.BringToFront();

            return panel;
        }

        private Panel BuildAdjustmentsPanel()
        {
            Panel panel = MakeCard(new Padding(0));
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;

            // Discount
            panel.Controls.Add(MakeLabel("Discount", 5, 5));
            numDiscount = new NumericUpDown();
            numDiscount.Location = new Point(5, 23);
            numDiscount.Size = new Size(85, 28);
            numDiscount.DecimalPlaces = 2;
            numDiscount.Maximum = 10000000;
            numDiscount.ThousandsSeparator = true;
            panel.Controls.Add(numDiscount);

            cmbDiscountType = new ComboBox();
            cmbDiscountType.Location = new Point(95, 23);
            cmbDiscountType.Size = new Size(55, 28);
            cmbDiscountType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDiscountType.Items.AddRange(new object[] { "%", "PHP" });
            cmbDiscountType.SelectedIndex = 0;
            panel.Controls.Add(cmbDiscountType);

            panel.Controls.Add(MakeLabel("Reason (e.g. employee, promo, charity)", 160, 5));
            txtDiscountReason = new TextBox();
            txtDiscountReason.Location = new Point(160, 23);
            txtDiscountReason.Size = new Size(230, 28);
            panel.Controls.Add(txtDiscountReason);

            // Senior citizen / PWD
            panel.Controls.Add(MakeLabel("Senior citizen / PWD (20%, VAT-exempt)", 5, 58));
            cmbEligibility = new ComboBox();
            cmbEligibility.Location = new Point(5, 76);
            cmbEligibility.Size = new Size(145, 28);
            cmbEligibility.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEligibility.DataSource = Enum.GetValues(typeof(DiscountEligibility));
            panel.Controls.Add(cmbEligibility);

            panel.Controls.Add(MakeLabel("OSCA / PWD ID No.", 160, 58));
            txtEligibilityId = new TextBox();
            txtEligibilityId.Location = new Point(160, 76);
            txtEligibilityId.Size = new Size(230, 28);
            panel.Controls.Add(txtEligibilityId);

            lblEligibilityHint = MakeLabel("", 5, 104);
            lblEligibilityHint.ForeColor = Color.FromArgb(180, 83, 9);
            panel.Controls.Add(lblEligibilityHint);

            // VAT
            panel.Controls.Add(MakeLabel("VAT rate (%)", 5, 126));
            numVatRate = new NumericUpDown();
            numVatRate.Location = new Point(5, 144);
            numVatRate.Size = new Size(85, 28);
            numVatRate.DecimalPlaces = 2;
            numVatRate.Maximum = 100;
            panel.Controls.Add(numVatRate);
            panel.Controls.Add(MakeLabel("Waived automatically for senior citizens / PWD.", 95, 147));

            // HMO
            panel.Controls.Add(MakeLabel("HMO provider", 5, 179));
            cmbHmoProvider = new ComboBox();
            cmbHmoProvider.Location = new Point(5, 197);
            cmbHmoProvider.Size = new Size(145, 28);
            cmbHmoProvider.DropDownStyle = ComboBoxStyle.DropDown;   // pick a common one or type another
            cmbHmoProvider.Items.AddRange(new object[] { "Maxicare", "Intellicare", "MediCard", "PhilCare", "Cocolife", "ValuCare", "Etiqa" });
            panel.Controls.Add(cmbHmoProvider);

            panel.Controls.Add(MakeLabel("LOA / Approval No.", 160, 179));
            txtHmoLoa = new TextBox();
            txtHmoLoa.Location = new Point(160, 197);
            txtHmoLoa.Size = new Size(120, 28);
            panel.Controls.Add(txtHmoLoa);

            panel.Controls.Add(MakeLabel("Coverage amount", 290, 179));
            numHmoCoverage = new NumericUpDown();
            numHmoCoverage.Location = new Point(290, 197);
            numHmoCoverage.Size = new Size(100, 28);
            numHmoCoverage.DecimalPlaces = 2;
            numHmoCoverage.Maximum = 10000000;
            numHmoCoverage.ThousandsSeparator = true;
            panel.Controls.Add(numHmoCoverage);

            btnApplyAdjustments = new Button();
            btnApplyAdjustments.Text = "Apply Adjustments";
            btnApplyAdjustments.Location = new Point(5, 240);
            btnApplyAdjustments.Size = new Size(150, 30);
            btnApplyAdjustments.BackColor = Color.FromArgb(37, 99, 235);
            btnApplyAdjustments.ForeColor = Color.White;
            btnApplyAdjustments.FlatStyle = FlatStyle.Flat;
            btnApplyAdjustments.Click += BtnApplyAdjustments_Click;
            panel.Controls.Add(btnApplyAdjustments);

            return panel;
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

            // Current balance of the selected bill, split between the patient and the HMO.
            Panel summary = new Panel();
            summary.Dock = DockStyle.Top;
            summary.Height = 62;
            summary.BackColor = Color.FromArgb(249, 250, 251);
            panel.Controls.Add(summary);
            summary.BringToFront();

            lblPatientBalance = MakeLabel("", 5, 4);
            lblPatientBalance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            summary.Controls.Add(lblPatientBalance);
            lblHmoBalance = MakeLabel("", 5, 23);
            summary.Controls.Add(lblHmoBalance);
            lblPatientOutstanding = MakeLabel("", 5, 42);
            lblPatientOutstanding.ForeColor = Color.FromArgb(180, 83, 9);
            summary.Controls.Add(lblPatientOutstanding);

            Panel entry = new Panel();
            entry.Dock = DockStyle.Top;
            entry.Height = 140;
            panel.Controls.Add(entry);
            entry.BringToFront();

            entry.Controls.Add(MakeLabel("Amount", 5, 5));
            numPayAmount = new NumericUpDown();
            numPayAmount.Location = new Point(5, 23);
            numPayAmount.Size = new Size(110, 28);
            numPayAmount.DecimalPlaces = 2;
            numPayAmount.Maximum = 10000000;
            numPayAmount.ThousandsSeparator = true;
            numPayAmount.ValueChanged += (s, e) => UpdateChange();
            entry.Controls.Add(numPayAmount);

            entry.Controls.Add(MakeLabel("Method", 120, 5));
            cmbMethod = new ComboBox();
            cmbMethod.Location = new Point(120, 23);
            cmbMethod.Size = new Size(110, 28);
            cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMethod.DataSource = Enum.GetValues(typeof(PaymentMethod));
            cmbMethod.SelectedIndexChanged += CmbMethod_SelectedIndexChanged;
            entry.Controls.Add(cmbMethod);

            entry.Controls.Add(MakeLabel("Reference / OR No.", 235, 5));
            txtReference = new TextBox();
            txtReference.Location = new Point(235, 23);
            txtReference.Size = new Size(150, 28);
            entry.Controls.Add(txtReference);

            // Method-specific details: only the panel for the selected method is shown.
            pnlCash = MakeMethodPanel(entry);
            pnlCash.Controls.Add(MakeLabel("Cash tendered", 0, 0));
            numTendered = new NumericUpDown();
            numTendered.Location = new Point(0, 18);
            numTendered.Size = new Size(110, 28);
            numTendered.DecimalPlaces = 2;
            numTendered.Maximum = 10000000;
            numTendered.ThousandsSeparator = true;
            numTendered.ValueChanged += (s, e) => UpdateChange();
            pnlCash.Controls.Add(numTendered);
            lblChange = MakeLabel("Change: 0.00", 120, 21);
            lblChange.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            pnlCash.Controls.Add(lblChange);

            pnlCard = MakeMethodPanel(entry);
            pnlCard.Controls.Add(MakeLabel("Card type", 0, 0));
            cmbCardType = new ComboBox();
            cmbCardType.Location = new Point(0, 18);
            cmbCardType.Size = new Size(110, 28);
            cmbCardType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCardType.Items.AddRange(new object[] { "Visa", "Mastercard", "JCB", "Amex", "Debit card" });
            cmbCardType.SelectedIndex = 0;
            pnlCard.Controls.Add(cmbCardType);
            pnlCard.Controls.Add(MakeLabel("Last 4 digits", 115, 0));
            txtCardLast4 = new TextBox();
            txtCardLast4.Location = new Point(115, 18);
            txtCardLast4.Size = new Size(75, 28);
            txtCardLast4.MaxLength = 4;
            txtCardLast4.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
            pnlCard.Controls.Add(txtCardLast4);
            pnlCard.Controls.Add(MakeLabel("Approval code", 195, 0));
            txtApprovalCode = new TextBox();
            txtApprovalCode.Location = new Point(195, 18);
            txtApprovalCode.Size = new Size(110, 28);
            pnlCard.Controls.Add(txtApprovalCode);

            pnlHmo = MakeMethodPanel(entry);
            pnlHmo.Controls.Add(MakeLabel("HMO provider", 0, 0));
            txtPayHmoProvider = new TextBox();
            txtPayHmoProvider.Location = new Point(0, 18);
            txtPayHmoProvider.Size = new Size(150, 28);
            pnlHmo.Controls.Add(txtPayHmoProvider);
            pnlHmo.Controls.Add(MakeLabel("LOA / Approval No.", 155, 0));
            txtPayHmoLoa = new TextBox();
            txtPayHmoLoa.Location = new Point(155, 18);
            txtPayHmoLoa.Size = new Size(150, 28);
            pnlHmo.Controls.Add(txtPayHmoLoa);

            btnPay = new Button();
            btnPay.Text = "Record Payment";
            btnPay.Location = new Point(5, 104);
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

            ShowMethodPanel();
            return panel;
        }

        private static Panel MakeMethodPanel(Panel entry)
        {
            Panel p = new Panel();
            p.Location = new Point(5, 55);
            p.Size = new Size(390, 46);
            entry.Controls.Add(p);
            return p;
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
            if (pnlCash == null) return;   // still building the panel
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
            if (lblChange == null || numTendered == null) return;
            decimal change = numTendered.Value - numPayAmount.Value;
            lblChange.Text = change >= 0 ? "Change: " + Money(change) : "Short by " + Money(-change);
            lblChange.ForeColor = change >= 0 ? Color.Black : Color.FromArgb(220, 38, 38);
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
            cmbPatient.DisplayMember = "Name";
            cmbPatient.ValueMember = "Id";
            cmbPatient.DataSource = HospitalData.ActivePatients().ToList();
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
                .Where(b => filter == AllStatuses
                    || (filter == WithBalance ? b.Status != BillStatus.Cancelled && b.TotalOutstanding > 0
                                              : b.Status.ToString() == filter))
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

            decimal patientsOwe = HospitalData.OutstandingBalance(), hmosOwe = HospitalData.OutstandingHmo();
            lblBills.Text = "Bills    Outstanding - patients: " + Money(patientsOwe) + "   HMO: " + Money(hmosOwe) +
                            "   Total: " + Money(patientsOwe + hmosOwe);
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
            btnCancelBill.Enabled = editable;
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
            decimal allBills = HospitalData.PatientOutstanding(bill.PatientId);
            int count = HospitalData.PatientBillsWithBalance(bill.PatientId);
            lblPatientOutstanding.Text = allBills > bill.Balance
                ? HospitalData.PatientName(bill.PatientId) + " owes " + Money(allBills) + " across " + count + " bills."
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
            var adj = bill != null ? bill.Adjustments : new BillAdjustments { VatRate = HospitalData.DefaultVatRate };

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
            var patient = bill != null ? HospitalData.GetPatient(bill.PatientId) : null;
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
                HospitalData.UpdateBillAdjustments(bill, adj);
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

            using (var printer = new BillStatementPrinter(bill))
                printer.ShowPreview(FindForm());
        }

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
                msg += "\n\nThe patient is still admitted, so per-day charges cover the " +
                       admission.CalculateDaysStayed() + " day(s) so far and will be updated on discharge.";
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
                HospitalData.RecordPayment(bill, payment);

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

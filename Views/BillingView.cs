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

        // Top: nurse notice / Create Bill form, bills header, filter row, bills grid
        private Label lblNurseNotice;
        private Panel createPanel;
        private Label lblCreateTitle;
        private Label lblPatient;
        private ComboBox cmbPatient;
        private Label lblAdmission;
        private ComboBox cmbAdmission;
        private Label lblAppointment;
        private ComboBox cmbAppointment;
        private CheckBox chkAutoCharges;
        private Label lblNotes;
        private TextBox txtNotes;
        private Button btnCreate;
        private Label lblBills;
        private Panel btnPanel;
        private Label lblShow;
        private ComboBox cmbFilter;
        private Button btnSchedule;
        private DataGridView gridBills;
        private TableLayoutPanel detail;

        // Detail left: bill items
        private Panel itemsCard;
        private Label lblItems;
        private Panel itemEntry;
        private Label lblItemDesc;
        private TextBox txtItemDesc;
        private Label lblCategory;
        private ComboBox cmbCategory;
        private Label lblQty;
        private NumericUpDown numQty;
        private Label lblUnitPrice;
        private NumericUpDown numUnitPrice;
        private Button btnAddItem;
        private Button btnRemoveItem;
        private DataGridView gridItems;

        // Detail right: tabs
        private TabControl tabsSummary;
        private TabPage tabBreakdown;
        private TabPage adjustmentsTab;
        private TabPage tabPayments;

        // Breakdown tab
        private Panel breakdownCard;
        private DataGridView gridBreakdown;
        private DataGridViewTextBoxColumn colBreakdownLabel;
        private DataGridViewTextBoxColumn colBreakdownAmount;
        private Panel breakdownBottom;
        private Button btnPrintStatement;
        private readonly Font breakdownBold = new Font("Segoe UI", 9F, FontStyle.Bold);

        // Discounts / Tax / HMO tab
        private Panel adjustmentsCard;
        private Label lblDiscount;
        private NumericUpDown numDiscount;
        private ComboBox cmbDiscountType;
        private Label lblDiscountReason;
        private TextBox txtDiscountReason;
        private Label lblEligibility;
        private ComboBox cmbEligibility;
        private Label lblEligibilityId;
        private TextBox txtEligibilityId;
        private Label lblEligibilityHint;
        private Label lblVatRate;
        private NumericUpDown numVatRate;
        private Label lblVatNote;
        private Label lblHmoProvider;
        private ComboBox cmbHmoProvider;
        private Label lblHmoLoa;
        private TextBox txtHmoLoa;
        private Label lblHmoCoverage;
        private NumericUpDown numHmoCoverage;
        private Button btnApplyAdjustments;

        // Payments tab
        private Panel paymentsCard;
        private Label lblPayments;
        private Panel balanceSummary;
        private Label lblPatientBalance;
        private Label lblHmoBalance;
        private Label lblPatientOutstanding;
        private Panel paymentEntry;
        private Label lblPayAmount;
        private NumericUpDown numPayAmount;
        private Label lblMethod;
        private ComboBox cmbMethod;
        private Label lblReference;
        private TextBox txtReference;
        private Panel pnlCash;
        private Label lblTendered;
        private NumericUpDown numTendered;
        private Label lblChange;
        private Panel pnlCard;
        private Label lblCardType;
        private ComboBox cmbCardType;
        private Label lblCardLast4;
        private TextBox txtCardLast4;
        private Label lblApproval;
        private TextBox txtApprovalCode;
        private Panel pnlHmo;
        private Label lblPayHmoProvider;
        private TextBox txtPayHmoProvider;
        private Label lblPayHmoLoa;
        private TextBox txtPayHmoLoa;
        private Button btnPay;
        private Label lblPaymentsByAdmin;
        private DataGridView gridPayments;

        private bool loading;

        public BillingView()
        {
            // Every control and event is set up in InitializeComponent() (Designer format),
            // so the Designer shows the complete screen. Lists that come from enums are
            // filled here instead, since designer code can't enumerate them.
            InitializeComponent();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
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
        }

        // Nurses view bills, print statements and add charges for what the patient used;
        // creating bills, removing charges, discounts/VAT/HMO, payments and the charge schedule are
        // administrator work. Hidden here, and refused by HospitalData if reached anyway.
        private void ApplyPermissions()
        {
            createPanel.Visible = Permissions.Can(Permission.CreateBills);
            lblNurseNotice.Visible = !createPanel.Visible;   // says what this role can do here
            btnSchedule.Visible = Permissions.Can(Permission.ManageChargeSchedule);
            btnAddItem.Visible = Permissions.Can(Permission.AddBillCharges);
            btnRemoveItem.Visible = Permissions.Can(Permission.RemoveBillCharges);
            if (!Permissions.Can(Permission.AdjustBills))
                tabsSummary.TabPages.Remove(adjustmentsTab);

            bool pays = Permissions.Can(Permission.RecordPayments);
            paymentEntry.Visible = pays;
            lblPaymentsByAdmin.Visible = !pays;
        }

        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            using (var dlg = new ChargeScheduleForm())
                dlg.ShowDialog(this);
        }

        private void CmbFilter_SelectedIndexChanged(object sender, EventArgs e) => LoadBills();

        private void PaymentAmount_ValueChanged(object sender, EventArgs e) => UpdateChange();

        // Only digits in the card's last-4 box.
        private void TxtCardLast4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                e.Handled = true;
        }

        // Designer-generated layout, top to bottom: the nurse notice (hidden for admins), the
        // Create Bill form (admins), the "Bills" header, the filter row, the bills grid, and a
        // two-column detail area: bill items on the left; Breakdown / Discounts, Tax & HMO /
        // Payments tabs on the right. The cash, card and HMO panels share one spot in the
        // payment form; only the one for the selected method is shown at runtime.
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblNurseNotice = new System.Windows.Forms.Label();
            this.createPanel = new System.Windows.Forms.Panel();
            this.lblCreateTitle = new System.Windows.Forms.Label();
            this.lblPatient = new System.Windows.Forms.Label();
            this.cmbPatient = new System.Windows.Forms.ComboBox();
            this.lblAdmission = new System.Windows.Forms.Label();
            this.cmbAdmission = new System.Windows.Forms.ComboBox();
            this.lblAppointment = new System.Windows.Forms.Label();
            this.cmbAppointment = new System.Windows.Forms.ComboBox();
            this.chkAutoCharges = new System.Windows.Forms.CheckBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.lblBills = new System.Windows.Forms.Label();
            this.btnPanel = new System.Windows.Forms.Panel();
            this.lblShow = new System.Windows.Forms.Label();
            this.cmbFilter = new System.Windows.Forms.ComboBox();
            this.btnSchedule = new System.Windows.Forms.Button();
            this.gridBills = new System.Windows.Forms.DataGridView();
            this.detail = new System.Windows.Forms.TableLayoutPanel();
            this.itemsCard = new System.Windows.Forms.Panel();
            this.gridItems = new System.Windows.Forms.DataGridView();
            this.itemEntry = new System.Windows.Forms.Panel();
            this.lblItemDesc = new System.Windows.Forms.Label();
            this.txtItemDesc = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblQty = new System.Windows.Forms.Label();
            this.numQty = new System.Windows.Forms.NumericUpDown();
            this.lblUnitPrice = new System.Windows.Forms.Label();
            this.numUnitPrice = new System.Windows.Forms.NumericUpDown();
            this.btnAddItem = new System.Windows.Forms.Button();
            this.btnRemoveItem = new System.Windows.Forms.Button();
            this.lblItems = new System.Windows.Forms.Label();
            this.tabsSummary = new System.Windows.Forms.TabControl();
            this.tabBreakdown = new System.Windows.Forms.TabPage();
            this.breakdownCard = new System.Windows.Forms.Panel();
            this.gridBreakdown = new System.Windows.Forms.DataGridView();
            this.colBreakdownLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBreakdownAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.breakdownBottom = new System.Windows.Forms.Panel();
            this.btnPrintStatement = new System.Windows.Forms.Button();
            this.adjustmentsTab = new System.Windows.Forms.TabPage();
            this.adjustmentsCard = new System.Windows.Forms.Panel();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.numDiscount = new System.Windows.Forms.NumericUpDown();
            this.cmbDiscountType = new System.Windows.Forms.ComboBox();
            this.lblDiscountReason = new System.Windows.Forms.Label();
            this.txtDiscountReason = new System.Windows.Forms.TextBox();
            this.lblEligibility = new System.Windows.Forms.Label();
            this.cmbEligibility = new System.Windows.Forms.ComboBox();
            this.lblEligibilityId = new System.Windows.Forms.Label();
            this.txtEligibilityId = new System.Windows.Forms.TextBox();
            this.lblEligibilityHint = new System.Windows.Forms.Label();
            this.lblVatRate = new System.Windows.Forms.Label();
            this.numVatRate = new System.Windows.Forms.NumericUpDown();
            this.lblVatNote = new System.Windows.Forms.Label();
            this.lblHmoProvider = new System.Windows.Forms.Label();
            this.cmbHmoProvider = new System.Windows.Forms.ComboBox();
            this.lblHmoLoa = new System.Windows.Forms.Label();
            this.txtHmoLoa = new System.Windows.Forms.TextBox();
            this.lblHmoCoverage = new System.Windows.Forms.Label();
            this.numHmoCoverage = new System.Windows.Forms.NumericUpDown();
            this.btnApplyAdjustments = new System.Windows.Forms.Button();
            this.tabPayments = new System.Windows.Forms.TabPage();
            this.paymentsCard = new System.Windows.Forms.Panel();
            this.gridPayments = new System.Windows.Forms.DataGridView();
            this.lblPaymentsByAdmin = new System.Windows.Forms.Label();
            this.paymentEntry = new System.Windows.Forms.Panel();
            this.lblPayAmount = new System.Windows.Forms.Label();
            this.numPayAmount = new System.Windows.Forms.NumericUpDown();
            this.lblMethod = new System.Windows.Forms.Label();
            this.cmbMethod = new System.Windows.Forms.ComboBox();
            this.lblReference = new System.Windows.Forms.Label();
            this.txtReference = new System.Windows.Forms.TextBox();
            this.pnlCash = new System.Windows.Forms.Panel();
            this.lblTendered = new System.Windows.Forms.Label();
            this.numTendered = new System.Windows.Forms.NumericUpDown();
            this.lblChange = new System.Windows.Forms.Label();
            this.pnlCard = new System.Windows.Forms.Panel();
            this.lblCardType = new System.Windows.Forms.Label();
            this.cmbCardType = new System.Windows.Forms.ComboBox();
            this.lblCardLast4 = new System.Windows.Forms.Label();
            this.txtCardLast4 = new System.Windows.Forms.TextBox();
            this.lblApproval = new System.Windows.Forms.Label();
            this.txtApprovalCode = new System.Windows.Forms.TextBox();
            this.pnlHmo = new System.Windows.Forms.Panel();
            this.lblPayHmoProvider = new System.Windows.Forms.Label();
            this.txtPayHmoProvider = new System.Windows.Forms.TextBox();
            this.lblPayHmoLoa = new System.Windows.Forms.Label();
            this.txtPayHmoLoa = new System.Windows.Forms.TextBox();
            this.btnPay = new System.Windows.Forms.Button();
            this.balanceSummary = new System.Windows.Forms.Panel();
            this.lblPatientBalance = new System.Windows.Forms.Label();
            this.lblHmoBalance = new System.Windows.Forms.Label();
            this.lblPatientOutstanding = new System.Windows.Forms.Label();
            this.lblPayments = new System.Windows.Forms.Label();
            this.createPanel.SuspendLayout();
            this.btnPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridBills)).BeginInit();
            this.detail.SuspendLayout();
            this.itemsCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridItems)).BeginInit();
            this.itemEntry.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQty)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUnitPrice)).BeginInit();
            this.tabsSummary.SuspendLayout();
            this.tabBreakdown.SuspendLayout();
            this.breakdownCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridBreakdown)).BeginInit();
            this.breakdownBottom.SuspendLayout();
            this.adjustmentsTab.SuspendLayout();
            this.adjustmentsCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDiscount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numVatRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHmoCoverage)).BeginInit();
            this.tabPayments.SuspendLayout();
            this.paymentsCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPayments)).BeginInit();
            this.paymentEntry.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPayAmount)).BeginInit();
            this.pnlCash.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTendered)).BeginInit();
            this.pnlCard.SuspendLayout();
            this.pnlHmo.SuspendLayout();
            this.balanceSummary.SuspendLayout();
            this.SuspendLayout();
            //
            // lblNurseNotice
            // Shown instead of the Create Bill form for roles that can't create bills (nurses).
            //
            this.lblNurseNotice.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(253)))), ((int)(((byte)(250)))));
            this.lblNurseNotice.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNurseNotice.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblNurseNotice.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(118)))), ((int)(((byte)(110)))));
            this.lblNurseNotice.Location = new System.Drawing.Point(10, 10);
            this.lblNurseNotice.Name = "lblNurseNotice";
            this.lblNurseNotice.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.lblNurseNotice.Size = new System.Drawing.Size(1004, 44);
            this.lblNurseNotice.TabIndex = 5;
            this.lblNurseNotice.Text = "Nurse access: view bills and their breakdown, print statements, and add charges (medicines, laboratory, procedures, supplies). Payments, discounts and VAT/HMO are handled by an administrator. Admission bills are created automatically.";
            this.lblNurseNotice.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblNurseNotice.Visible = false;
            //
            // createPanel
            //
            this.createPanel.BackColor = System.Drawing.Color.White;
            this.createPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.createPanel.Controls.Add(this.lblCreateTitle);
            this.createPanel.Controls.Add(this.lblPatient);
            this.createPanel.Controls.Add(this.cmbPatient);
            this.createPanel.Controls.Add(this.lblAdmission);
            this.createPanel.Controls.Add(this.cmbAdmission);
            this.createPanel.Controls.Add(this.lblAppointment);
            this.createPanel.Controls.Add(this.cmbAppointment);
            this.createPanel.Controls.Add(this.chkAutoCharges);
            this.createPanel.Controls.Add(this.lblNotes);
            this.createPanel.Controls.Add(this.txtNotes);
            this.createPanel.Controls.Add(this.btnCreate);
            this.createPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.createPanel.Location = new System.Drawing.Point(10, 54);
            this.createPanel.Name = "createPanel";
            this.createPanel.Padding = new System.Windows.Forms.Padding(15);
            this.createPanel.Size = new System.Drawing.Size(1004, 145);
            this.createPanel.TabIndex = 4;
            //
            // lblCreateTitle
            //
            this.lblCreateTitle.AutoSize = true;
            this.lblCreateTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblCreateTitle.Location = new System.Drawing.Point(15, 10);
            this.lblCreateTitle.Name = "lblCreateTitle";
            this.lblCreateTitle.Size = new System.Drawing.Size(94, 21);
            this.lblCreateTitle.TabIndex = 0;
            this.lblCreateTitle.Text = "Create Bill";
            //
            // lblPatient
            //
            this.lblPatient.AutoSize = true;
            this.lblPatient.Location = new System.Drawing.Point(15, 45);
            this.lblPatient.Name = "lblPatient";
            this.lblPatient.Size = new System.Drawing.Size(40, 13);
            this.lblPatient.TabIndex = 1;
            this.lblPatient.Text = "Patient";
            //
            // cmbPatient
            //
            this.cmbPatient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatient.Location = new System.Drawing.Point(15, 65);
            this.cmbPatient.Name = "cmbPatient";
            this.cmbPatient.Size = new System.Drawing.Size(250, 21);
            this.cmbPatient.TabIndex = 2;
            this.cmbPatient.SelectedIndexChanged += new System.EventHandler(this.CmbPatient_SelectedIndexChanged);
            //
            // lblAdmission
            //
            this.lblAdmission.AutoSize = true;
            this.lblAdmission.Location = new System.Drawing.Point(280, 45);
            this.lblAdmission.Name = "lblAdmission";
            this.lblAdmission.Size = new System.Drawing.Size(106, 13);
            this.lblAdmission.TabIndex = 3;
            this.lblAdmission.Text = "Admission (optional)";
            //
            // cmbAdmission
            //
            this.cmbAdmission.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAdmission.Location = new System.Drawing.Point(280, 65);
            this.cmbAdmission.Name = "cmbAdmission";
            this.cmbAdmission.Size = new System.Drawing.Size(240, 21);
            this.cmbAdmission.TabIndex = 4;
            //
            // lblAppointment
            //
            this.lblAppointment.AutoSize = true;
            this.lblAppointment.Location = new System.Drawing.Point(535, 45);
            this.lblAppointment.Name = "lblAppointment";
            this.lblAppointment.Size = new System.Drawing.Size(119, 13);
            this.lblAppointment.TabIndex = 5;
            this.lblAppointment.Text = "Appointment (optional)";
            //
            // cmbAppointment
            //
            this.cmbAppointment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAppointment.Location = new System.Drawing.Point(535, 65);
            this.cmbAppointment.Name = "cmbAppointment";
            this.cmbAppointment.Size = new System.Drawing.Size(240, 21);
            this.cmbAppointment.TabIndex = 6;
            //
            // chkAutoCharges
            //
            this.chkAutoCharges.AutoSize = true;
            this.chkAutoCharges.Checked = true;
            this.chkAutoCharges.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoCharges.Location = new System.Drawing.Point(15, 102);
            this.chkAutoCharges.Name = "chkAutoCharges";
            this.chkAutoCharges.Size = new System.Drawing.Size(135, 17);
            this.chkAutoCharges.TabIndex = 7;
            this.chkAutoCharges.Text = "Add scheduled charges";
            //
            // lblNotes
            //
            this.lblNotes.AutoSize = true;
            this.lblNotes.Location = new System.Drawing.Point(280, 105);
            this.lblNotes.Name = "lblNotes";
            this.lblNotes.Size = new System.Drawing.Size(35, 13);
            this.lblNotes.TabIndex = 8;
            this.lblNotes.Text = "Notes";
            //
            // txtNotes
            //
            this.txtNotes.Location = new System.Drawing.Point(325, 102);
            this.txtNotes.Name = "txtNotes";
            this.txtNotes.Size = new System.Drawing.Size(310, 20);
            this.txtNotes.TabIndex = 9;
            //
            // btnCreate
            //
            this.btnCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreate.ForeColor = System.Drawing.Color.White;
            this.btnCreate.Location = new System.Drawing.Point(650, 99);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(125, 30);
            this.btnCreate.TabIndex = 10;
            this.btnCreate.Text = "Create Bill";
            this.btnCreate.UseVisualStyleBackColor = false;
            this.btnCreate.Click += new System.EventHandler(this.BtnCreate_Click);
            //
            // lblBills
            //
            this.lblBills.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBills.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblBills.Location = new System.Drawing.Point(10, 199);
            this.lblBills.Name = "lblBills";
            this.lblBills.Size = new System.Drawing.Size(1004, 30);
            this.lblBills.TabIndex = 3;
            this.lblBills.Text = "Bills";
            this.lblBills.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // btnPanel
            //
            this.btnPanel.Controls.Add(this.lblShow);
            this.btnPanel.Controls.Add(this.cmbFilter);
            this.btnPanel.Controls.Add(this.btnSchedule);
            this.btnPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnPanel.Location = new System.Drawing.Point(10, 229);
            this.btnPanel.Name = "btnPanel";
            this.btnPanel.Size = new System.Drawing.Size(1004, 40);
            this.btnPanel.TabIndex = 2;
            //
            // lblShow
            //
            this.lblShow.AutoSize = true;
            this.lblShow.Location = new System.Drawing.Point(0, 12);
            this.lblShow.Name = "lblShow";
            this.lblShow.Size = new System.Drawing.Size(37, 13);
            this.lblShow.TabIndex = 0;
            this.lblShow.Text = "Show:";
            //
            // cmbFilter
            // "All", "Outstanding" (anything still owed), then each bill status.
            //
            this.cmbFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilter.Items.AddRange(new object[] {
            "All",
            "Outstanding",
            "Unpaid",
            "PartiallyPaid",
            "Paid",
            "Cancelled"});
            this.cmbFilter.Location = new System.Drawing.Point(42, 8);
            this.cmbFilter.Name = "cmbFilter";
            this.cmbFilter.Size = new System.Drawing.Size(130, 21);
            this.cmbFilter.TabIndex = 1;
            this.cmbFilter.SelectedIndexChanged += new System.EventHandler(this.CmbFilter_SelectedIndexChanged);
            //
            // btnSchedule
            //
            this.btnSchedule.Location = new System.Drawing.Point(190, 5);
            this.btnSchedule.Name = "btnSchedule";
            this.btnSchedule.Size = new System.Drawing.Size(190, 30);
            this.btnSchedule.TabIndex = 2;
            this.btnSchedule.Text = "Admission Charge Schedule...";
            this.btnSchedule.UseVisualStyleBackColor = true;
            this.btnSchedule.Click += new System.EventHandler(this.BtnSchedule_Click);
            //
            // gridBills
            //
            this.gridBills.AllowUserToAddRows = false;
            this.gridBills.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridBills.BackgroundColor = System.Drawing.Color.White;
            this.gridBills.Dock = System.Windows.Forms.DockStyle.Top;
            this.gridBills.Location = new System.Drawing.Point(10, 269);
            this.gridBills.MultiSelect = false;
            this.gridBills.Name = "gridBills";
            this.gridBills.ReadOnly = true;
            this.gridBills.RowHeadersVisible = false;
            this.gridBills.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridBills.Size = new System.Drawing.Size(1004, 190);
            this.gridBills.TabIndex = 1;
            this.gridBills.SelectionChanged += new System.EventHandler(this.GridBills_SelectionChanged);
            //
            // detail
            //
            this.detail.ColumnCount = 2;
            this.detail.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.detail.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.detail.Controls.Add(this.itemsCard, 0, 0);
            this.detail.Controls.Add(this.tabsSummary, 1, 0);
            this.detail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detail.Location = new System.Drawing.Point(10, 459);
            this.detail.Name = "detail";
            this.detail.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.detail.RowCount = 1;
            this.detail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.detail.Size = new System.Drawing.Size(1004, 291);
            this.detail.TabIndex = 0;
            //
            // itemsCard
            //
            this.itemsCard.BackColor = System.Drawing.Color.White;
            this.itemsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.itemsCard.Controls.Add(this.gridItems);
            this.itemsCard.Controls.Add(this.itemEntry);
            this.itemsCard.Controls.Add(this.lblItems);
            this.itemsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemsCard.Location = new System.Drawing.Point(0, 10);
            this.itemsCard.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.itemsCard.Name = "itemsCard";
            this.itemsCard.Padding = new System.Windows.Forms.Padding(10);
            this.itemsCard.Size = new System.Drawing.Size(547, 281);
            this.itemsCard.TabIndex = 0;
            //
            // lblItems
            //
            this.lblItems.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblItems.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblItems.Location = new System.Drawing.Point(10, 10);
            this.lblItems.Name = "lblItems";
            this.lblItems.Size = new System.Drawing.Size(525, 28);
            this.lblItems.TabIndex = 0;
            this.lblItems.Text = "Bill Items (select a bill)";
            //
            // itemEntry
            //
            this.itemEntry.Controls.Add(this.lblItemDesc);
            this.itemEntry.Controls.Add(this.txtItemDesc);
            this.itemEntry.Controls.Add(this.lblCategory);
            this.itemEntry.Controls.Add(this.cmbCategory);
            this.itemEntry.Controls.Add(this.lblQty);
            this.itemEntry.Controls.Add(this.numQty);
            this.itemEntry.Controls.Add(this.lblUnitPrice);
            this.itemEntry.Controls.Add(this.numUnitPrice);
            this.itemEntry.Controls.Add(this.btnAddItem);
            this.itemEntry.Controls.Add(this.btnRemoveItem);
            this.itemEntry.Dock = System.Windows.Forms.DockStyle.Top;
            this.itemEntry.Location = new System.Drawing.Point(10, 38);
            this.itemEntry.Name = "itemEntry";
            this.itemEntry.Size = new System.Drawing.Size(525, 90);
            this.itemEntry.TabIndex = 1;
            //
            // lblItemDesc
            //
            this.lblItemDesc.AutoSize = true;
            this.lblItemDesc.Location = new System.Drawing.Point(5, 0);
            this.lblItemDesc.Name = "lblItemDesc";
            this.lblItemDesc.Size = new System.Drawing.Size(60, 13);
            this.lblItemDesc.TabIndex = 0;
            this.lblItemDesc.Text = "Description";
            //
            // txtItemDesc
            //
            this.txtItemDesc.Location = new System.Drawing.Point(5, 18);
            this.txtItemDesc.Name = "txtItemDesc";
            this.txtItemDesc.Size = new System.Drawing.Size(200, 20);
            this.txtItemDesc.TabIndex = 1;
            //
            // lblCategory
            //
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(210, 0);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(49, 13);
            this.lblCategory.TabIndex = 2;
            this.lblCategory.Text = "Category";
            //
            // cmbCategory
            // (items come from the BillCategory enum at runtime)
            //
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.Location = new System.Drawing.Point(210, 18);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(110, 21);
            this.cmbCategory.TabIndex = 3;
            //
            // lblQty
            //
            this.lblQty.AutoSize = true;
            this.lblQty.Location = new System.Drawing.Point(325, 0);
            this.lblQty.Name = "lblQty";
            this.lblQty.Size = new System.Drawing.Size(23, 13);
            this.lblQty.TabIndex = 4;
            this.lblQty.Text = "Qty";
            //
            // numQty
            //
            this.numQty.Location = new System.Drawing.Point(325, 18);
            this.numQty.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numQty.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numQty.Name = "numQty";
            this.numQty.Size = new System.Drawing.Size(55, 20);
            this.numQty.TabIndex = 5;
            this.numQty.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            //
            // lblUnitPrice
            //
            this.lblUnitPrice.AutoSize = true;
            this.lblUnitPrice.Location = new System.Drawing.Point(385, 0);
            this.lblUnitPrice.Name = "lblUnitPrice";
            this.lblUnitPrice.Size = new System.Drawing.Size(53, 13);
            this.lblUnitPrice.TabIndex = 6;
            this.lblUnitPrice.Text = "Unit Price";
            //
            // numUnitPrice
            //
            this.numUnitPrice.DecimalPlaces = 2;
            this.numUnitPrice.Location = new System.Drawing.Point(385, 18);
            this.numUnitPrice.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numUnitPrice.Name = "numUnitPrice";
            this.numUnitPrice.Size = new System.Drawing.Size(100, 20);
            this.numUnitPrice.TabIndex = 7;
            this.numUnitPrice.ThousandsSeparator = true;
            //
            // btnAddItem
            //
            this.btnAddItem.Location = new System.Drawing.Point(5, 52);
            this.btnAddItem.Name = "btnAddItem";
            this.btnAddItem.Size = new System.Drawing.Size(100, 30);
            this.btnAddItem.TabIndex = 8;
            this.btnAddItem.Text = "Add Item";
            this.btnAddItem.UseVisualStyleBackColor = true;
            this.btnAddItem.Click += new System.EventHandler(this.BtnAddItem_Click);
            //
            // btnRemoveItem
            //
            this.btnRemoveItem.Location = new System.Drawing.Point(110, 52);
            this.btnRemoveItem.Name = "btnRemoveItem";
            this.btnRemoveItem.Size = new System.Drawing.Size(130, 30);
            this.btnRemoveItem.TabIndex = 9;
            this.btnRemoveItem.Text = "Remove Selected";
            this.btnRemoveItem.UseVisualStyleBackColor = true;
            this.btnRemoveItem.Click += new System.EventHandler(this.BtnRemoveItem_Click);
            //
            // gridItems
            //
            this.gridItems.AllowUserToAddRows = false;
            this.gridItems.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridItems.BackgroundColor = System.Drawing.Color.White;
            this.gridItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridItems.Location = new System.Drawing.Point(10, 128);
            this.gridItems.MultiSelect = false;
            this.gridItems.Name = "gridItems";
            this.gridItems.ReadOnly = true;
            this.gridItems.RowHeadersVisible = false;
            this.gridItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridItems.Size = new System.Drawing.Size(525, 141);
            this.gridItems.TabIndex = 2;
            //
            // tabsSummary
            // Right-hand side: breakdown of the selected bill, its adjustments, and payments.
            //
            this.tabsSummary.Controls.Add(this.tabBreakdown);
            this.tabsSummary.Controls.Add(this.adjustmentsTab);
            this.tabsSummary.Controls.Add(this.tabPayments);
            this.tabsSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsSummary.Location = new System.Drawing.Point(557, 10);
            this.tabsSummary.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.tabsSummary.Name = "tabsSummary";
            this.tabsSummary.SelectedIndex = 0;
            this.tabsSummary.Size = new System.Drawing.Size(447, 281);
            this.tabsSummary.TabIndex = 1;
            //
            // tabBreakdown
            //
            this.tabBreakdown.Controls.Add(this.breakdownCard);
            this.tabBreakdown.Location = new System.Drawing.Point(4, 22);
            this.tabBreakdown.Name = "tabBreakdown";
            this.tabBreakdown.Size = new System.Drawing.Size(439, 255);
            this.tabBreakdown.TabIndex = 0;
            this.tabBreakdown.Text = "Breakdown";
            this.tabBreakdown.UseVisualStyleBackColor = true;
            //
            // breakdownCard
            //
            this.breakdownCard.BackColor = System.Drawing.Color.White;
            this.breakdownCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.breakdownCard.Controls.Add(this.gridBreakdown);
            this.breakdownCard.Controls.Add(this.breakdownBottom);
            this.breakdownCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.breakdownCard.Location = new System.Drawing.Point(0, 0);
            this.breakdownCard.Name = "breakdownCard";
            this.breakdownCard.Padding = new System.Windows.Forms.Padding(10);
            this.breakdownCard.Size = new System.Drawing.Size(439, 255);
            this.breakdownCard.TabIndex = 0;
            //
            // gridBreakdown
            // Two columns (description, amount), no headers; rows are added in LoadBreakdown().
            //
            this.gridBreakdown.AllowUserToAddRows = false;
            this.gridBreakdown.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.gridBreakdown.BackgroundColor = System.Drawing.Color.White;
            this.gridBreakdown.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gridBreakdown.ColumnHeadersVisible = false;
            this.gridBreakdown.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colBreakdownLabel,
            this.colBreakdownAmount});
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridBreakdown.DefaultCellStyle = dataGridViewCellStyle3;
            this.gridBreakdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridBreakdown.Location = new System.Drawing.Point(10, 10);
            this.gridBreakdown.MultiSelect = false;
            this.gridBreakdown.Name = "gridBreakdown";
            this.gridBreakdown.ReadOnly = true;
            this.gridBreakdown.RowHeadersVisible = false;
            this.gridBreakdown.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridBreakdown.Size = new System.Drawing.Size(417, 193);
            this.gridBreakdown.TabIndex = 0;
            //
            // colBreakdownLabel
            //
            this.colBreakdownLabel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.colBreakdownLabel.DefaultCellStyle = dataGridViewCellStyle1;
            this.colBreakdownLabel.HeaderText = "Label";
            this.colBreakdownLabel.Name = "colBreakdownLabel";
            this.colBreakdownLabel.ReadOnly = true;
            //
            // colBreakdownAmount
            //
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.colBreakdownAmount.DefaultCellStyle = dataGridViewCellStyle2;
            this.colBreakdownAmount.HeaderText = "Amount";
            this.colBreakdownAmount.Name = "colBreakdownAmount";
            this.colBreakdownAmount.ReadOnly = true;
            this.colBreakdownAmount.Width = 110;
            //
            // breakdownBottom
            //
            this.breakdownBottom.Controls.Add(this.btnPrintStatement);
            this.breakdownBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.breakdownBottom.Location = new System.Drawing.Point(10, 203);
            this.breakdownBottom.Name = "breakdownBottom";
            this.breakdownBottom.Size = new System.Drawing.Size(417, 40);
            this.breakdownBottom.TabIndex = 1;
            //
            // btnPrintStatement
            //
            this.btnPrintStatement.Location = new System.Drawing.Point(0, 6);
            this.btnPrintStatement.Name = "btnPrintStatement";
            this.btnPrintStatement.Size = new System.Drawing.Size(140, 30);
            this.btnPrintStatement.TabIndex = 0;
            this.btnPrintStatement.Text = "Print Statement...";
            this.btnPrintStatement.UseVisualStyleBackColor = true;
            this.btnPrintStatement.Click += new System.EventHandler(this.BtnPrintStatement_Click);
            //
            // adjustmentsTab
            //
            this.adjustmentsTab.Controls.Add(this.adjustmentsCard);
            this.adjustmentsTab.Location = new System.Drawing.Point(4, 22);
            this.adjustmentsTab.Name = "adjustmentsTab";
            this.adjustmentsTab.Size = new System.Drawing.Size(439, 255);
            this.adjustmentsTab.TabIndex = 1;
            this.adjustmentsTab.Text = "Discounts / Tax / HMO";
            this.adjustmentsTab.UseVisualStyleBackColor = true;
            //
            // adjustmentsCard
            //
            this.adjustmentsCard.AutoScroll = true;
            this.adjustmentsCard.BackColor = System.Drawing.Color.White;
            this.adjustmentsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.adjustmentsCard.Controls.Add(this.lblDiscount);
            this.adjustmentsCard.Controls.Add(this.numDiscount);
            this.adjustmentsCard.Controls.Add(this.cmbDiscountType);
            this.adjustmentsCard.Controls.Add(this.lblDiscountReason);
            this.adjustmentsCard.Controls.Add(this.txtDiscountReason);
            this.adjustmentsCard.Controls.Add(this.lblEligibility);
            this.adjustmentsCard.Controls.Add(this.cmbEligibility);
            this.adjustmentsCard.Controls.Add(this.lblEligibilityId);
            this.adjustmentsCard.Controls.Add(this.txtEligibilityId);
            this.adjustmentsCard.Controls.Add(this.lblEligibilityHint);
            this.adjustmentsCard.Controls.Add(this.lblVatRate);
            this.adjustmentsCard.Controls.Add(this.numVatRate);
            this.adjustmentsCard.Controls.Add(this.lblVatNote);
            this.adjustmentsCard.Controls.Add(this.lblHmoProvider);
            this.adjustmentsCard.Controls.Add(this.cmbHmoProvider);
            this.adjustmentsCard.Controls.Add(this.lblHmoLoa);
            this.adjustmentsCard.Controls.Add(this.txtHmoLoa);
            this.adjustmentsCard.Controls.Add(this.lblHmoCoverage);
            this.adjustmentsCard.Controls.Add(this.numHmoCoverage);
            this.adjustmentsCard.Controls.Add(this.btnApplyAdjustments);
            this.adjustmentsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.adjustmentsCard.Location = new System.Drawing.Point(0, 0);
            this.adjustmentsCard.Name = "adjustmentsCard";
            this.adjustmentsCard.Padding = new System.Windows.Forms.Padding(10);
            this.adjustmentsCard.Size = new System.Drawing.Size(439, 255);
            this.adjustmentsCard.TabIndex = 0;
            //
            // lblDiscount
            //
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Location = new System.Drawing.Point(5, 5);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(49, 13);
            this.lblDiscount.TabIndex = 0;
            this.lblDiscount.Text = "Discount";
            //
            // numDiscount
            //
            this.numDiscount.DecimalPlaces = 2;
            this.numDiscount.Location = new System.Drawing.Point(5, 23);
            this.numDiscount.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numDiscount.Name = "numDiscount";
            this.numDiscount.Size = new System.Drawing.Size(85, 20);
            this.numDiscount.TabIndex = 1;
            this.numDiscount.ThousandsSeparator = true;
            //
            // cmbDiscountType
            //
            this.cmbDiscountType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDiscountType.Items.AddRange(new object[] {
            "%",
            "PHP"});
            this.cmbDiscountType.Location = new System.Drawing.Point(95, 23);
            this.cmbDiscountType.Name = "cmbDiscountType";
            this.cmbDiscountType.Size = new System.Drawing.Size(55, 21);
            this.cmbDiscountType.TabIndex = 2;
            //
            // lblDiscountReason
            //
            this.lblDiscountReason.AutoSize = true;
            this.lblDiscountReason.Location = new System.Drawing.Point(160, 5);
            this.lblDiscountReason.Name = "lblDiscountReason";
            this.lblDiscountReason.Size = new System.Drawing.Size(199, 13);
            this.lblDiscountReason.TabIndex = 3;
            this.lblDiscountReason.Text = "Reason (e.g. employee, promo, charity)";
            //
            // txtDiscountReason
            //
            this.txtDiscountReason.Location = new System.Drawing.Point(160, 23);
            this.txtDiscountReason.Name = "txtDiscountReason";
            this.txtDiscountReason.Size = new System.Drawing.Size(230, 20);
            this.txtDiscountReason.TabIndex = 4;
            //
            // lblEligibility
            //
            this.lblEligibility.AutoSize = true;
            this.lblEligibility.Location = new System.Drawing.Point(5, 58);
            this.lblEligibility.Name = "lblEligibility";
            this.lblEligibility.Size = new System.Drawing.Size(203, 13);
            this.lblEligibility.TabIndex = 5;
            this.lblEligibility.Text = "Senior citizen / PWD (20%, VAT-exempt)";
            //
            // cmbEligibility
            // (items come from the DiscountEligibility enum at runtime)
            //
            this.cmbEligibility.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEligibility.Location = new System.Drawing.Point(5, 76);
            this.cmbEligibility.Name = "cmbEligibility";
            this.cmbEligibility.Size = new System.Drawing.Size(145, 21);
            this.cmbEligibility.TabIndex = 6;
            //
            // lblEligibilityId
            //
            this.lblEligibilityId.AutoSize = true;
            this.lblEligibilityId.Location = new System.Drawing.Point(160, 58);
            this.lblEligibilityId.Name = "lblEligibilityId";
            this.lblEligibilityId.Size = new System.Drawing.Size(101, 13);
            this.lblEligibilityId.TabIndex = 7;
            this.lblEligibilityId.Text = "OSCA / PWD ID No.";
            //
            // txtEligibilityId
            //
            this.txtEligibilityId.Location = new System.Drawing.Point(160, 76);
            this.txtEligibilityId.Name = "txtEligibilityId";
            this.txtEligibilityId.Size = new System.Drawing.Size(230, 20);
            this.txtEligibilityId.TabIndex = 8;
            //
            // lblEligibilityHint
            // Filled at runtime when the patient's age qualifies them (60+).
            //
            this.lblEligibilityHint.AutoSize = true;
            this.lblEligibilityHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.lblEligibilityHint.Location = new System.Drawing.Point(5, 104);
            this.lblEligibilityHint.Name = "lblEligibilityHint";
            this.lblEligibilityHint.Size = new System.Drawing.Size(250, 13);
            this.lblEligibilityHint.TabIndex = 9;
            this.lblEligibilityHint.Text = "(senior citizen hint, shown for patients aged 60+)";
            //
            // lblVatRate
            //
            this.lblVatRate.AutoSize = true;
            this.lblVatRate.Location = new System.Drawing.Point(5, 126);
            this.lblVatRate.Name = "lblVatRate";
            this.lblVatRate.Size = new System.Drawing.Size(69, 13);
            this.lblVatRate.TabIndex = 10;
            this.lblVatRate.Text = "VAT rate (%)";
            //
            // numVatRate
            //
            this.numVatRate.DecimalPlaces = 2;
            this.numVatRate.Location = new System.Drawing.Point(5, 144);
            this.numVatRate.Name = "numVatRate";
            this.numVatRate.Size = new System.Drawing.Size(85, 20);
            this.numVatRate.TabIndex = 11;
            //
            // lblVatNote
            //
            this.lblVatNote.AutoSize = true;
            this.lblVatNote.Location = new System.Drawing.Point(95, 147);
            this.lblVatNote.Name = "lblVatNote";
            this.lblVatNote.Size = new System.Drawing.Size(239, 13);
            this.lblVatNote.TabIndex = 12;
            this.lblVatNote.Text = "Waived automatically for senior citizens / PWD.";
            //
            // lblHmoProvider
            //
            this.lblHmoProvider.AutoSize = true;
            this.lblHmoProvider.Location = new System.Drawing.Point(5, 179);
            this.lblHmoProvider.Name = "lblHmoProvider";
            this.lblHmoProvider.Size = new System.Drawing.Size(74, 13);
            this.lblHmoProvider.TabIndex = 13;
            this.lblHmoProvider.Text = "HMO provider";
            //
            // cmbHmoProvider
            // Pick a common HMO or type another.
            //
            this.cmbHmoProvider.Items.AddRange(new object[] {
            "Maxicare",
            "Intellicare",
            "MediCard",
            "PhilCare",
            "Cocolife",
            "ValuCare",
            "Etiqa"});
            this.cmbHmoProvider.Location = new System.Drawing.Point(5, 197);
            this.cmbHmoProvider.Name = "cmbHmoProvider";
            this.cmbHmoProvider.Size = new System.Drawing.Size(145, 21);
            this.cmbHmoProvider.TabIndex = 14;
            //
            // lblHmoLoa
            //
            this.lblHmoLoa.AutoSize = true;
            this.lblHmoLoa.Location = new System.Drawing.Point(160, 179);
            this.lblHmoLoa.Name = "lblHmoLoa";
            this.lblHmoLoa.Size = new System.Drawing.Size(101, 13);
            this.lblHmoLoa.TabIndex = 15;
            this.lblHmoLoa.Text = "LOA / Approval No.";
            //
            // txtHmoLoa
            //
            this.txtHmoLoa.Location = new System.Drawing.Point(160, 197);
            this.txtHmoLoa.Name = "txtHmoLoa";
            this.txtHmoLoa.Size = new System.Drawing.Size(120, 20);
            this.txtHmoLoa.TabIndex = 16;
            //
            // lblHmoCoverage
            //
            this.lblHmoCoverage.AutoSize = true;
            this.lblHmoCoverage.Location = new System.Drawing.Point(290, 179);
            this.lblHmoCoverage.Name = "lblHmoCoverage";
            this.lblHmoCoverage.Size = new System.Drawing.Size(89, 13);
            this.lblHmoCoverage.TabIndex = 17;
            this.lblHmoCoverage.Text = "Coverage amount";
            //
            // numHmoCoverage
            //
            this.numHmoCoverage.DecimalPlaces = 2;
            this.numHmoCoverage.Location = new System.Drawing.Point(290, 197);
            this.numHmoCoverage.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numHmoCoverage.Name = "numHmoCoverage";
            this.numHmoCoverage.Size = new System.Drawing.Size(100, 20);
            this.numHmoCoverage.TabIndex = 18;
            this.numHmoCoverage.ThousandsSeparator = true;
            //
            // btnApplyAdjustments
            //
            this.btnApplyAdjustments.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnApplyAdjustments.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyAdjustments.ForeColor = System.Drawing.Color.White;
            this.btnApplyAdjustments.Location = new System.Drawing.Point(5, 240);
            this.btnApplyAdjustments.Name = "btnApplyAdjustments";
            this.btnApplyAdjustments.Size = new System.Drawing.Size(150, 30);
            this.btnApplyAdjustments.TabIndex = 19;
            this.btnApplyAdjustments.Text = "Apply Adjustments";
            this.btnApplyAdjustments.UseVisualStyleBackColor = false;
            this.btnApplyAdjustments.Click += new System.EventHandler(this.BtnApplyAdjustments_Click);
            //
            // tabPayments
            //
            this.tabPayments.Controls.Add(this.paymentsCard);
            this.tabPayments.Location = new System.Drawing.Point(4, 22);
            this.tabPayments.Name = "tabPayments";
            this.tabPayments.Size = new System.Drawing.Size(439, 255);
            this.tabPayments.TabIndex = 2;
            this.tabPayments.Text = "Payments";
            this.tabPayments.UseVisualStyleBackColor = true;
            //
            // paymentsCard
            // Top to bottom: header, balance summary, payment form (or the "admins only" note), payments grid.
            //
            this.paymentsCard.BackColor = System.Drawing.Color.White;
            this.paymentsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.paymentsCard.Controls.Add(this.gridPayments);
            this.paymentsCard.Controls.Add(this.lblPaymentsByAdmin);
            this.paymentsCard.Controls.Add(this.paymentEntry);
            this.paymentsCard.Controls.Add(this.balanceSummary);
            this.paymentsCard.Controls.Add(this.lblPayments);
            this.paymentsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paymentsCard.Location = new System.Drawing.Point(0, 0);
            this.paymentsCard.Name = "paymentsCard";
            this.paymentsCard.Padding = new System.Windows.Forms.Padding(10);
            this.paymentsCard.Size = new System.Drawing.Size(439, 255);
            this.paymentsCard.TabIndex = 0;
            //
            // lblPayments
            //
            this.lblPayments.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPayments.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPayments.Location = new System.Drawing.Point(10, 10);
            this.lblPayments.Name = "lblPayments";
            this.lblPayments.Size = new System.Drawing.Size(417, 28);
            this.lblPayments.TabIndex = 0;
            this.lblPayments.Text = "Payments";
            //
            // balanceSummary
            // Current balance of the selected bill, split between the patient and the HMO.
            //
            this.balanceSummary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(250)))), ((int)(((byte)(251)))));
            this.balanceSummary.Controls.Add(this.lblPatientBalance);
            this.balanceSummary.Controls.Add(this.lblHmoBalance);
            this.balanceSummary.Controls.Add(this.lblPatientOutstanding);
            this.balanceSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.balanceSummary.Location = new System.Drawing.Point(10, 38);
            this.balanceSummary.Name = "balanceSummary";
            this.balanceSummary.Size = new System.Drawing.Size(417, 62);
            this.balanceSummary.TabIndex = 1;
            //
            // lblPatientBalance
            //
            this.lblPatientBalance.AutoSize = true;
            this.lblPatientBalance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPatientBalance.Location = new System.Drawing.Point(5, 4);
            this.lblPatientBalance.Name = "lblPatientBalance";
            this.lblPatientBalance.Size = new System.Drawing.Size(98, 15);
            this.lblPatientBalance.TabIndex = 0;
            this.lblPatientBalance.Text = "Patient balance: -";
            //
            // lblHmoBalance
            //
            this.lblHmoBalance.AutoSize = true;
            this.lblHmoBalance.Location = new System.Drawing.Point(5, 23);
            this.lblHmoBalance.Name = "lblHmoBalance";
            this.lblHmoBalance.Size = new System.Drawing.Size(97, 13);
            this.lblHmoBalance.TabIndex = 1;
            this.lblHmoBalance.Text = "HMO outstanding: -";
            //
            // lblPatientOutstanding
            // Shown when the patient also owes on other bills.
            //
            this.lblPatientOutstanding.AutoSize = true;
            this.lblPatientOutstanding.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.lblPatientOutstanding.Location = new System.Drawing.Point(5, 42);
            this.lblPatientOutstanding.Name = "lblPatientOutstanding";
            this.lblPatientOutstanding.Size = new System.Drawing.Size(200, 13);
            this.lblPatientOutstanding.TabIndex = 2;
            this.lblPatientOutstanding.Text = "(patient total across all bills)";
            //
            // paymentEntry
            // Hidden for roles that can't record payments (lblPaymentsByAdmin shows instead).
            //
            this.paymentEntry.Controls.Add(this.lblPayAmount);
            this.paymentEntry.Controls.Add(this.numPayAmount);
            this.paymentEntry.Controls.Add(this.lblMethod);
            this.paymentEntry.Controls.Add(this.cmbMethod);
            this.paymentEntry.Controls.Add(this.lblReference);
            this.paymentEntry.Controls.Add(this.txtReference);
            this.paymentEntry.Controls.Add(this.pnlCash);
            this.paymentEntry.Controls.Add(this.pnlCard);
            this.paymentEntry.Controls.Add(this.pnlHmo);
            this.paymentEntry.Controls.Add(this.btnPay);
            this.paymentEntry.Dock = System.Windows.Forms.DockStyle.Top;
            this.paymentEntry.Location = new System.Drawing.Point(10, 100);
            this.paymentEntry.Name = "paymentEntry";
            this.paymentEntry.Size = new System.Drawing.Size(417, 140);
            this.paymentEntry.TabIndex = 2;
            //
            // lblPayAmount
            //
            this.lblPayAmount.AutoSize = true;
            this.lblPayAmount.Location = new System.Drawing.Point(5, 5);
            this.lblPayAmount.Name = "lblPayAmount";
            this.lblPayAmount.Size = new System.Drawing.Size(43, 13);
            this.lblPayAmount.TabIndex = 0;
            this.lblPayAmount.Text = "Amount";
            //
            // numPayAmount
            //
            this.numPayAmount.DecimalPlaces = 2;
            this.numPayAmount.Location = new System.Drawing.Point(5, 23);
            this.numPayAmount.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numPayAmount.Name = "numPayAmount";
            this.numPayAmount.Size = new System.Drawing.Size(110, 20);
            this.numPayAmount.TabIndex = 1;
            this.numPayAmount.ThousandsSeparator = true;
            this.numPayAmount.ValueChanged += new System.EventHandler(this.PaymentAmount_ValueChanged);
            //
            // lblMethod
            //
            this.lblMethod.AutoSize = true;
            this.lblMethod.Location = new System.Drawing.Point(120, 5);
            this.lblMethod.Name = "lblMethod";
            this.lblMethod.Size = new System.Drawing.Size(43, 13);
            this.lblMethod.TabIndex = 2;
            this.lblMethod.Text = "Method";
            //
            // cmbMethod
            // (items come from the PaymentMethod enum at runtime)
            //
            this.cmbMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMethod.Location = new System.Drawing.Point(120, 23);
            this.cmbMethod.Name = "cmbMethod";
            this.cmbMethod.Size = new System.Drawing.Size(110, 21);
            this.cmbMethod.TabIndex = 3;
            this.cmbMethod.SelectedIndexChanged += new System.EventHandler(this.CmbMethod_SelectedIndexChanged);
            //
            // lblReference
            //
            this.lblReference.AutoSize = true;
            this.lblReference.Location = new System.Drawing.Point(235, 5);
            this.lblReference.Name = "lblReference";
            this.lblReference.Size = new System.Drawing.Size(101, 13);
            this.lblReference.TabIndex = 4;
            this.lblReference.Text = "Reference / OR No.";
            //
            // txtReference
            //
            this.txtReference.Location = new System.Drawing.Point(235, 23);
            this.txtReference.Name = "txtReference";
            this.txtReference.Size = new System.Drawing.Size(150, 20);
            this.txtReference.TabIndex = 5;
            //
            // pnlCash
            // Cash details (shown when the method is Cash).
            //
            this.pnlCash.Controls.Add(this.lblTendered);
            this.pnlCash.Controls.Add(this.numTendered);
            this.pnlCash.Controls.Add(this.lblChange);
            this.pnlCash.Location = new System.Drawing.Point(5, 55);
            this.pnlCash.Name = "pnlCash";
            this.pnlCash.Size = new System.Drawing.Size(390, 46);
            this.pnlCash.TabIndex = 6;
            //
            // lblTendered
            //
            this.lblTendered.AutoSize = true;
            this.lblTendered.Location = new System.Drawing.Point(0, 0);
            this.lblTendered.Name = "lblTendered";
            this.lblTendered.Size = new System.Drawing.Size(75, 13);
            this.lblTendered.TabIndex = 0;
            this.lblTendered.Text = "Cash tendered";
            //
            // numTendered
            //
            this.numTendered.DecimalPlaces = 2;
            this.numTendered.Location = new System.Drawing.Point(0, 18);
            this.numTendered.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numTendered.Name = "numTendered";
            this.numTendered.Size = new System.Drawing.Size(110, 20);
            this.numTendered.TabIndex = 1;
            this.numTendered.ThousandsSeparator = true;
            this.numTendered.ValueChanged += new System.EventHandler(this.PaymentAmount_ValueChanged);
            //
            // lblChange
            //
            this.lblChange.AutoSize = true;
            this.lblChange.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblChange.Location = new System.Drawing.Point(120, 21);
            this.lblChange.Name = "lblChange";
            this.lblChange.Size = new System.Drawing.Size(81, 15);
            this.lblChange.TabIndex = 2;
            this.lblChange.Text = "Change: 0.00";
            //
            // pnlCard
            // Card details (shown when the method is Card; only the last 4 digits are kept).
            //
            this.pnlCard.Controls.Add(this.lblCardType);
            this.pnlCard.Controls.Add(this.cmbCardType);
            this.pnlCard.Controls.Add(this.lblCardLast4);
            this.pnlCard.Controls.Add(this.txtCardLast4);
            this.pnlCard.Controls.Add(this.lblApproval);
            this.pnlCard.Controls.Add(this.txtApprovalCode);
            this.pnlCard.Location = new System.Drawing.Point(5, 55);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Size = new System.Drawing.Size(390, 46);
            this.pnlCard.TabIndex = 7;
            //
            // lblCardType
            //
            this.lblCardType.AutoSize = true;
            this.lblCardType.Location = new System.Drawing.Point(0, 0);
            this.lblCardType.Name = "lblCardType";
            this.lblCardType.Size = new System.Drawing.Size(53, 13);
            this.lblCardType.TabIndex = 0;
            this.lblCardType.Text = "Card type";
            //
            // cmbCardType
            //
            this.cmbCardType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCardType.Items.AddRange(new object[] {
            "Visa",
            "Mastercard",
            "JCB",
            "Amex",
            "Debit card"});
            this.cmbCardType.Location = new System.Drawing.Point(0, 18);
            this.cmbCardType.Name = "cmbCardType";
            this.cmbCardType.Size = new System.Drawing.Size(110, 21);
            this.cmbCardType.TabIndex = 1;
            //
            // lblCardLast4
            //
            this.lblCardLast4.AutoSize = true;
            this.lblCardLast4.Location = new System.Drawing.Point(115, 0);
            this.lblCardLast4.Name = "lblCardLast4";
            this.lblCardLast4.Size = new System.Drawing.Size(67, 13);
            this.lblCardLast4.TabIndex = 2;
            this.lblCardLast4.Text = "Last 4 digits";
            //
            // txtCardLast4
            //
            this.txtCardLast4.Location = new System.Drawing.Point(115, 18);
            this.txtCardLast4.MaxLength = 4;
            this.txtCardLast4.Name = "txtCardLast4";
            this.txtCardLast4.Size = new System.Drawing.Size(75, 20);
            this.txtCardLast4.TabIndex = 3;
            this.txtCardLast4.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtCardLast4_KeyPress);
            //
            // lblApproval
            //
            this.lblApproval.AutoSize = true;
            this.lblApproval.Location = new System.Drawing.Point(195, 0);
            this.lblApproval.Name = "lblApproval";
            this.lblApproval.Size = new System.Drawing.Size(76, 13);
            this.lblApproval.TabIndex = 4;
            this.lblApproval.Text = "Approval code";
            //
            // txtApprovalCode
            //
            this.txtApprovalCode.Location = new System.Drawing.Point(195, 18);
            this.txtApprovalCode.Name = "txtApprovalCode";
            this.txtApprovalCode.Size = new System.Drawing.Size(110, 20);
            this.txtApprovalCode.TabIndex = 5;
            //
            // pnlHmo
            // HMO settlement details (shown when the method is HMO).
            //
            this.pnlHmo.Controls.Add(this.lblPayHmoProvider);
            this.pnlHmo.Controls.Add(this.txtPayHmoProvider);
            this.pnlHmo.Controls.Add(this.lblPayHmoLoa);
            this.pnlHmo.Controls.Add(this.txtPayHmoLoa);
            this.pnlHmo.Location = new System.Drawing.Point(5, 55);
            this.pnlHmo.Name = "pnlHmo";
            this.pnlHmo.Size = new System.Drawing.Size(390, 46);
            this.pnlHmo.TabIndex = 8;
            //
            // lblPayHmoProvider
            //
            this.lblPayHmoProvider.AutoSize = true;
            this.lblPayHmoProvider.Location = new System.Drawing.Point(0, 0);
            this.lblPayHmoProvider.Name = "lblPayHmoProvider";
            this.lblPayHmoProvider.Size = new System.Drawing.Size(74, 13);
            this.lblPayHmoProvider.TabIndex = 0;
            this.lblPayHmoProvider.Text = "HMO provider";
            //
            // txtPayHmoProvider
            //
            this.txtPayHmoProvider.Location = new System.Drawing.Point(0, 18);
            this.txtPayHmoProvider.Name = "txtPayHmoProvider";
            this.txtPayHmoProvider.Size = new System.Drawing.Size(150, 20);
            this.txtPayHmoProvider.TabIndex = 1;
            //
            // lblPayHmoLoa
            //
            this.lblPayHmoLoa.AutoSize = true;
            this.lblPayHmoLoa.Location = new System.Drawing.Point(155, 0);
            this.lblPayHmoLoa.Name = "lblPayHmoLoa";
            this.lblPayHmoLoa.Size = new System.Drawing.Size(101, 13);
            this.lblPayHmoLoa.TabIndex = 2;
            this.lblPayHmoLoa.Text = "LOA / Approval No.";
            //
            // txtPayHmoLoa
            //
            this.txtPayHmoLoa.Location = new System.Drawing.Point(155, 18);
            this.txtPayHmoLoa.Name = "txtPayHmoLoa";
            this.txtPayHmoLoa.Size = new System.Drawing.Size(150, 20);
            this.txtPayHmoLoa.TabIndex = 3;
            //
            // btnPay
            //
            this.btnPay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(150)))), ((int)(((byte)(105)))));
            this.btnPay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPay.ForeColor = System.Drawing.Color.White;
            this.btnPay.Location = new System.Drawing.Point(5, 104);
            this.btnPay.Name = "btnPay";
            this.btnPay.Size = new System.Drawing.Size(130, 30);
            this.btnPay.TabIndex = 9;
            this.btnPay.Text = "Record Payment";
            this.btnPay.UseVisualStyleBackColor = false;
            this.btnPay.Click += new System.EventHandler(this.BtnPay_Click);
            //
            // lblPaymentsByAdmin
            // Shown instead of the payment form for roles that can't record payments (nurses).
            //
            this.lblPaymentsByAdmin.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPaymentsByAdmin.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblPaymentsByAdmin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblPaymentsByAdmin.Location = new System.Drawing.Point(10, 240);
            this.lblPaymentsByAdmin.Name = "lblPaymentsByAdmin";
            this.lblPaymentsByAdmin.Size = new System.Drawing.Size(417, 30);
            this.lblPaymentsByAdmin.TabIndex = 3;
            this.lblPaymentsByAdmin.Text = "Payments are recorded by an administrator.";
            this.lblPaymentsByAdmin.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPaymentsByAdmin.Visible = false;
            //
            // gridPayments
            //
            this.gridPayments.AllowUserToAddRows = false;
            this.gridPayments.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridPayments.BackgroundColor = System.Drawing.Color.White;
            this.gridPayments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPayments.Location = new System.Drawing.Point(10, 270);
            this.gridPayments.MultiSelect = false;
            this.gridPayments.Name = "gridPayments";
            this.gridPayments.ReadOnly = true;
            this.gridPayments.RowHeadersVisible = false;
            this.gridPayments.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPayments.Size = new System.Drawing.Size(417, 0);
            this.gridPayments.TabIndex = 4;
            //
            // BillingView
            // Fill first, then bottom-to-top: the last one added docks at the very top.
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.detail);
            this.Controls.Add(this.gridBills);
            this.Controls.Add(this.btnPanel);
            this.Controls.Add(this.lblBills);
            this.Controls.Add(this.createPanel);
            this.Controls.Add(this.lblNurseNotice);
            this.Name = "BillingView";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(1024, 760);
            this.createPanel.ResumeLayout(false);
            this.createPanel.PerformLayout();
            this.btnPanel.ResumeLayout(false);
            this.btnPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridBills)).EndInit();
            this.detail.ResumeLayout(false);
            this.itemsCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridItems)).EndInit();
            this.itemEntry.ResumeLayout(false);
            this.itemEntry.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQty)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUnitPrice)).EndInit();
            this.tabsSummary.ResumeLayout(false);
            this.tabBreakdown.ResumeLayout(false);
            this.breakdownCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridBreakdown)).EndInit();
            this.breakdownBottom.ResumeLayout(false);
            this.adjustmentsTab.ResumeLayout(false);
            this.adjustmentsCard.ResumeLayout(false);
            this.adjustmentsCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDiscount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numVatRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHmoCoverage)).EndInit();
            this.tabPayments.ResumeLayout(false);
            this.paymentsCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPayments)).EndInit();
            this.paymentEntry.ResumeLayout(false);
            this.paymentEntry.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPayAmount)).EndInit();
            this.pnlCash.ResumeLayout(false);
            this.pnlCash.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTendered)).EndInit();
            this.pnlCard.ResumeLayout(false);
            this.pnlCard.PerformLayout();
            this.pnlHmo.ResumeLayout(false);
            this.pnlHmo.PerformLayout();
            this.balanceSummary.ResumeLayout(false);
            this.balanceSummary.PerformLayout();
            this.ResumeLayout(false);

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
            // Hospital-wide totals are for administrators; everyone sees each bill's own balance.
            lblBills.Text = !Permissions.Can(Permission.ViewFinancials) ? "Bills" :
                "Bills    Outstanding - patients: " + Money(patientsOwe) + "   HMO: " + Money(hmosOwe) +
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
                    ". Add items to that bill instead.", "Already Billed",
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

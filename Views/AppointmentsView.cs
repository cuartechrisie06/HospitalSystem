using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    public class AppointmentsView : UserControl
    {
        private const string PatientSearchPlaceholder = "Search active patients by ID, name, or contact...";

        // Layout
        private TableLayoutPanel root;
        private Panel scheduleCard;
        private Label lblScheduleTitle;
        private TableLayoutPanel scheduleLayout;
        private Panel patientPicker;
        private Label lblPickPatient;
        private Panel searchSpacer;
        private Panel fieldsPanel;
        private Label lblDepartment;
        private Label lblDoctor;
        private Label lblDateTime;
        private Label lblReason;
        private Label lblList;
        private Panel actionBar;

        // Scheduling form
        private DataGridView gridPatients;
        private TextBox txtPatientSearch;
        private Label lblSelectedPatient;
        private ComboBox cmbDepartment;
        private ComboBox cmbDoctor;
        private DateTimePicker dtpDate;
        private TextBox txtReason;
        private Button btnSchedule;

        // Appointments list + actions
        private DataGridView grid;
        private Button btnConfirm;
        private Button btnCancel;
        private Button btnReschedule;
        private Button btnComplete;

        private int selectedPatientId = 0;
        private bool patientSearchPlaceholderActive = true;

        public AppointmentsView()
        {
            // Every control and event is set up in InitializeComponent() (Designer format),
            // so the Designer shows the complete screen.
            InitializeComponent();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                dtpDate.MinDate = DateTime.Today;   // can't be expressed in designer code
                LoadDepartments();
                LoadDoctors();
                LoadPatientList("");
                LoadAppointments();
            }
        }

        // Designer-generated layout. root is a 4-row table, top to bottom: the Schedule
        // Appointment card (patient picker on the left, fields on the right), the list title,
        // the action buttons, and the appointments grid.
        private void InitializeComponent()
        {
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.scheduleCard = new System.Windows.Forms.Panel();
            this.scheduleLayout = new System.Windows.Forms.TableLayoutPanel();
            this.patientPicker = new System.Windows.Forms.Panel();
            this.gridPatients = new System.Windows.Forms.DataGridView();
            this.searchSpacer = new System.Windows.Forms.Panel();
            this.txtPatientSearch = new System.Windows.Forms.TextBox();
            this.lblPickPatient = new System.Windows.Forms.Label();
            this.fieldsPanel = new System.Windows.Forms.Panel();
            this.lblSelectedPatient = new System.Windows.Forms.Label();
            this.lblDepartment = new System.Windows.Forms.Label();
            this.cmbDepartment = new System.Windows.Forms.ComboBox();
            this.lblDoctor = new System.Windows.Forms.Label();
            this.cmbDoctor = new System.Windows.Forms.ComboBox();
            this.lblDateTime = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblReason = new System.Windows.Forms.Label();
            this.txtReason = new System.Windows.Forms.TextBox();
            this.btnSchedule = new System.Windows.Forms.Button();
            this.lblScheduleTitle = new System.Windows.Forms.Label();
            this.lblList = new System.Windows.Forms.Label();
            this.actionBar = new System.Windows.Forms.Panel();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnReschedule = new System.Windows.Forms.Button();
            this.btnComplete = new System.Windows.Forms.Button();
            this.grid = new System.Windows.Forms.DataGridView();
            this.root.SuspendLayout();
            this.scheduleCard.SuspendLayout();
            this.scheduleLayout.SuspendLayout();
            this.patientPicker.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPatients)).BeginInit();
            this.fieldsPanel.SuspendLayout();
            this.actionBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            //
            // root
            //
            this.root.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.root.ColumnCount = 1;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.scheduleCard, 0, 0);
            this.root.Controls.Add(this.lblList, 0, 1);
            this.root.Controls.Add(this.actionBar, 0, 2);
            this.root.Controls.Add(this.grid, 0, 3);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(10, 10);
            this.root.Name = "root";
            this.root.RowCount = 4;
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 372F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Size = new System.Drawing.Size(1004, 740);
            this.root.TabIndex = 0;
            //
            // scheduleCard
            //
            this.scheduleCard.BackColor = System.Drawing.Color.White;
            this.scheduleCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.scheduleCard.Controls.Add(this.scheduleLayout);
            this.scheduleCard.Controls.Add(this.lblScheduleTitle);
            this.scheduleCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scheduleCard.Location = new System.Drawing.Point(0, 0);
            this.scheduleCard.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.scheduleCard.Name = "scheduleCard";
            this.scheduleCard.Padding = new System.Windows.Forms.Padding(12);
            this.scheduleCard.Size = new System.Drawing.Size(1004, 364);
            this.scheduleCard.TabIndex = 0;
            //
            // lblScheduleTitle
            //
            this.lblScheduleTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblScheduleTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblScheduleTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblScheduleTitle.Location = new System.Drawing.Point(12, 12);
            this.lblScheduleTitle.Name = "lblScheduleTitle";
            this.lblScheduleTitle.Size = new System.Drawing.Size(978, 30);
            this.lblScheduleTitle.TabIndex = 0;
            this.lblScheduleTitle.Text = "Schedule Appointment";
            //
            // scheduleLayout
            //
            this.scheduleLayout.ColumnCount = 2;
            this.scheduleLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this.scheduleLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this.scheduleLayout.Controls.Add(this.patientPicker, 0, 0);
            this.scheduleLayout.Controls.Add(this.fieldsPanel, 1, 0);
            this.scheduleLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scheduleLayout.Location = new System.Drawing.Point(12, 42);
            this.scheduleLayout.Name = "scheduleLayout";
            this.scheduleLayout.RowCount = 1;
            this.scheduleLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.scheduleLayout.Size = new System.Drawing.Size(978, 308);
            this.scheduleLayout.TabIndex = 1;
            //
            // patientPicker
            // Left column: searchable table of ACTIVE patients.
            //
            this.patientPicker.Controls.Add(this.gridPatients);
            this.patientPicker.Controls.Add(this.searchSpacer);
            this.patientPicker.Controls.Add(this.txtPatientSearch);
            this.patientPicker.Controls.Add(this.lblPickPatient);
            this.patientPicker.Dock = System.Windows.Forms.DockStyle.Fill;
            this.patientPicker.Location = new System.Drawing.Point(3, 3);
            this.patientPicker.Name = "patientPicker";
            this.patientPicker.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.patientPicker.Size = new System.Drawing.Size(443, 302);
            this.patientPicker.TabIndex = 0;
            //
            // lblPickPatient
            //
            this.lblPickPatient.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPickPatient.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblPickPatient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblPickPatient.Location = new System.Drawing.Point(0, 0);
            this.lblPickPatient.Name = "lblPickPatient";
            this.lblPickPatient.Size = new System.Drawing.Size(433, 22);
            this.lblPickPatient.TabIndex = 0;
            this.lblPickPatient.Text = "1. Search && select an active patient";
            //
            // txtPatientSearch
            //
            this.txtPatientSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPatientSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtPatientSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtPatientSearch.Location = new System.Drawing.Point(0, 22);
            this.txtPatientSearch.Name = "txtPatientSearch";
            this.txtPatientSearch.Size = new System.Drawing.Size(433, 25);
            this.txtPatientSearch.TabIndex = 1;
            this.txtPatientSearch.Text = "Search active patients by ID, name, or contact...";
            this.txtPatientSearch.TextChanged += new System.EventHandler(this.TxtPatientSearch_TextChanged);
            this.txtPatientSearch.Enter += new System.EventHandler(this.TxtPatientSearch_Enter);
            this.txtPatientSearch.Leave += new System.EventHandler(this.TxtPatientSearch_Leave);
            //
            // searchSpacer
            // A little breathing room under the search box.
            //
            this.searchSpacer.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchSpacer.Location = new System.Drawing.Point(0, 47);
            this.searchSpacer.Name = "searchSpacer";
            this.searchSpacer.Size = new System.Drawing.Size(433, 6);
            this.searchSpacer.TabIndex = 2;
            //
            // gridPatients
            //
            this.gridPatients.AllowUserToAddRows = false;
            this.gridPatients.AllowUserToResizeRows = false;
            this.gridPatients.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridPatients.BackgroundColor = System.Drawing.Color.White;
            this.gridPatients.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPatients.Location = new System.Drawing.Point(0, 53);
            this.gridPatients.MultiSelect = false;
            this.gridPatients.Name = "gridPatients";
            this.gridPatients.ReadOnly = true;
            this.gridPatients.RowHeadersVisible = false;
            this.gridPatients.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPatients.Size = new System.Drawing.Size(433, 249);
            this.gridPatients.TabIndex = 3;
            this.gridPatients.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GridPatients_CellClick);
            //
            // fieldsPanel
            // Right column: the remaining appointment fields.
            //
            this.fieldsPanel.Controls.Add(this.lblSelectedPatient);
            this.fieldsPanel.Controls.Add(this.lblDepartment);
            this.fieldsPanel.Controls.Add(this.cmbDepartment);
            this.fieldsPanel.Controls.Add(this.lblDoctor);
            this.fieldsPanel.Controls.Add(this.cmbDoctor);
            this.fieldsPanel.Controls.Add(this.lblDateTime);
            this.fieldsPanel.Controls.Add(this.dtpDate);
            this.fieldsPanel.Controls.Add(this.lblReason);
            this.fieldsPanel.Controls.Add(this.txtReason);
            this.fieldsPanel.Controls.Add(this.btnSchedule);
            this.fieldsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fieldsPanel.Location = new System.Drawing.Point(452, 3);
            this.fieldsPanel.Name = "fieldsPanel";
            this.fieldsPanel.Size = new System.Drawing.Size(523, 302);
            this.fieldsPanel.TabIndex = 1;
            //
            // lblSelectedPatient
            //
            this.lblSelectedPatient.AutoSize = true;
            this.lblSelectedPatient.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSelectedPatient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.lblSelectedPatient.Location = new System.Drawing.Point(0, 4);
            this.lblSelectedPatient.MaximumSize = new System.Drawing.Size(460, 0);
            this.lblSelectedPatient.Name = "lblSelectedPatient";
            this.lblSelectedPatient.Size = new System.Drawing.Size(380, 17);
            this.lblSelectedPatient.TabIndex = 0;
            this.lblSelectedPatient.Text = "Selected patient:  (none — pick one from the list on the left)";
            //
            // lblDepartment
            //
            this.lblDepartment.AutoSize = true;
            this.lblDepartment.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDepartment.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.lblDepartment.Location = new System.Drawing.Point(0, 40);
            this.lblDepartment.Name = "lblDepartment";
            this.lblDepartment.Size = new System.Drawing.Size(80, 15);
            this.lblDepartment.TabIndex = 1;
            this.lblDepartment.Text = "Department *";
            //
            // cmbDepartment
            //
            this.cmbDepartment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDepartment.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbDepartment.Location = new System.Drawing.Point(0, 62);
            this.cmbDepartment.Name = "cmbDepartment";
            this.cmbDepartment.Size = new System.Drawing.Size(300, 25);
            this.cmbDepartment.TabIndex = 2;
            //
            // lblDoctor
            //
            this.lblDoctor.AutoSize = true;
            this.lblDoctor.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDoctor.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.lblDoctor.Location = new System.Drawing.Point(0, 96);
            this.lblDoctor.Name = "lblDoctor";
            this.lblDoctor.Size = new System.Drawing.Size(130, 15);
            this.lblDoctor.TabIndex = 3;
            this.lblDoctor.Text = "Doctor *  (active only)";
            //
            // cmbDoctor
            //
            this.cmbDoctor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDoctor.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbDoctor.Location = new System.Drawing.Point(0, 118);
            this.cmbDoctor.Name = "cmbDoctor";
            this.cmbDoctor.Size = new System.Drawing.Size(300, 25);
            this.cmbDoctor.TabIndex = 4;
            //
            // lblDateTime
            //
            this.lblDateTime.AutoSize = true;
            this.lblDateTime.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDateTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.lblDateTime.Location = new System.Drawing.Point(0, 152);
            this.lblDateTime.Name = "lblDateTime";
            this.lblDateTime.Size = new System.Drawing.Size(80, 15);
            this.lblDateTime.TabIndex = 5;
            this.lblDateTime.Text = "Date && Time *";
            //
            // dtpDate
            // (MinDate = today is set at runtime in the constructor)
            //
            this.dtpDate.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpDate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDate.Location = new System.Drawing.Point(0, 174);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(300, 25);
            this.dtpDate.TabIndex = 6;
            //
            // lblReason
            //
            this.lblReason.AutoSize = true;
            this.lblReason.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReason.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.lblReason.Location = new System.Drawing.Point(0, 208);
            this.lblReason.Name = "lblReason";
            this.lblReason.Size = new System.Drawing.Size(45, 15);
            this.lblReason.TabIndex = 7;
            this.lblReason.Text = "Reason";
            //
            // txtReason
            //
            this.txtReason.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtReason.Location = new System.Drawing.Point(0, 230);
            this.txtReason.Name = "txtReason";
            this.txtReason.Size = new System.Drawing.Size(360, 25);
            this.txtReason.TabIndex = 8;
            //
            // btnSchedule
            //
            this.btnSchedule.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnSchedule.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSchedule.FlatAppearance.BorderSize = 0;
            this.btnSchedule.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSchedule.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSchedule.ForeColor = System.Drawing.Color.White;
            this.btnSchedule.Location = new System.Drawing.Point(0, 268);
            this.btnSchedule.Name = "btnSchedule";
            this.btnSchedule.Size = new System.Drawing.Size(200, 36);
            this.btnSchedule.TabIndex = 9;
            this.btnSchedule.Text = "Schedule Appointment";
            this.btnSchedule.UseVisualStyleBackColor = false;
            this.btnSchedule.Click += new System.EventHandler(this.BtnSchedule_Click);
            //
            // lblList
            //
            this.lblList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblList.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblList.Location = new System.Drawing.Point(3, 372);
            this.lblList.Name = "lblList";
            this.lblList.Size = new System.Drawing.Size(998, 34);
            this.lblList.TabIndex = 1;
            this.lblList.Text = "Appointments";
            this.lblList.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // actionBar
            //
            this.actionBar.Controls.Add(this.btnConfirm);
            this.actionBar.Controls.Add(this.btnCancel);
            this.actionBar.Controls.Add(this.btnReschedule);
            this.actionBar.Controls.Add(this.btnComplete);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBar.Location = new System.Drawing.Point(3, 409);
            this.actionBar.Name = "actionBar";
            this.actionBar.Size = new System.Drawing.Size(998, 38);
            this.actionBar.TabIndex = 2;
            //
            // btnConfirm
            //
            this.btnConfirm.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnConfirm.Location = new System.Drawing.Point(0, 6);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(140, 30);
            this.btnConfirm.TabIndex = 0;
            this.btnConfirm.Text = "Confirm Selected";
            this.btnConfirm.UseVisualStyleBackColor = true;
            this.btnConfirm.Click += new System.EventHandler(this.BtnConfirm_Click);
            //
            // btnCancel
            //
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Location = new System.Drawing.Point(150, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(140, 30);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel Selected";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // btnReschedule
            //
            this.btnReschedule.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReschedule.Location = new System.Drawing.Point(300, 6);
            this.btnReschedule.Name = "btnReschedule";
            this.btnReschedule.Size = new System.Drawing.Size(150, 30);
            this.btnReschedule.TabIndex = 2;
            this.btnReschedule.Text = "Reschedule Selected";
            this.btnReschedule.UseVisualStyleBackColor = true;
            this.btnReschedule.Click += new System.EventHandler(this.BtnReschedule_Click);
            //
            // btnComplete
            //
            this.btnComplete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnComplete.Location = new System.Drawing.Point(460, 6);
            this.btnComplete.Name = "btnComplete";
            this.btnComplete.Size = new System.Drawing.Size(140, 30);
            this.btnComplete.TabIndex = 3;
            this.btnComplete.Text = "Mark Completed";
            this.btnComplete.UseVisualStyleBackColor = true;
            this.btnComplete.Click += new System.EventHandler(this.BtnComplete_Click);
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersHeight = 34;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(3, 453);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(998, 284);
            this.grid.TabIndex = 3;
            //
            // AppointmentsView
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.root);
            this.Name = "AppointmentsView";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(1024, 760);
            this.root.ResumeLayout(false);
            this.scheduleCard.ResumeLayout(false);
            this.scheduleLayout.ResumeLayout(false);
            this.patientPicker.ResumeLayout(false);
            this.patientPicker.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPatients)).EndInit();
            this.fieldsPanel.ResumeLayout(false);
            this.fieldsPanel.PerformLayout();
            this.actionBar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        // ===================== Combo / list loading =====================
        private void LoadDepartments()
        {
            cmbDepartment.DisplayMember = "Name";
            cmbDepartment.ValueMember = "Id";
            cmbDepartment.DataSource = HospitalData.Departments.ToList();
            cmbDepartment.SelectedIndex = cmbDepartment.Items.Count > 0 ? 0 : -1;
        }

        // Only active doctors can be scheduled (deactivated doctors never appear here).
        private void LoadDoctors()
        {
            cmbDoctor.DisplayMember = "Name";
            cmbDoctor.ValueMember = "Id";
            cmbDoctor.DataSource = HospitalData.ActiveDoctors();
            cmbDoctor.SelectedIndex = cmbDoctor.Items.Count > 0 ? 0 : -1;
        }

        // Only active patients are listed, so a deactivated patient can never be scheduled.
        private void LoadPatientList(string filter)
        {
            var active = HospitalData.ActivePatients();
            var source = active.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                string f = filter.ToLower();
                source = source.Where(p =>
                    p.FullName.ToLower().Contains(f) ||
                    p.PatientNo.ToLower().Contains(f) ||
                    (p.Contact != null && p.Contact.Contains(f)));
            }

            var rows = source
                .OrderBy(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    No = p.PatientNo,
                    Name = p.FullName,
                    Age = p.Age.HasValue ? p.Age.Value.ToString() : "-",
                    p.Gender,
                    p.Contact
                }).ToList();

            gridPatients.DataSource = null;
            gridPatients.DataSource = rows;
            if (gridPatients.Columns["Id"] != null)
                gridPatients.Columns["Id"].Visible = false;
            if (gridPatients.Rows.Count > 0)
                gridPatients.FirstDisplayedScrollingRowIndex = 0;
            gridPatients.ClearSelection();
            if (gridPatients.CurrentCell != null)
                gridPatients.CurrentCell = null;
        }

        private void LoadAppointments()
        {
            grid.DataSource = HospitalData.Appointments
                .OrderByDescending(a => a.ScheduledOn)
                .Select(a => new
                {
                    a.Id,
                    a.AppointmentNo,
                    Patient = HospitalData.PatientName(a.PatientId),
                    Doctor = HospitalData.DoctorName(a.DoctorId),
                    Department = HospitalData.DepartmentName(a.DepartmentId),
                    Date = a.ScheduledOn.ToString("yyyy-MM-dd HH:mm"),
                    a.Reason,
                    a.Status
                }).ToList();

            if (grid.Columns["Id"] != null)
                grid.Columns["Id"].Visible = false;
        }

        // ===================== Patient search box (placeholder + live filter) =====================
        private void TxtPatientSearch_Enter(object sender, EventArgs e)
        {
            if (patientSearchPlaceholderActive)
            {
                txtPatientSearch.Text = "";
                txtPatientSearch.ForeColor = Color.FromArgb(17, 24, 39);
                patientSearchPlaceholderActive = false;
            }
        }

        private void TxtPatientSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPatientSearch.Text))
            {
                patientSearchPlaceholderActive = true;
                txtPatientSearch.Text = PatientSearchPlaceholder;
                txtPatientSearch.ForeColor = Color.Gray;
            }
        }

        private void TxtPatientSearch_TextChanged(object sender, EventArgs e)
        {
            if (patientSearchPlaceholderActive) return;
            LoadPatientList(txtPatientSearch.Text.Trim());
        }

        private void GridPatients_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var idObj = gridPatients.Rows[e.RowIndex].Cells["Id"].Value;
            if (idObj == null) return;

            selectedPatientId = Convert.ToInt32(idObj);
            var p = HospitalData.GetPatient(selectedPatientId);
            if (p != null)
            {
                lblSelectedPatient.Text = "Selected patient:  " + p.FullName + "  (" + p.PatientNo + ")";
                lblSelectedPatient.ForeColor = Color.FromArgb(5, 150, 105);
            }
        }

        // ===================== Actions =====================
        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            // Patient: must be chosen from the list and still active.
            if (selectedPatientId == 0)
            {
                MessageBox.Show("Please select a patient from the list on the left.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var patient = HospitalData.GetPatient(selectedPatientId);
            if (patient == null || !patient.IsActive)
            {
                MessageBox.Show("The selected patient is not active and cannot be scheduled. Please choose an active patient.",
                    "Inactive Patient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetSelectedPatient();
                LoadPatientList(patientSearchPlaceholderActive ? "" : txtPatientSearch.Text.Trim());
                return;
            }

            if (cmbDepartment.SelectedValue == null || cmbDoctor.SelectedValue == null)
            {
                MessageBox.Show("Please select a Department and a Doctor.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Doctor: must still be active (guards against deactivation after the list loaded).
            int doctorId = Convert.ToInt32(cmbDoctor.SelectedValue);
            var doctor = HospitalData.GetDoctor(doctorId);
            if (doctor == null || !doctor.IsActive)
            {
                MessageBox.Show("The selected doctor is no longer active. The doctor list has been refreshed; please pick an active doctor.",
                    "Inactive Doctor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadDoctors();
                return;
            }

            DateTime when = dtpDate.Value;
            if (HospitalData.HasAppointmentClash(doctorId, when))
            {
                MessageBox.Show("This doctor already has an appointment around that time.", "Conflict",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HospitalData.AddAppointment(new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctorId,
                DepartmentId = Convert.ToInt32(cmbDepartment.SelectedValue),
                ScheduledOn = when,
                Reason = txtReason.Text.Trim(),
                Status = "Pending"
            });

            MessageBox.Show("Appointment scheduled (Pending).", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadAppointments();
            txtReason.Clear();
            ResetSelectedPatient();
        }

        private void ResetSelectedPatient()
        {
            selectedPatientId = 0;
            lblSelectedPatient.Text = "Selected patient:  (none — pick one from the list on the left)";
            lblSelectedPatient.ForeColor = Color.FromArgb(185, 28, 28);
        }

        private Appointment GetSelectedAppointment()
        {
            if (grid.CurrentRow == null) return null;
            int id = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
            return HospitalData.Appointments.FirstOrDefault(a => a.Id == id);
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            var appt = GetSelectedAppointment();
            if (appt == null) return;

            if (appt.Status != "Pending")
            {
                MessageBox.Show("Only Pending appointments can be confirmed.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HospitalData.UpdateAppointmentStatus(appt, "Confirmed");
            MessageBox.Show("Appointment confirmed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadAppointments();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            var appt = GetSelectedAppointment();
            if (appt == null) return;

            if (!appt.IsOpen)
            {
                MessageBox.Show("Only Pending or Confirmed appointments can be cancelled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Cancel this appointment?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                HospitalData.CancelAppointment(appt);
                LoadAppointments();
            }
        }

        private void BtnReschedule_Click(object sender, EventArgs e)
        {
            var appt = GetSelectedAppointment();
            if (appt == null) return;

            if (!appt.IsOpen)
            {
                MessageBox.Show("Only Pending or Confirmed appointments can be rescheduled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime? newDate = PromptForNewDate(appt);
            if (newDate == null) return;

            if (HospitalData.HasAppointmentClash(appt.DoctorId, newDate.Value, appt.Id))
            {
                MessageBox.Show("This doctor already has an appointment around that time.", "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                HospitalData.RescheduleAppointment(appt, newDate.Value);
                MessageBox.Show("Appointment moved to " + appt.ScheduledOn.ToString("yyyy-MM-dd HH:mm") + ".", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAppointments();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Reschedule", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnComplete_Click(object sender, EventArgs e)
        {
            var appt = GetSelectedAppointment();
            if (appt == null) return;

            if (appt.Status != "Confirmed")
            {
                MessageBox.Show("Only Confirmed appointments can be marked as completed.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Mark this appointment as completed?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                HospitalData.CompleteAppointment(appt);
                LoadAppointments();
            }
        }

        // Small modal with a single date/time picker. Returns null if the user cancels.
        private DateTime? PromptForNewDate(Appointment appt)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Reschedule " + appt.AppointmentNo;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(320, 130);

                Label lbl = new Label();
                lbl.Text = "New date & time (currently " + appt.ScheduledOn.ToString("yyyy-MM-dd HH:mm") + ")";
                lbl.Location = new Point(15, 15);
                lbl.AutoSize = true;
                dlg.Controls.Add(lbl);

                DateTimePicker dtp = new DateTimePicker();
                dtp.Location = new Point(15, 40);
                dtp.Size = new Size(290, 28);
                dtp.Format = DateTimePickerFormat.Custom;
                dtp.CustomFormat = "yyyy-MM-dd HH:mm";
                dtp.MinDate = DateTime.Today;
                dtp.Value = appt.ScheduledOn < DateTime.Today ? DateTime.Now : appt.ScheduledOn;
                dlg.Controls.Add(dtp);

                Button ok = new Button();
                ok.Text = "Reschedule";
                ok.DialogResult = DialogResult.OK;
                ok.Location = new Point(115, 85);
                ok.Size = new Size(90, 30);
                dlg.Controls.Add(ok);

                Button cancel = new Button();
                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.Location = new Point(215, 85);
                cancel.Size = new Size(90, 30);
                dlg.Controls.Add(cancel);

                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                return dlg.ShowDialog(this) == DialogResult.OK ? dtp.Value : (DateTime?)null;
            }
        }
    }
}

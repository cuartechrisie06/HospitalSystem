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
        private DataGridView grid;
        private ComboBox cmbPatient, cmbDepartment, cmbDoctor;
        private DateTimePicker dtpDate;
        private TextBox txtReason;
        private Button btnSchedule, btnConfirm, btnCancel, btnReschedule, btnComplete;
        private Panel formPanel;
        private Label title;
        private Label lblP;
        private Label lblD;
        private Label lblDoc;
        private Label lblDate;
        private Label lblR;
        private Label lblList;
        private Panel btnPanel;
        private Label lblStatus;

        public AppointmentsView()
        {
            InitializeComponent();

            // Wire events
            cmbDepartment.SelectedIndexChanged += CmbDepartment_SelectedIndexChanged;
            btnSchedule.Click += BtnSchedule_Click;
            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += BtnCancel_Click;
            btnReschedule.Click += BtnReschedule_Click;
            btnComplete.Click += BtnComplete_Click;

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                LoadAppointments();
                LoadCombos();
            }
        }

        private void InitializeComponent()
        {
            this.formPanel = new System.Windows.Forms.Panel();
            this.title = new System.Windows.Forms.Label();
            this.lblP = new System.Windows.Forms.Label();
            this.cmbPatient = new System.Windows.Forms.ComboBox();
            this.lblD = new System.Windows.Forms.Label();
            this.cmbDepartment = new System.Windows.Forms.ComboBox();
            this.lblDoc = new System.Windows.Forms.Label();
            this.cmbDoctor = new System.Windows.Forms.ComboBox();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblR = new System.Windows.Forms.Label();
            this.txtReason = new System.Windows.Forms.TextBox();
            this.btnSchedule = new System.Windows.Forms.Button();
            this.lblList = new System.Windows.Forms.Label();
            this.btnPanel = new System.Windows.Forms.Panel();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnReschedule = new System.Windows.Forms.Button();
            this.btnComplete = new System.Windows.Forms.Button();
            this.grid = new System.Windows.Forms.DataGridView();
            this.formPanel.SuspendLayout();
            this.btnPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // formPanel
            // 
            this.formPanel.BackColor = System.Drawing.Color.White;
            this.formPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.formPanel.Controls.Add(this.title);
            this.formPanel.Controls.Add(this.lblP);
            this.formPanel.Controls.Add(this.cmbPatient);
            this.formPanel.Controls.Add(this.lblD);
            this.formPanel.Controls.Add(this.cmbDepartment);
            this.formPanel.Controls.Add(this.lblDoc);
            this.formPanel.Controls.Add(this.cmbDoctor);
            this.formPanel.Controls.Add(this.lblDate);
            this.formPanel.Controls.Add(this.dtpDate);
            this.formPanel.Controls.Add(this.lblR);
            this.formPanel.Controls.Add(this.txtReason);
            this.formPanel.Controls.Add(this.btnSchedule);
            this.formPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.formPanel.Location = new System.Drawing.Point(10, 80);
            this.formPanel.Name = "formPanel";
            this.formPanel.Padding = new System.Windows.Forms.Padding(15);
            this.formPanel.Size = new System.Drawing.Size(1454, 200);
            this.formPanel.TabIndex = 0;
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.title.Location = new System.Drawing.Point(15, 10);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(275, 32);
            this.title.TabIndex = 0;
            this.title.Text = "Schedule Appointment";
            // 
            // lblP
            // 
            this.lblP.AutoSize = true;
            this.lblP.Location = new System.Drawing.Point(15, 45);
            this.lblP.Name = "lblP";
            this.lblP.Size = new System.Drawing.Size(59, 20);
            this.lblP.TabIndex = 1;
            this.lblP.Text = "Patient";
            // 
            // cmbPatient
            // 
            this.cmbPatient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatient.Location = new System.Drawing.Point(15, 65);
            this.cmbPatient.Name = "cmbPatient";
            this.cmbPatient.Size = new System.Drawing.Size(250, 28);
            this.cmbPatient.TabIndex = 2;
            // 
            // lblD
            // 
            this.lblD.AutoSize = true;
            this.lblD.Location = new System.Drawing.Point(280, 45);
            this.lblD.Name = "lblD";
            this.lblD.Size = new System.Drawing.Size(94, 20);
            this.lblD.TabIndex = 3;
            this.lblD.Text = "Department";
            // 
            // cmbDepartment
            // 
            this.cmbDepartment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDepartment.Location = new System.Drawing.Point(280, 65);
            this.cmbDepartment.Name = "cmbDepartment";
            this.cmbDepartment.Size = new System.Drawing.Size(200, 28);
            this.cmbDepartment.TabIndex = 4;
            // 
            // lblDoc
            // 
            this.lblDoc.AutoSize = true;
            this.lblDoc.Location = new System.Drawing.Point(500, 45);
            this.lblDoc.Name = "lblDoc";
            this.lblDoc.Size = new System.Drawing.Size(57, 20);
            this.lblDoc.TabIndex = 5;
            this.lblDoc.Text = "Doctor";
            // 
            // cmbDoctor
            // 
            this.cmbDoctor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDoctor.Location = new System.Drawing.Point(500, 65);
            this.cmbDoctor.Name = "cmbDoctor";
            this.cmbDoctor.Size = new System.Drawing.Size(220, 28);
            this.cmbDoctor.TabIndex = 6;
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.Location = new System.Drawing.Point(15, 105);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(86, 20);
            this.lblDate.TabIndex = 7;
            this.lblDate.Text = "Date & Time";
            // 
            // dtpDate
            // 
            this.dtpDate.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDate.Location = new System.Drawing.Point(15, 125);
            this.dtpDate.MinDate = new System.DateTime(2026, 9, 26, 0, 0, 0, 0);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(250, 26);
            this.dtpDate.TabIndex = 8;
            // 
            // lblR
            // 
            this.lblR.AutoSize = true;
            this.lblR.Location = new System.Drawing.Point(280, 105);
            this.lblR.Name = "lblR";
            this.lblR.Size = new System.Drawing.Size(65, 20);
            this.lblR.TabIndex = 9;
            this.lblR.Text = "Reason";
            // 
            // txtReason
            // 
            this.txtReason.Location = new System.Drawing.Point(280, 125);
            this.txtReason.Name = "txtReason";
            this.txtReason.Size = new System.Drawing.Size(440, 26);
            this.txtReason.TabIndex = 10;
            // 
            // btnSchedule
            // 
            this.btnSchedule.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnSchedule.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSchedule.ForeColor = System.Drawing.Color.White;
            this.btnSchedule.Location = new System.Drawing.Point(15, 160);
            this.btnSchedule.Name = "btnSchedule";
            this.btnSchedule.Size = new System.Drawing.Size(180, 32);
            this.btnSchedule.TabIndex = 11;
            this.btnSchedule.Text = "Schedule Appointment";
            this.btnSchedule.UseVisualStyleBackColor = false;
            // 
            // lblList
            // 
            this.lblList.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblList.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblList.Location = new System.Drawing.Point(10, 50);
            this.lblList.Name = "lblList";
            this.lblList.Size = new System.Drawing.Size(1454, 30);
            this.lblList.TabIndex = 1;
            this.lblList.Text = "Appointments";
            // 
            // btnPanel
            // 
            this.btnPanel.Controls.Add(this.btnConfirm);
            this.btnPanel.Controls.Add(this.btnCancel);
            this.btnPanel.Controls.Add(this.btnReschedule);
            this.btnPanel.Controls.Add(this.btnComplete);
            this.btnPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnPanel.Location = new System.Drawing.Point(10, 10);
            this.btnPanel.Name = "btnPanel";
            this.btnPanel.Size = new System.Drawing.Size(1454, 40);
            this.btnPanel.TabIndex = 2;
            // 
            // btnConfirm
            // 
            this.btnConfirm.Location = new System.Drawing.Point(0, 5);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(140, 30);
            this.btnConfirm.TabIndex = 0;
            this.btnConfirm.Text = "Confirm Selected";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(150, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(140, 30);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel Selected";
            // 
            // btnReschedule
            // 
            this.btnReschedule.Location = new System.Drawing.Point(300, 5);
            this.btnReschedule.Name = "btnReschedule";
            this.btnReschedule.Size = new System.Drawing.Size(150, 30);
            this.btnReschedule.TabIndex = 2;
            this.btnReschedule.Text = "Reschedule Selected";
            // 
            // btnComplete
            // 
            this.btnComplete.Location = new System.Drawing.Point(460, 5);
            this.btnComplete.Name = "btnComplete";
            this.btnComplete.Size = new System.Drawing.Size(140, 30);
            this.btnComplete.TabIndex = 3;
            this.btnComplete.Text = "Mark Completed";
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersHeight = 34;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(10, 10);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 62;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1454, 824);
            this.grid.TabIndex = 3;
            // 
            // AppointmentsView
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.formPanel);
            this.Controls.Add(this.lblList);
            this.Controls.Add(this.btnPanel);
            this.Controls.Add(this.grid);
            this.Name = "AppointmentsView";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(1474, 844);
            this.formPanel.ResumeLayout(false);
            this.formPanel.PerformLayout();
            this.btnPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
        }

        private void LoadCombos()
        {
            // Patient
            cmbPatient.DisplayMember = "Name";
            cmbPatient.ValueMember = "Id";
            cmbPatient.DataSource = HospitalData.ActivePatients();

            // Department
            cmbDepartment.DisplayMember = "Name";
            cmbDepartment.ValueMember = "Id";
            cmbDepartment.DataSource = HospitalData.Departments.ToList();

            // Load doctors for the first department
            if (cmbDepartment.Items.Count > 0)
            {
                cmbDepartment.SelectedIndex = 0;
            }
        }

        private void CmbDepartment_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDepartment.SelectedValue == null) return;

            int deptId = Convert.ToInt32(cmbDepartment.SelectedValue);

            // Show all 5 doctors (Ana Reyes, Mark Villanueva, Liza Tan, Jose Cruz, Grace Lim)
            var doctors = HospitalData.ActiveDoctors().ToList();

            // Clear first to avoid duplicates
            cmbDoctor.DataSource = null;
            cmbDoctor.DisplayMember = "Name";
            cmbDoctor.ValueMember = "Id";
            cmbDoctor.DataSource = doctors;
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

        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            if (cmbPatient.SelectedValue == null || cmbDoctor.SelectedValue == null || cmbDepartment.SelectedValue == null)
            {
                MessageBox.Show("Please select Patient, Department and Doctor.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int doctorId = Convert.ToInt32(cmbDoctor.SelectedValue);
            DateTime when = dtpDate.Value;

            // Simple double-booking check
            if (HospitalData.HasAppointmentClash(doctorId, when))
            {
                MessageBox.Show("This doctor already has an appointment around that time.", "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HospitalData.AddAppointment(new Appointment
            {
                PatientId = Convert.ToInt32(cmbPatient.SelectedValue),
                DoctorId = doctorId,
                DepartmentId = Convert.ToInt32(cmbDepartment.SelectedValue),
                ScheduledOn = when,
                Reason = txtReason.Text.Trim(),
                Status = "Pending"
            });

            MessageBox.Show("Appointment scheduled (Pending).", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadAppointments();
            txtReason.Clear();
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
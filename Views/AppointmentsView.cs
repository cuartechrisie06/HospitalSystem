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
        private Button btnSchedule, btnConfirm, btnCancel;
        private Label lblStatus;

        public AppointmentsView()
        {
            InitializeComponent();

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
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Padding = new Padding(10);

            // ===== Top form =====
            Panel formPanel = new Panel();
            formPanel.Dock = DockStyle.Top;
            formPanel.Height = 200;
            formPanel.BackColor = Color.White;
            formPanel.Padding = new Padding(15);
            formPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(formPanel);

            Label title = new Label();
            title.Text = "Schedule Appointment";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.Location = new Point(15, 10);
            title.AutoSize = true;
            formPanel.Controls.Add(title);

            // Patient
            Label lblP = new Label();
            lblP.Text = "Patient";
            lblP.Location = new Point(15, 45);
            lblP.AutoSize = true;
            formPanel.Controls.Add(lblP);
            cmbPatient = new ComboBox();
            cmbPatient.Location = new Point(15, 65);
            cmbPatient.Size = new Size(250, 28);
            cmbPatient.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbPatient);

            // Department
            Label lblD = new Label();
            lblD.Text = "Department";
            lblD.Location = new Point(280, 45);
            lblD.AutoSize = true;
            formPanel.Controls.Add(lblD);
            cmbDepartment = new ComboBox();
            cmbDepartment.Location = new Point(280, 65);
            cmbDepartment.Size = new Size(200, 28);
            cmbDepartment.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDepartment.SelectedIndexChanged += CmbDepartment_SelectedIndexChanged;
            formPanel.Controls.Add(cmbDepartment);

            // Doctor
            Label lblDoc = new Label();
            lblDoc.Text = "Doctor";
            lblDoc.Location = new Point(500, 45);
            lblDoc.AutoSize = true;
            formPanel.Controls.Add(lblDoc);
            cmbDoctor = new ComboBox();
            cmbDoctor.Location = new Point(500, 65);
            cmbDoctor.Size = new Size(220, 28);
            cmbDoctor.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbDoctor);

            // Date
            Label lblDate = new Label();
            lblDate.Text = "Date & Time";
            lblDate.Location = new Point(15, 105);
            lblDate.AutoSize = true;
            formPanel.Controls.Add(lblDate);
            dtpDate = new DateTimePicker();
            dtpDate.Location = new Point(15, 125);
            dtpDate.Size = new Size(250, 28);
            dtpDate.Format = DateTimePickerFormat.Custom;
            dtpDate.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpDate.MinDate = DateTime.Today;
            formPanel.Controls.Add(dtpDate);

            // Reason
            Label lblR = new Label();
            lblR.Text = "Reason";
            lblR.Location = new Point(280, 105);
            lblR.AutoSize = true;
            formPanel.Controls.Add(lblR);
            txtReason = new TextBox();
            txtReason.Location = new Point(280, 125);
            txtReason.Size = new Size(440, 28);
            formPanel.Controls.Add(txtReason);

            btnSchedule = new Button();
            btnSchedule.Text = "Schedule Appointment";
            btnSchedule.Location = new Point(15, 160);
            btnSchedule.Size = new Size(180, 32);
            btnSchedule.BackColor = Color.FromArgb(37, 99, 235);
            btnSchedule.ForeColor = Color.White;
            btnSchedule.FlatStyle = FlatStyle.Flat;
            btnSchedule.Click += BtnSchedule_Click;
            formPanel.Controls.Add(btnSchedule);

            // ===== Grid =====
            Label lblList = new Label();
            lblList.Text = "Appointments";
            lblList.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblList.Dock = DockStyle.Top;
            lblList.Height = 30;
            this.Controls.Add(lblList);

            Panel btnPanel = new Panel();
            btnPanel.Dock = DockStyle.Top;
            btnPanel.Height = 40;
            this.Controls.Add(btnPanel);

            btnConfirm = new Button();
            btnConfirm.Text = "Confirm Selected";
            btnConfirm.Location = new Point(0, 5);
            btnConfirm.Size = new Size(140, 30);
            btnConfirm.Click += BtnConfirm_Click;
            btnPanel.Controls.Add(btnConfirm);

            btnCancel = new Button();
            btnCancel.Text = "Cancel Selected";
            btnCancel.Location = new Point(150, 5);
            btnCancel.Size = new Size(140, 30);
            btnCancel.Click += BtnCancel_Click;
            btnPanel.Controls.Add(btnCancel);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            this.Controls.Add(grid);
            grid.BringToFront();
        }

        private void LoadCombos()
        {
            cmbPatient.DataSource = HospitalData.ActivePatients();
            cmbPatient.DisplayMember = "ToString";
            cmbPatient.ValueMember = "Id";

            cmbDepartment.DisplayMember = "Name";
            cmbDepartment.ValueMember = "Id";
            cmbDepartment.DataSource = HospitalData.Departments.ToList();
            cmbDepartment.DisplayMember = "Name"; // ensure display member remains set after reordering (harmless duplicate)
        }

        private void CmbDepartment_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDepartment.SelectedValue == null) return;
            int deptId = Convert.ToInt32(cmbDepartment.SelectedValue);
            cmbDoctor.DataSource = HospitalData.ActiveDoctors().Where(d => d.DepartmentId == deptId).ToList();
            cmbDoctor.DisplayMember = "ToString";
            cmbDoctor.ValueMember = "Id";
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
            bool clash = HospitalData.Appointments.Any(a =>
                a.DoctorId == doctorId &&
                a.Status != "Cancelled" &&
                Math.Abs((a.ScheduledOn - when).TotalMinutes) < 30);

            if (clash)
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
            if (MessageBox.Show("Cancel this appointment?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                HospitalData.UpdateAppointmentStatus(appt, "Cancelled");
                LoadAppointments();
            }
        }
    }
}

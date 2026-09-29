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

        // Scheduling form
        private DataGridView gridPatients;
        private TextBox txtPatientSearch;
        private Label lblSelectedPatient;
        private ComboBox cmbDepartment, cmbDoctor;
        private DateTimePicker dtpDate;
        private TextBox txtReason;
        private Button btnSchedule;

        // Appointments list + actions
        private DataGridView grid;
        private Button btnConfirm, btnCancel, btnReschedule, btnComplete;

        private int selectedPatientId = 0;
        private bool patientSearchPlaceholderActive = true;

        public AppointmentsView()
        {
            InitializeComponent();
            WireEvents();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                LoadDepartments();
                LoadDoctors();
                LoadPatientList("");
                LoadAppointments();
            }
        }

        // Wired in plain code (not InitializeComponent) so it survives a Designer save,
        // matching the convention used by the other views in this project.
        private void WireEvents()
        {
            txtPatientSearch.Enter += TxtPatientSearch_Enter;
            txtPatientSearch.Leave += TxtPatientSearch_Leave;
            txtPatientSearch.TextChanged += TxtPatientSearch_TextChanged;
            gridPatients.CellClick += GridPatients_CellClick;

            btnSchedule.Click += BtnSchedule_Click;
            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += BtnCancel_Click;
            btnReschedule.Click += BtnReschedule_Click;
            btnComplete.Click += BtnComplete_Click;
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Padding = new Padding(10);

            // Deterministic top-to-bottom layout: schedule card, list header, action bar, grid.
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.FromArgb(243, 244, 246)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 372));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.Controls.Add(root);

            root.Controls.Add(BuildScheduleCard(), 0, 0);

            var lblList = new Label
            {
                Text = "Appointments",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            root.Controls.Add(lblList, 0, 1);

            root.Controls.Add(BuildActionBar(), 0, 2);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeight = 34
            };
            root.Controls.Add(grid, 0, 3);
        }

        private Panel BuildScheduleCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 8)
            };

            var title = new Label
            {
                Text = "Schedule Appointment",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 30
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));

            inner.Controls.Add(BuildPatientPicker(), 0, 0);
            inner.Controls.Add(BuildFieldsPanel(), 1, 0);

            card.Controls.Add(inner);
            card.Controls.Add(title);
            inner.BringToFront();
            return card;
        }

        // Left column: searchable table of ACTIVE patients (replaces the old combo box).
        private Panel BuildPatientPicker()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };

            var lblHdr = new Label
            {
                Text = "1. Search & select an active patient",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Dock = DockStyle.Top,
                Height = 22
            };

            txtPatientSearch = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                Text = PatientSearchPlaceholder,
                ForeColor = Color.Gray
            };
            // A little breathing room under the search box.
            var spacer = new Panel { Dock = DockStyle.Top, Height = 6 };

            gridPatients = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToResizeRows = false
            };

            panel.Controls.Add(gridPatients);
            panel.Controls.Add(spacer);
            panel.Controls.Add(txtPatientSearch);
            panel.Controls.Add(lblHdr);
            gridPatients.BringToFront();
            return panel;
        }

        // Right column: the remaining appointment fields.
        private Panel BuildFieldsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            lblSelectedPatient = new Label
            {
                Text = "Selected patient:  (none — pick one from the list on the left)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(0, 4),
                AutoSize = true,
                MaximumSize = new Size(460, 0)
            };
            panel.Controls.Add(lblSelectedPatient);

            AddFieldLabel(panel, "Department *", 0, 40);
            cmbDepartment = new ComboBox
            {
                Location = new Point(0, 62),
                Size = new Size(300, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            panel.Controls.Add(cmbDepartment);

            AddFieldLabel(panel, "Doctor *  (active only)", 0, 96);
            cmbDoctor = new ComboBox
            {
                Location = new Point(0, 118),
                Size = new Size(300, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            panel.Controls.Add(cmbDoctor);

            AddFieldLabel(panel, "Date & Time *", 0, 152);
            dtpDate = new DateTimePicker
            {
                Location = new Point(0, 174),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                MinDate = DateTime.Today
            };
            panel.Controls.Add(dtpDate);

            AddFieldLabel(panel, "Reason", 0, 208);
            txtReason = new TextBox
            {
                Location = new Point(0, 230),
                Size = new Size(360, 28),
                Font = new Font("Segoe UI", 10F)
            };
            panel.Controls.Add(txtReason);

            btnSchedule = new Button
            {
                Text = "Schedule Appointment",
                Location = new Point(0, 268),
                Size = new Size(200, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSchedule.FlatAppearance.BorderSize = 0;
            panel.Controls.Add(btnSchedule);

            return panel;
        }

        private Panel BuildActionBar()
        {
            var bar = new Panel { Dock = DockStyle.Fill };

            btnConfirm = MakeActionButton("Confirm Selected", 0);
            btnCancel = MakeActionButton("Cancel Selected", 150);
            btnReschedule = MakeActionButton("Reschedule Selected", 300);
            btnComplete = MakeActionButton("Mark Completed", 460);

            bar.Controls.Add(btnConfirm);
            bar.Controls.Add(btnCancel);
            bar.Controls.Add(btnReschedule);
            bar.Controls.Add(btnComplete);
            return bar;
        }

        private static Button MakeActionButton(string text, int x)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 6),
                Size = new Size(text.Length > 16 ? 150 : 140, 30),
                Cursor = Cursors.Hand
            };
        }

        private void AddFieldLabel(Panel parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(75, 85, 99)
            });
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

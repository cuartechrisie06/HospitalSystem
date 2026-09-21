using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    public class AdmissionsView : UserControl
    {
        private DataGridView gridAdmissions;
        private DataGridView gridBeds;
        private ComboBox cmbPatient, cmbDoctor, cmbBed;
        private TextBox txtDiagnosis;
        private Button btnAdmit, btnDischarge;

        public AdmissionsView()
        {
            InitializeComponent();
            LoadCombos();
            LoadAdmissions();
            LoadBeds();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Padding = new Padding(10);

            // ===== Admit form =====
            Panel formPanel = new Panel();
            formPanel.Dock = DockStyle.Top;
            formPanel.Height = 180;
            formPanel.BackColor = Color.White;
            formPanel.Padding = new Padding(15);
            formPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(formPanel);

            Label title = new Label();
            title.Text = "Admit Patient";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.Location = new Point(15, 10);
            title.AutoSize = true;
            formPanel.Controls.Add(title);

            Label lblP = new Label { Text = "Patient", Location = new Point(15, 45), AutoSize = true };
            formPanel.Controls.Add(lblP);
            cmbPatient = new ComboBox();
            cmbPatient.Location = new Point(15, 65);
            cmbPatient.Size = new Size(250, 28);
            cmbPatient.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbPatient);

            Label lblD = new Label { Text = "Doctor", Location = new Point(280, 45), AutoSize = true };
            formPanel.Controls.Add(lblD);
            cmbDoctor = new ComboBox();
            cmbDoctor.Location = new Point(280, 65);
            cmbDoctor.Size = new Size(220, 28);
            cmbDoctor.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbDoctor);

            Label lblB = new Label { Text = "Room / Bed (available only)", Location = new Point(520, 45), AutoSize = true };
            formPanel.Controls.Add(lblB);
            cmbBed = new ComboBox();
            cmbBed.Location = new Point(520, 65);
            cmbBed.Size = new Size(280, 28);
            cmbBed.DropDownStyle = ComboBoxStyle.DropDownList;
            formPanel.Controls.Add(cmbBed);

            Label lblDiag = new Label { Text = "Diagnosis / Reason", Location = new Point(15, 105), AutoSize = true };
            formPanel.Controls.Add(lblDiag);
            txtDiagnosis = new TextBox();
            txtDiagnosis.Location = new Point(15, 125);
            txtDiagnosis.Size = new Size(500, 28);
            formPanel.Controls.Add(txtDiagnosis);

            btnAdmit = new Button();
            btnAdmit.Text = "Admit Patient";
            btnAdmit.Location = new Point(530, 122);
            btnAdmit.Size = new Size(130, 32);
            btnAdmit.BackColor = Color.FromArgb(5, 150, 105);
            btnAdmit.ForeColor = Color.White;
            btnAdmit.FlatStyle = FlatStyle.Flat;
            btnAdmit.Click += BtnAdmit_Click;
            formPanel.Controls.Add(btnAdmit);

            btnDischarge = new Button();
            btnDischarge.Text = "Discharge Selected";
            btnDischarge.Location = new Point(670, 122);
            btnDischarge.Size = new Size(130, 32);
            btnDischarge.BackColor = Color.FromArgb(220, 38, 38);
            btnDischarge.ForeColor = Color.White;
            btnDischarge.FlatStyle = FlatStyle.Flat;
            btnDischarge.Click += BtnDischarge_Click;
            formPanel.Controls.Add(btnDischarge);

            // ===== Admissions grid =====
            Label lblAdm = new Label();
            lblAdm.Text = "Active & Recent Admissions";
            lblAdm.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblAdm.Dock = DockStyle.Top;
            lblAdm.Height = 28;
            this.Controls.Add(lblAdm);

            gridAdmissions = new DataGridView();
            gridAdmissions.Dock = DockStyle.Top;
            gridAdmissions.Height = 220;
            gridAdmissions.AllowUserToAddRows = false;
            gridAdmissions.ReadOnly = true;
            gridAdmissions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAdmissions.MultiSelect = false;
            gridAdmissions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAdmissions.RowHeadersVisible = false;
            gridAdmissions.BackgroundColor = Color.White;
            this.Controls.Add(gridAdmissions);

            // ===== Bed board =====
            Label lblBeds = new Label();
            lblBeds.Text = "Bed Status Board";
            lblBeds.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblBeds.Dock = DockStyle.Top;
            lblBeds.Height = 28;
            this.Controls.Add(lblBeds);

            gridBeds = new DataGridView();
            gridBeds.Dock = DockStyle.Fill;
            gridBeds.AllowUserToAddRows = false;
            gridBeds.ReadOnly = true;
            gridBeds.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridBeds.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridBeds.RowHeadersVisible = false;
            gridBeds.BackgroundColor = Color.White;
            this.Controls.Add(gridBeds);

            // Correct z-order
            this.Controls.SetChildIndex(gridBeds, 0);
            this.Controls.SetChildIndex(lblBeds, 0);
            this.Controls.SetChildIndex(gridAdmissions, 0);
            this.Controls.SetChildIndex(lblAdm, 0);
            this.Controls.SetChildIndex(formPanel, 0);
        }

        private void LoadCombos()
        {
            // Only patients not currently admitted
            var admittedIds = HospitalData.ActiveAdmissions().Select(a => a.PatientId).ToHashSet();
            cmbPatient.DataSource = HospitalData.Patients.Where(p => !admittedIds.Contains(p.Id)).ToList();
            cmbPatient.DisplayMember = "ToString";
            cmbPatient.ValueMember = "Id";

            cmbDoctor.DataSource = HospitalData.Doctors.ToList();
            cmbDoctor.DisplayMember = "ToString";
            cmbDoctor.ValueMember = "Id";

            cmbBed.DataSource = HospitalData.AvailableBeds();
            cmbBed.DisplayMember = "Label";
            cmbBed.ValueMember = "Id";
        }

        private void LoadAdmissions()
        {
            gridAdmissions.DataSource = HospitalData.Admissions
                .OrderByDescending(a => a.AdmittedOn)
                .Select(a => new
                {
                    a.Id,
                    a.AdmissionNo,
                    Patient = HospitalData.PatientName(a.PatientId),
                    Doctor = HospitalData.DoctorName(a.DoctorId),
                    Bed = HospitalData.BedLabel(a.BedId),
                    Admitted = a.AdmittedOn.ToString("yyyy-MM-dd HH:mm"),
                    Discharged = a.DischargedOn.HasValue ? a.DischargedOn.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                    a.Diagnosis,
                    a.Status,
                    Days = a.DaysStayed
                }).ToList();

            if (gridAdmissions.Columns["Id"] != null)
                gridAdmissions.Columns["Id"].Visible = false;
        }

        private void LoadBeds()
        {
            gridBeds.DataSource = HospitalData.Beds.Select(b => new
            {
                b.Id,
                Room = b.RoomNo,
                Bed = b.BedNo,
                b.Ward,
                b.Status
            }).ToList();

            if (gridBeds.Columns["Id"] != null)
                gridBeds.Columns["Id"].Visible = false;
        }

        private void BtnAdmit_Click(object sender, EventArgs e)
        {
            if (cmbPatient.SelectedValue == null || cmbDoctor.SelectedValue == null || cmbBed.SelectedValue == null)
            {
                MessageBox.Show("Please select Patient, Doctor and an available Bed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDiagnosis.Text))
            {
                MessageBox.Show("Please enter a diagnosis / reason for admission.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HospitalData.AddAdmission(new Admission
            {
                PatientId = Convert.ToInt32(cmbPatient.SelectedValue),
                DoctorId = Convert.ToInt32(cmbDoctor.SelectedValue),
                BedId = Convert.ToInt32(cmbBed.SelectedValue),
                Diagnosis = txtDiagnosis.Text.Trim()
            });

            MessageBox.Show("Patient admitted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtDiagnosis.Clear();
            LoadCombos();
            LoadAdmissions();
            LoadBeds();
        }

        private void BtnDischarge_Click(object sender, EventArgs e)
        {
            if (gridAdmissions.CurrentRow == null) return;
            int id = Convert.ToInt32(gridAdmissions.CurrentRow.Cells["Id"].Value);
            var adm = HospitalData.Admissions.FirstOrDefault(a => a.Id == id);
            if (adm == null) return;

            if (adm.Status != "Active")
            {
                MessageBox.Show("This admission is already discharged.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Discharge this patient and free the bed?", "Confirm Discharge",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                HospitalData.Discharge(adm);
                MessageBox.Show("Patient discharged. Stay: " + adm.DaysStayed + " day(s).", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCombos();
                LoadAdmissions();
                LoadBeds();
            }
        }
    }
}

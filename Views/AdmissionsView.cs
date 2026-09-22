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
        private Panel formPanel;
        private Label title;
        private Label lblP;
        private Label lblD;
        private Label lblB;
        private Label lblDiag;
        private Label lblAdm;
        private Label lblBeds;

        public AdmissionsView()
        {
            InitializeComponent();
            WireEvents();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                LoadCombos();
                LoadAdmissions();
                LoadBeds();
            }
        }

        // Deliberately kept OUT of InitializeComponent(): the WinForms Designer
        // only tracks event/property wiring made through its own component model.
        // Anything wired here in plain code survives a future Designer save intact;
        // anything left inside InitializeComponent() risks being silently dropped
        // the next time the form is opened and saved in the Designer.
        private void WireEvents()
        {
            btnAdmit.Click += BtnAdmit_Click;
            btnDischarge.Click += BtnDischarge_Click;
        }

        private void InitializeComponent()
        {
            this.formPanel = new System.Windows.Forms.Panel();
            this.title = new System.Windows.Forms.Label();
            this.lblP = new System.Windows.Forms.Label();
            this.cmbPatient = new System.Windows.Forms.ComboBox();
            this.lblD = new System.Windows.Forms.Label();
            this.cmbDoctor = new System.Windows.Forms.ComboBox();
            this.lblB = new System.Windows.Forms.Label();
            this.cmbBed = new System.Windows.Forms.ComboBox();
            this.lblDiag = new System.Windows.Forms.Label();
            this.txtDiagnosis = new System.Windows.Forms.TextBox();
            this.btnAdmit = new System.Windows.Forms.Button();
            this.btnDischarge = new System.Windows.Forms.Button();
            this.lblAdm = new System.Windows.Forms.Label();
            this.gridAdmissions = new System.Windows.Forms.DataGridView();
            this.lblBeds = new System.Windows.Forms.Label();
            this.gridBeds = new System.Windows.Forms.DataGridView();
            this.formPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAdmissions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridBeds)).BeginInit();
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
            this.formPanel.Controls.Add(this.cmbDoctor);
            this.formPanel.Controls.Add(this.lblB);
            this.formPanel.Controls.Add(this.cmbBed);
            this.formPanel.Controls.Add(this.lblDiag);
            this.formPanel.Controls.Add(this.txtDiagnosis);
            this.formPanel.Controls.Add(this.btnAdmit);
            this.formPanel.Controls.Add(this.btnDischarge);
            this.formPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.formPanel.Location = new System.Drawing.Point(10, 286);
            this.formPanel.Name = "formPanel";
            this.formPanel.Padding = new System.Windows.Forms.Padding(15);
            this.formPanel.Size = new System.Drawing.Size(807, 180);
            this.formPanel.TabIndex = 0;
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.title.Location = new System.Drawing.Point(15, 10);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(116, 21);
            this.title.TabIndex = 0;
            this.title.Text = "Admit Patient";
            // 
            // lblP
            // 
            this.lblP.AutoSize = true;
            this.lblP.Location = new System.Drawing.Point(15, 45);
            this.lblP.Name = "lblP";
            this.lblP.Size = new System.Drawing.Size(40, 13);
            this.lblP.TabIndex = 1;
            this.lblP.Text = "Patient";
            // 
            // cmbPatient
            // 
            this.cmbPatient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatient.Location = new System.Drawing.Point(15, 65);
            this.cmbPatient.Name = "cmbPatient";
            this.cmbPatient.Size = new System.Drawing.Size(250, 21);
            this.cmbPatient.TabIndex = 2;
            // 
            // lblD
            // 
            this.lblD.AutoSize = true;
            this.lblD.Location = new System.Drawing.Point(280, 45);
            this.lblD.Name = "lblD";
            this.lblD.Size = new System.Drawing.Size(39, 13);
            this.lblD.TabIndex = 3;
            this.lblD.Text = "Doctor";
            // 
            // cmbDoctor
            // 
            this.cmbDoctor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDoctor.Location = new System.Drawing.Point(280, 65);
            this.cmbDoctor.Name = "cmbDoctor";
            this.cmbDoctor.Size = new System.Drawing.Size(220, 21);
            this.cmbDoctor.TabIndex = 4;
            // 
            // lblB
            // 
            this.lblB.AutoSize = true;
            this.lblB.Location = new System.Drawing.Point(520, 45);
            this.lblB.Name = "lblB";
            this.lblB.Size = new System.Drawing.Size(138, 13);
            this.lblB.TabIndex = 5;
            this.lblB.Text = "Room / Bed (available only)";
            // 
            // cmbBed
            // 
            this.cmbBed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBed.Location = new System.Drawing.Point(520, 65);
            this.cmbBed.Name = "cmbBed";
            this.cmbBed.Size = new System.Drawing.Size(280, 21);
            this.cmbBed.TabIndex = 6;
            // 
            // lblDiag
            // 
            this.lblDiag.AutoSize = true;
            this.lblDiag.Location = new System.Drawing.Point(15, 105);
            this.lblDiag.Name = "lblDiag";
            this.lblDiag.Size = new System.Drawing.Size(101, 13);
            this.lblDiag.TabIndex = 7;
            this.lblDiag.Text = "Diagnosis / Reason";
            // 
            // txtDiagnosis
            // 
            this.txtDiagnosis.Location = new System.Drawing.Point(15, 125);
            this.txtDiagnosis.Name = "txtDiagnosis";
            this.txtDiagnosis.Size = new System.Drawing.Size(500, 20);
            this.txtDiagnosis.TabIndex = 8;
            // 
            // btnAdmit
            // 
            this.btnAdmit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(150)))), ((int)(((byte)(105)))));
            this.btnAdmit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdmit.ForeColor = System.Drawing.Color.White;
            this.btnAdmit.Location = new System.Drawing.Point(530, 122);
            this.btnAdmit.Name = "btnAdmit";
            this.btnAdmit.Size = new System.Drawing.Size(130, 32);
            this.btnAdmit.TabIndex = 9;
            this.btnAdmit.Text = "Admit Patient";
            this.btnAdmit.UseVisualStyleBackColor = false;
            // 
            // btnDischarge
            // 
            this.btnDischarge.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.btnDischarge.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDischarge.ForeColor = System.Drawing.Color.White;
            this.btnDischarge.Location = new System.Drawing.Point(670, 122);
            this.btnDischarge.Name = "btnDischarge";
            this.btnDischarge.Size = new System.Drawing.Size(130, 32);
            this.btnDischarge.TabIndex = 10;
            this.btnDischarge.Text = "Discharge Selected";
            this.btnDischarge.UseVisualStyleBackColor = false;
            // 
            // lblAdm
            // 
            this.lblAdm.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAdm.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblAdm.Location = new System.Drawing.Point(10, 258);
            this.lblAdm.Name = "lblAdm";
            this.lblAdm.Size = new System.Drawing.Size(807, 28);
            this.lblAdm.TabIndex = 1;
            this.lblAdm.Text = "Active & Recent Admissions";
            // 
            // gridAdmissions
            // 
            this.gridAdmissions.AllowUserToAddRows = false;
            this.gridAdmissions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridAdmissions.BackgroundColor = System.Drawing.Color.White;
            this.gridAdmissions.Dock = System.Windows.Forms.DockStyle.Top;
            this.gridAdmissions.Location = new System.Drawing.Point(10, 38);
            this.gridAdmissions.MultiSelect = false;
            this.gridAdmissions.Name = "gridAdmissions";
            this.gridAdmissions.ReadOnly = true;
            this.gridAdmissions.RowHeadersVisible = false;
            this.gridAdmissions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAdmissions.Size = new System.Drawing.Size(807, 220);
            this.gridAdmissions.TabIndex = 2;
            // 
            // lblBeds
            // 
            this.lblBeds.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBeds.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblBeds.Location = new System.Drawing.Point(10, 10);
            this.lblBeds.Name = "lblBeds";
            this.lblBeds.Size = new System.Drawing.Size(807, 28);
            this.lblBeds.TabIndex = 3;
            this.lblBeds.Text = "Bed Status Board";
            // 
            // gridBeds
            // 
            this.gridBeds.AllowUserToAddRows = false;
            this.gridBeds.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridBeds.BackgroundColor = System.Drawing.Color.White;
            this.gridBeds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridBeds.Location = new System.Drawing.Point(10, 10);
            this.gridBeds.Name = "gridBeds";
            this.gridBeds.ReadOnly = true;
            this.gridBeds.RowHeadersVisible = false;
            this.gridBeds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridBeds.Size = new System.Drawing.Size(807, 371);
            this.gridBeds.TabIndex = 4;
            // 
            // AdmissionsView
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.formPanel);
            this.Controls.Add(this.lblAdm);
            this.Controls.Add(this.gridAdmissions);
            this.Controls.Add(this.lblBeds);
            this.Controls.Add(this.gridBeds);
            this.Name = "AdmissionsView";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(827, 391);
            this.formPanel.ResumeLayout(false);
            this.formPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAdmissions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridBeds)).EndInit();
            this.ResumeLayout(false);

        }

        private void LoadCombos()
        {
            // Only patients not currently admitted
            var admittedIds = HospitalData.ActiveAdmissions().Select(a => a.PatientId).ToHashSet();
            cmbPatient.DataSource = HospitalData.ActivePatients().Where(p => !admittedIds.Contains(p.Id)).ToList();
            cmbPatient.DisplayMember = "ToString";
            cmbPatient.ValueMember = "Id";

            cmbDoctor.DataSource = HospitalData.ActiveDoctors();
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

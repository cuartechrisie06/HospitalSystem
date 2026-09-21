using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    public class PatientsView : UserControl
    {
        private DataGridView grid;
        private TextBox txtSearch;
        private TextBox txtName, txtAge, txtContact, txtAddress;
        private ComboBox cmbGender, cmbBlood;
        private Button btnSearch, btnNew, btnSave;
        private Label lblFormTitle;
        private int selectedId = 0;

        public PatientsView()
        {
            InitializeComponent();
            btnSearch.Click += (s, e) => LoadPatients(txtSearch.Text.Trim());
            LoadPatients("");
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(243, 244, 246);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.SplitterDistance = 700;
            split.FixedPanel = FixedPanel.Panel1;
            split.BackColor = Color.FromArgb(243, 244, 246);
            this.Controls.Add(split);

            // LEFT
            Panel left = new Panel();
            left.Dock = DockStyle.Fill;
            left.Padding = new Padding(0, 0, 10, 0);
            left.BackColor = Color.FromArgb(243, 244, 246);
            split.Panel1.Controls.Add(left);

            Label lblList = new Label();
            lblList.Text = "Patient List";
            lblList.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblList.ForeColor = Color.FromArgb(30, 41, 59);
            lblList.Location = new Point(0, 5);
            lblList.AutoSize = true;
            left.Controls.Add(lblList);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(0, 40);
            txtSearch.Size = new Size(280, 28);
            txtSearch.Font = new Font("Segoe UI", 10F);
            left.Controls.Add(txtSearch);

            btnSearch = new Button();
            btnSearch.Text = "Search";
            btnSearch.Location = new Point(290, 38);
            btnSearch.Size = new Size(80, 30);
            btnSearch.FlatStyle = FlatStyle.Flat;
            left.Controls.Add(btnSearch);

            btnNew = new Button();
            btnNew.Text = "+ New";
            btnNew.Location = new Point(380, 38);
            btnNew.Size = new Size(90, 30);
            btnNew.BackColor = Color.FromArgb(37, 99, 235);
            btnNew.ForeColor = Color.White;
            btnNew.FlatStyle = FlatStyle.Flat;
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += BtnNew_Click;
            left.Controls.Add(btnNew);

            grid = new DataGridView();
            grid.Location = new Point(0, 80);
            grid.Size = new Size(540, 480);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.SelectionChanged += Grid_SelectionChanged;
            left.Controls.Add(grid);

            // RIGHT
            Panel right = new Panel();
            right.Dock = DockStyle.Fill;
            right.BackColor = Color.White;
            right.Padding = new Padding(25);
            right.BorderStyle = BorderStyle.FixedSingle;
            split.Panel2.Controls.Add(right);

            lblFormTitle = new Label();
            lblFormTitle.Text = "Register New Patient";
            lblFormTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblFormTitle.ForeColor = Color.FromArgb(30, 41, 59);
            lblFormTitle.Location = new Point(25, 20);
            lblFormTitle.AutoSize = true;
            right.Controls.Add(lblFormTitle);

            int y = 65;
            AddLabel(right, "Full Name *", 25, y);
            txtName = AddTextBox(right, 25, y + 22, 320);
            y += 65;

            AddLabel(right, "Age *", 25, y);
            txtAge = AddTextBox(right, 25, y + 22, 100);

            AddLabel(right, "Gender", 150, y);
            cmbGender = new ComboBox();
            cmbGender.Location = new Point(150, y + 22);
            cmbGender.Size = new Size(130, 28);
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGender.Font = new Font("Segoe UI", 10F);
            cmbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
            right.Controls.Add(cmbGender);
            y += 65;

            AddLabel(right, "Contact", 25, y);
            txtContact = AddTextBox(right, 25, y + 22, 320);
            y += 65;

            AddLabel(right, "Address", 25, y);
            txtAddress = AddTextBox(right, 25, y + 22, 320);
            y += 65;

            AddLabel(right, "Blood Type", 25, y);
            cmbBlood = new ComboBox();
            cmbBlood.Location = new Point(25, y + 22);
            cmbBlood.Size = new Size(130, 28);
            cmbBlood.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBlood.Font = new Font("Segoe UI", 10F);
            cmbBlood.Items.AddRange(new object[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" });
            right.Controls.Add(cmbBlood);
            y += 80;

            btnSave = new Button();
            btnSave.Text = "Save Patient";
            btnSave.Location = new Point(25, y);
            btnSave.Size = new Size(160, 40);
            btnSave.BackColor = Color.FromArgb(37, 99, 235);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += BtnSave_Click;
            right.Controls.Add(btnSave);
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI", 9F);
            lbl.ForeColor = Color.FromArgb(75, 85, 99);
            parent.Controls.Add(lbl);
        }

        private TextBox AddTextBox(Control parent, int x, int y, int width)
        {
            TextBox txt = new TextBox();
            txt.Location = new Point(x, y);
            txt.Size = new Size(width, 28);
            txt.Font = new Font("Segoe UI", 10F);
            parent.Controls.Add(txt);
            return txt;
        }

        private void LoadPatients(string filter)
        {
            var source = HospitalData.Patients.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filter = filter.ToLower();
                source = source.Where(p =>
                    p.FullName.ToLower().Contains(filter) ||
                    p.PatientNo.ToLower().Contains(filter) ||
                    (p.Contact != null && p.Contact.Contains(filter)));
            }
            grid.DataSource = source.Select(p => new
            {
                p.Id,
                No = p.PatientNo,
                Name = p.FullName,
                p.Age,
                p.Gender,
                p.Contact,
                Blood = p.BloodType
            }).ToList();
            if (grid.Columns["Id"] != null)
                grid.Columns["Id"].Visible = false;
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Index < 0) return;
            var idObj = grid.CurrentRow.Cells["Id"].Value;
            if (idObj == null) return;
            selectedId = Convert.ToInt32(idObj);
            var p = HospitalData.GetPatient(selectedId);
            if (p == null) return;
            lblFormTitle.Text = "Update Patient – " + p.PatientNo;
            txtName.Text = p.FullName;
            txtAge.Text = p.Age.ToString();
            txtContact.Text = p.Contact ?? "";
            txtAddress.Text = p.Address ?? "";
            cmbGender.SelectedItem = p.Gender;
            cmbBlood.SelectedItem = p.BloodType;
            btnSave.Text = "Update Record";
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            grid.SelectionChanged -= Grid_SelectionChanged;

            selectedId = 0;
            lblFormTitle.Text = "Register New Patient";
            txtName.Clear();
            txtAge.Clear();
            txtContact.Clear();
            txtAddress.Clear();
            cmbGender.SelectedIndex = -1;
            cmbBlood.SelectedIndex = -1;
            btnSave.Text = "Save Patient";

            grid.ClearSelection();
            if (grid.CurrentCell != null)
                grid.CurrentCell = null;

            grid.SelectionChanged += Grid_SelectionChanged;
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Full Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            int age;
            if (!int.TryParse(txtAge.Text, out age) || age <= 0 || age > 150)
            {
                MessageBox.Show("Please enter a valid age (1-150).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAge.Focus();
                return;
            }
            if (selectedId == 0)
            {
                HospitalData.AddPatient(new Patient
                {
                    FullName = txtName.Text.Trim(),
                    Age = age,
                    Gender = cmbGender.SelectedItem != null ? cmbGender.SelectedItem.ToString() : "",
                    Contact = txtContact.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    BloodType = cmbBlood.SelectedItem != null ? cmbBlood.SelectedItem.ToString() : ""
                });
                MessageBox.Show("Patient registered successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var p = HospitalData.GetPatient(selectedId);
                if (p != null)
                {
                    p.FullName = txtName.Text.Trim();
                    p.Age = age;
                    p.Gender = cmbGender.SelectedItem != null ? cmbGender.SelectedItem.ToString() : "";
                    p.Contact = txtContact.Text.Trim();
                    p.Address = txtAddress.Text.Trim();
                    p.BloodType = cmbBlood.SelectedItem != null ? cmbBlood.SelectedItem.ToString() : "";
                    MessageBox.Show("Patient updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            LoadPatients(txtSearch.Text.Trim());
            BtnNew_Click(null, null);
        }
    }
}
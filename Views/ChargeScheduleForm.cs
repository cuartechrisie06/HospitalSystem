using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // Maintains the pre-set charges that are billed automatically on admission.
    public class ChargeScheduleForm : Form
    {
        private const string AllWards = "(All wards)";

        private DataGridView grid;
        private TextBox txtDescription;
        private ComboBox cmbCategory, cmbWard;
        private NumericUpDown numPrice;
        private CheckBox chkPerDay, chkShowInactive;
        private Button btnAdd, btnUpdate, btnToggle, btnClear, btnClose;

        public ChargeScheduleForm()
        {
            InitializeComponent();

            if (!DesignTimeHelper.IsDesignMode)
            {
                LoadWards();
                LoadGrid();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Admission Charge Schedule";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(760, 480);
            this.BackColor = Color.White;

            Label info = new Label();
            info.Text = "These charges are added to a patient's bill automatically when they are admitted. " +
                        "Per-day charges are updated to the full length of stay on discharge. " +
                        "Price changes only affect bills generated afterwards.";
            info.Location = new Point(15, 10);
            info.Size = new Size(730, 32);
            this.Controls.Add(info);

            chkShowInactive = new CheckBox();
            chkShowInactive.Text = "Show inactive";
            chkShowInactive.Location = new Point(15, 45);
            chkShowInactive.AutoSize = true;
            chkShowInactive.CheckedChanged += (s, e) => LoadGrid();
            this.Controls.Add(chkShowInactive);

            grid = new DataGridView();
            grid.Location = new Point(15, 70);
            grid.Size = new Size(730, 240);
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.SelectionChanged += Grid_SelectionChanged;
            this.Controls.Add(grid);

            this.Controls.Add(MakeLabel("Description", 15, 325));
            txtDescription = new TextBox();
            txtDescription.Location = new Point(15, 345);
            txtDescription.Size = new Size(250, 28);
            this.Controls.Add(txtDescription);

            this.Controls.Add(MakeLabel("Category", 275, 325));
            cmbCategory = new ComboBox();
            cmbCategory.Location = new Point(275, 345);
            cmbCategory.Size = new Size(110, 28);
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.DataSource = Enum.GetValues(typeof(BillCategory));
            this.Controls.Add(cmbCategory);

            this.Controls.Add(MakeLabel("Ward", 395, 325));
            cmbWard = new ComboBox();
            cmbWard.Location = new Point(395, 345);
            cmbWard.Size = new Size(130, 28);
            cmbWard.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(cmbWard);

            this.Controls.Add(MakeLabel("Unit Price", 535, 325));
            numPrice = new NumericUpDown();
            numPrice.Location = new Point(535, 345);
            numPrice.Size = new Size(100, 28);
            numPrice.DecimalPlaces = 2;
            numPrice.Maximum = 10000000;
            numPrice.ThousandsSeparator = true;
            this.Controls.Add(numPrice);

            chkPerDay = new CheckBox();
            chkPerDay.Text = "Per day";
            chkPerDay.Location = new Point(650, 347);
            chkPerDay.AutoSize = true;
            this.Controls.Add(chkPerDay);

            btnAdd = MakeButton("Add New", 15, 390);
            btnAdd.BackColor = Color.FromArgb(37, 99, 235);
            btnAdd.ForeColor = Color.White;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = MakeButton("Update Selected", 135, 390);
            btnUpdate.Click += BtnUpdate_Click;

            btnToggle = MakeButton("Deactivate", 255, 390);
            btnToggle.Click += BtnToggle_Click;

            btnClear = MakeButton("Clear", 375, 390);
            btnClear.Click += (s, e) => ClearFields();

            btnClose = MakeButton("Close", 635, 435);
            btnClose.DialogResult = DialogResult.OK;
            this.CancelButton = btnClose;
        }

        private Label MakeLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            return lbl;
        }

        private Button MakeButton(string text, int x, int y)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(110, 30);
            this.Controls.Add(btn);
            return btn;
        }

        // -------------------- Loading --------------------
        private void LoadWards()
        {
            var wards = new List<string> { AllWards };
            wards.AddRange(HospitalData.Wards());
            cmbWard.DataSource = wards;
        }

        private void LoadGrid(int selectId = 0)
        {
            grid.DataSource = HospitalData.ChargeSchedules
                .Where(c => chkShowInactive.Checked || c.IsActive)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Description)
                .Select(c => new
                {
                    c.Id,
                    c.Description,
                    Category = c.Category.ToString(),
                    Ward = c.WardLabel,
                    UnitPrice = c.UnitPrice.ToString("N2"),
                    c.Basis,
                    Status = c.IsActive ? "Active" : "Inactive"
                }).ToList();

            if (grid.Columns["Id"] != null)
                grid.Columns["Id"].Visible = false;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (Convert.ToInt32(row.Cells["Id"].Value) == selectId)
                {
                    grid.CurrentCell = row.Cells["Description"];
                    break;
                }
            }

            Grid_SelectionChanged(grid, EventArgs.Empty);
        }

        private ChargeSchedule GetSelected()
        {
            if (grid.CurrentRow == null) return null;
            int id = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
            return HospitalData.ChargeSchedules.FirstOrDefault(c => c.Id == id);
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            var c = GetSelected();
            btnUpdate.Enabled = c != null;
            btnToggle.Enabled = c != null;
            if (c == null) return;

            txtDescription.Text = c.Description;
            cmbCategory.SelectedItem = c.Category;
            // A ward no longer used by any bed still shows up so it isn't silently lost on update.
            if (!string.IsNullOrEmpty(c.Ward) && !cmbWard.Items.Contains(c.Ward))
            {
                var wards = ((List<string>)cmbWard.DataSource).ToList();
                wards.Add(c.Ward);
                cmbWard.DataSource = wards;
            }
            cmbWard.SelectedItem = string.IsNullOrEmpty(c.Ward) ? AllWards : c.Ward;
            numPrice.Value = Math.Min(c.UnitPrice, numPrice.Maximum);
            chkPerDay.Checked = c.PerDay;
            btnToggle.Text = c.IsActive ? "Deactivate" : "Reactivate";
        }

        private void ClearFields()
        {
            grid.ClearSelection();
            grid.CurrentCell = null;
            txtDescription.Clear();
            cmbCategory.SelectedIndex = 0;
            cmbWard.SelectedItem = AllWards;
            numPrice.Value = 0;
            chkPerDay.Checked = false;
            txtDescription.Focus();
        }

        // -------------------- Actions --------------------
        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Please enter a description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (numPrice.Value <= 0)
            {
                MessageBox.Show("Unit price must be greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void ApplyFields(ChargeSchedule c)
        {
            string ward = cmbWard.SelectedItem as string;
            c.Description = txtDescription.Text.Trim();
            c.Category = (BillCategory)cmbCategory.SelectedItem;
            c.Ward = ward == AllWards ? null : ward;
            c.UnitPrice = numPrice.Value;
            c.PerDay = chkPerDay.Checked;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateFields()) return;

            var c = new ChargeSchedule();
            ApplyFields(c);
            HospitalData.AddChargeSchedule(c);
            LoadGrid(c.Id);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            var c = GetSelected();
            if (c == null || !ValidateFields()) return;

            ApplyFields(c);
            HospitalData.UpdateChargeSchedule(c);
            LoadGrid(c.Id);
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            var c = GetSelected();
            if (c == null) return;

            c.IsActive = !c.IsActive;
            HospitalData.UpdateChargeSchedule(c);
            LoadGrid(c.Id);
        }
    }
}

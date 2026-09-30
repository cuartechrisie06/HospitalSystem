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

        private Label lblInfo;
        private CheckBox chkShowInactive;
        private DataGridView grid;
        private Label lblDescription;
        private TextBox txtDescription;
        private Label lblCategory;
        private ComboBox cmbCategory;
        private Label lblWard;
        private ComboBox cmbWard;
        private Label lblUnitPrice;
        private NumericUpDown numPrice;
        private CheckBox chkPerDay;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnToggle;
        private Button btnClear;
        private Button btnClose;

        public ChargeScheduleForm()
        {
            // Every control and event is set up in InitializeComponent() (Designer format),
            // so the Designer shows the complete dialog.
            InitializeComponent();

            if (!DesignTimeHelper.IsDesignMode)
            {
                cmbCategory.DataSource = Enum.GetValues(typeof(BillCategory));
                LoadWards();
                LoadGrid();
            }
        }

        // Designer-generated layout: explanation, "show inactive", the schedule grid, the
        // edit fields (description, category, ward, price, per day), the action buttons and Close.
        private void InitializeComponent()
        {
            this.lblInfo = new System.Windows.Forms.Label();
            this.chkShowInactive = new System.Windows.Forms.CheckBox();
            this.grid = new System.Windows.Forms.DataGridView();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblWard = new System.Windows.Forms.Label();
            this.cmbWard = new System.Windows.Forms.ComboBox();
            this.lblUnitPrice = new System.Windows.Forms.Label();
            this.numPrice = new System.Windows.Forms.NumericUpDown();
            this.chkPerDay = new System.Windows.Forms.CheckBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnToggle = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrice)).BeginInit();
            this.SuspendLayout();
            //
            // lblInfo
            //
            this.lblInfo.Location = new System.Drawing.Point(15, 10);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(730, 32);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "These charges are added to a patient\'s bill automatically when they are admitted. Per-day charges are updated to the full length of stay on discharge. Price changes only affect bills generated afterwards.";
            //
            // chkShowInactive
            //
            this.chkShowInactive.AutoSize = true;
            this.chkShowInactive.Location = new System.Drawing.Point(15, 45);
            this.chkShowInactive.Name = "chkShowInactive";
            this.chkShowInactive.Size = new System.Drawing.Size(93, 17);
            this.chkShowInactive.TabIndex = 1;
            this.chkShowInactive.Text = "Show inactive";
            this.chkShowInactive.CheckedChanged += new System.EventHandler(this.ChkShowInactive_CheckedChanged);
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.Location = new System.Drawing.Point(15, 70);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(730, 240);
            this.grid.TabIndex = 2;
            this.grid.SelectionChanged += new System.EventHandler(this.Grid_SelectionChanged);
            //
            // lblDescription
            //
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(15, 325);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(60, 13);
            this.lblDescription.TabIndex = 3;
            this.lblDescription.Text = "Description";
            //
            // txtDescription
            //
            this.txtDescription.Location = new System.Drawing.Point(15, 345);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(250, 20);
            this.txtDescription.TabIndex = 4;
            //
            // lblCategory
            //
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(275, 325);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(49, 13);
            this.lblCategory.TabIndex = 5;
            this.lblCategory.Text = "Category";
            //
            // cmbCategory
            // (items come from the BillCategory enum at runtime)
            //
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.Location = new System.Drawing.Point(275, 345);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(110, 21);
            this.cmbCategory.TabIndex = 6;
            //
            // lblWard
            //
            this.lblWard.AutoSize = true;
            this.lblWard.Location = new System.Drawing.Point(395, 325);
            this.lblWard.Name = "lblWard";
            this.lblWard.Size = new System.Drawing.Size(33, 13);
            this.lblWard.TabIndex = 7;
            this.lblWard.Text = "Ward";
            //
            // cmbWard
            //
            this.cmbWard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWard.Location = new System.Drawing.Point(395, 345);
            this.cmbWard.Name = "cmbWard";
            this.cmbWard.Size = new System.Drawing.Size(130, 21);
            this.cmbWard.TabIndex = 8;
            //
            // lblUnitPrice
            //
            this.lblUnitPrice.AutoSize = true;
            this.lblUnitPrice.Location = new System.Drawing.Point(535, 325);
            this.lblUnitPrice.Name = "lblUnitPrice";
            this.lblUnitPrice.Size = new System.Drawing.Size(53, 13);
            this.lblUnitPrice.TabIndex = 9;
            this.lblUnitPrice.Text = "Unit Price";
            //
            // numPrice
            //
            this.numPrice.DecimalPlaces = 2;
            this.numPrice.Location = new System.Drawing.Point(535, 345);
            this.numPrice.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numPrice.Name = "numPrice";
            this.numPrice.Size = new System.Drawing.Size(100, 20);
            this.numPrice.TabIndex = 10;
            this.numPrice.ThousandsSeparator = true;
            //
            // chkPerDay
            //
            this.chkPerDay.AutoSize = true;
            this.chkPerDay.Location = new System.Drawing.Point(650, 347);
            this.chkPerDay.Name = "chkPerDay";
            this.chkPerDay.Size = new System.Drawing.Size(63, 17);
            this.chkPerDay.TabIndex = 11;
            this.chkPerDay.Text = "Per day";
            //
            // btnAdd
            //
            this.btnAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Location = new System.Drawing.Point(15, 390);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(110, 30);
            this.btnAdd.TabIndex = 12;
            this.btnAdd.Text = "Add New";
            this.btnAdd.UseVisualStyleBackColor = false;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            //
            // btnUpdate
            //
            this.btnUpdate.Location = new System.Drawing.Point(135, 390);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(110, 30);
            this.btnUpdate.TabIndex = 13;
            this.btnUpdate.Text = "Update Selected";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);
            //
            // btnToggle
            //
            this.btnToggle.Location = new System.Drawing.Point(255, 390);
            this.btnToggle.Name = "btnToggle";
            this.btnToggle.Size = new System.Drawing.Size(110, 30);
            this.btnToggle.TabIndex = 14;
            this.btnToggle.Text = "Deactivate";
            this.btnToggle.UseVisualStyleBackColor = true;
            this.btnToggle.Click += new System.EventHandler(this.BtnToggle_Click);
            //
            // btnClear
            //
            this.btnClear.Location = new System.Drawing.Point(375, 390);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(110, 30);
            this.btnClear.TabIndex = 15;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            //
            // btnClose
            //
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Location = new System.Drawing.Point(635, 435);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(110, 30);
            this.btnClose.TabIndex = 16;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // ChargeScheduleForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(760, 480);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.chkShowInactive);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.cmbCategory);
            this.Controls.Add(this.lblWard);
            this.Controls.Add(this.cmbWard);
            this.Controls.Add(this.lblUnitPrice);
            this.Controls.Add(this.numPrice);
            this.Controls.Add(this.chkPerDay);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnToggle);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChargeScheduleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Admission Charge Schedule";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrice)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void ChkShowInactive_CheckedChanged(object sender, EventArgs e) => LoadGrid();

        private void BtnClear_Click(object sender, EventArgs e) => ClearFields();

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

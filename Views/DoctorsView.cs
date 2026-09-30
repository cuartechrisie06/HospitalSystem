using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    public class DoctorsView : UserControl
    {
        // Philippine mobile format: 09171234567 or +639171234567
        private static readonly Regex PhoneRegex = new Regex(@"^(09\d{9}|\+639\d{9})$");
        private const string SearchPlaceholder = "Search doctors by ID, name, specialization...";

        private DataGridView grid;
        private DataGridViewTextBoxColumn colId;
        private DataGridViewTextBoxColumn colNo;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colSpecialization;
        private DataGridViewTextBoxColumn colDepartment;
        private DataGridViewTextBoxColumn colContact;
        private DataGridViewTextBoxColumn colStatus;
        private DataGridViewButtonColumn colEdit;
        private DataGridViewButtonColumn colAction;
        private Panel header;
        private Label lblList;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnClear;
        private Button btnNew;
        private Label lblViewOnly;
        private CheckBox chkShowInactive;
        private Label lblCount;
        private bool searchPlaceholderActive = true;

        public DoctorsView()
        {
            // Every control, grid column and event is set up in InitializeComponent()
            // (Designer format), so the Designer shows the complete screen.
            InitializeComponent();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                ApplyPermissions();
                LoadDoctors("");
            }
        }

        // Doctor records are maintained by administrators; nurses get a read-only list.
        private void ApplyPermissions()
        {
            bool manage = Permissions.Can(Permission.ManageDoctors);
            btnNew.Visible = manage;
            colEdit.Visible = manage;
            colAction.Visible = manage;
            lblViewOnly.Visible = !manage;
            if (!manage)
                chkShowInactive.Left = lblViewOnly.Right + 20;
        }

        private void BtnSearch_Click(object sender, EventArgs e) => LoadDoctors(GetSearchText());

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearSearchBox();
            LoadDoctors("");
        }

        private void BtnNewDoctor_Click(object sender, EventArgs e) => OpenEditForm(null);

        private void ChkShowInactive_CheckedChanged(object sender, EventArgs e) => LoadDoctors(GetSearchText());

        // Designer-generated layout: header (title, search, buttons, "show inactive", count)
        // and the doctor grid with fixed columns (AutoGenerateColumns = false). The two button
        // columns (Edit, Deactivate/Activate) and "+ New Doctor" are hidden for nurses at runtime,
        // when the "View only" note is shown instead.
        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.lblList = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.lblViewOnly = new System.Windows.Forms.Label();
            this.chkShowInactive = new System.Windows.Forms.CheckBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSpecialization = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDepartment = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colContact = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEdit = new System.Windows.Forms.DataGridViewButtonColumn();
            this.colAction = new System.Windows.Forms.DataGridViewButtonColumn();
            this.header.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            //
            // header
            //
            this.header.Controls.Add(this.lblList);
            this.header.Controls.Add(this.txtSearch);
            this.header.Controls.Add(this.btnSearch);
            this.header.Controls.Add(this.btnClear);
            this.header.Controls.Add(this.btnNew);
            this.header.Controls.Add(this.lblViewOnly);
            this.header.Controls.Add(this.chkShowInactive);
            this.header.Controls.Add(this.lblCount);
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(15, 15);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(994, 90);
            this.header.TabIndex = 0;
            //
            // lblList
            //
            this.lblList.AutoSize = true;
            this.lblList.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblList.Location = new System.Drawing.Point(0, 0);
            this.lblList.Name = "lblList";
            this.lblList.Size = new System.Drawing.Size(76, 25);
            this.lblList.TabIndex = 0;
            this.lblList.Text = "Doctors";
            //
            // txtSearch
            //
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtSearch.Location = new System.Drawing.Point(0, 36);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(360, 25);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.Text = "Search doctors by ID, name, specialization...";
            this.txtSearch.Enter += new System.EventHandler(this.TxtSearch_Enter);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtSearch_KeyDown);
            this.txtSearch.Leave += new System.EventHandler(this.TxtSearch_Leave);
            //
            // btnSearch
            //
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSearch.FlatAppearance.BorderSize = 0;
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.Location = new System.Drawing.Point(368, 35);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(85, 32);
            this.btnSearch.TabIndex = 2;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = false;
            this.btnSearch.Click += new System.EventHandler(this.BtnSearch_Click);
            //
            // btnClear
            //
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Location = new System.Drawing.Point(461, 35);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 32);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear";
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            //
            // btnNew
            //
            this.btnNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(150)))), ((int)(((byte)(105)))));
            this.btnNew.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNew.FlatAppearance.BorderSize = 0;
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnNew.ForeColor = System.Drawing.Color.White;
            this.btnNew.Location = new System.Drawing.Point(544, 35);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(140, 32);
            this.btnNew.TabIndex = 4;
            this.btnNew.Text = "+ New Doctor";
            this.btnNew.UseVisualStyleBackColor = false;
            this.btnNew.Click += new System.EventHandler(this.BtnNewDoctor_Click);
            //
            // lblViewOnly
            //
            this.lblViewOnly.AutoSize = true;
            this.lblViewOnly.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
            this.lblViewOnly.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblViewOnly.Location = new System.Drawing.Point(544, 72);
            this.lblViewOnly.Name = "lblViewOnly";
            this.lblViewOnly.Size = new System.Drawing.Size(300, 15);
            this.lblViewOnly.TabIndex = 7;
            this.lblViewOnly.Text = "View only — doctor records are managed by administrators";
            this.lblViewOnly.Visible = false;
            //
            // chkShowInactive
            //
            this.chkShowInactive.AutoSize = true;
            this.chkShowInactive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkShowInactive.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkShowInactive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.chkShowInactive.Location = new System.Drawing.Point(700, 41);
            this.chkShowInactive.Name = "chkShowInactive";
            this.chkShowInactive.Size = new System.Drawing.Size(98, 19);
            this.chkShowInactive.TabIndex = 5;
            this.chkShowInactive.Text = "Show inactive";
            this.chkShowInactive.CheckedChanged += new System.EventHandler(this.ChkShowInactive_CheckedChanged);
            //
            // lblCount
            //
            this.lblCount.AutoSize = true;
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblCount.Location = new System.Drawing.Point(0, 72);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(92, 15);
            this.lblCount.TabIndex = 6;
            this.lblCount.Text = "Showing doctors";
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoGenerateColumns = false;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colId,
            this.colNo,
            this.colName,
            this.colSpecialization,
            this.colDepartment,
            this.colContact,
            this.colStatus,
            this.colEdit,
            this.colAction});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(15, 105);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(994, 640);
            this.grid.TabIndex = 1;
            this.grid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellClick);
            this.grid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.Grid_CellFormatting);
            //
            // colId
            //
            this.colId.DataPropertyName = "Id";
            this.colId.HeaderText = "Id";
            this.colId.Name = "colId";
            this.colId.ReadOnly = true;
            this.colId.Visible = false;
            //
            // colNo
            //
            this.colNo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colNo.DataPropertyName = "No";
            this.colNo.HeaderText = "Doctor ID";
            this.colNo.Name = "colNo";
            this.colNo.ReadOnly = true;
            //
            // colName
            //
            this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colName.DataPropertyName = "Name";
            this.colName.HeaderText = "Full Name";
            this.colName.MinimumWidth = 160;
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            //
            // colSpecialization
            //
            this.colSpecialization.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colSpecialization.DataPropertyName = "Specialization";
            this.colSpecialization.HeaderText = "Specialization";
            this.colSpecialization.Name = "colSpecialization";
            this.colSpecialization.ReadOnly = true;
            this.colSpecialization.Width = 150;
            //
            // colDepartment
            //
            this.colDepartment.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colDepartment.DataPropertyName = "Department";
            this.colDepartment.HeaderText = "Department";
            this.colDepartment.Name = "colDepartment";
            this.colDepartment.ReadOnly = true;
            this.colDepartment.Width = 140;
            //
            // colContact
            //
            this.colContact.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colContact.DataPropertyName = "Contact";
            this.colContact.HeaderText = "Contact";
            this.colContact.Name = "colContact";
            this.colContact.ReadOnly = true;
            this.colContact.Width = 125;
            //
            // colStatus
            //
            this.colStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colStatus.DataPropertyName = "Status";
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 85;
            //
            // colEdit
            //
            this.colEdit.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.colEdit.HeaderText = "Actions";
            this.colEdit.Name = "colEdit";
            this.colEdit.ReadOnly = true;
            this.colEdit.Text = "Edit";
            this.colEdit.UseColumnTextForButtonValue = true;
            this.colEdit.Width = 80;
            //
            // colAction
            //
            // Per-row action: text is set in CellFormatting to "Deactivate" for active
            // doctors and "Activate" for inactive ones (UseColumnTextForButtonValue off).
            this.colAction.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colAction.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.colAction.HeaderText = "";
            this.colAction.Name = "colAction";
            this.colAction.ReadOnly = true;
            //
            // DoctorsView
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.grid);
            this.Controls.Add(this.header);
            this.Name = "DoctorsView";
            this.Padding = new System.Windows.Forms.Padding(15);
            this.Size = new System.Drawing.Size(1024, 760);
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "colStatus" && e.Value != null)
            {
                e.CellStyle.ForeColor = e.Value.ToString() == "Active"
                    ? Color.FromArgb(5, 150, 105)
                    : Color.FromArgb(156, 163, 175);
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                return;
            }

            // Action button label depends on the row's status: active doctors can be
            // deactivated, inactive ones reactivated.
            if (colName == "colAction")
            {
                var statusObj = grid.Rows[e.RowIndex].Cells["colStatus"].Value;
                bool isActive = statusObj != null && statusObj.ToString() == "Active";
                e.Value = isActive ? "Deactivate" : "Activate";
                e.FormattingApplied = true;
            }
        }

        // ===================== Search box placeholder =====================
        // The placeholder text and grey colour are set in InitializeComponent.
        private void TxtSearch_Enter(object sender, EventArgs e)
        {
            if (searchPlaceholderActive)
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.FromArgb(17, 24, 39);
                searchPlaceholderActive = false;
            }
        }

        private void TxtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                ClearSearchBox();
        }

        private void ClearSearchBox()
        {
            txtSearch.Text = SearchPlaceholder;
            txtSearch.ForeColor = Color.Gray;
            searchPlaceholderActive = true;
        }

        private string GetSearchText() => searchPlaceholderActive ? "" : txtSearch.Text.Trim();

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadDoctors(GetSearchText());
            }
        }

        // ===================== List loading =====================
        private void LoadDoctors(string filter)
        {
            bool showInactive = chkShowInactive != null && chkShowInactive.Checked;
            var visible = showInactive ? HospitalData.AllDoctors() : HospitalData.ActiveDoctors();
            var source = visible.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                string f = filter.ToLower();
                source = source.Where(d =>
                    d.FullName.ToLower().Contains(f) ||
                    d.DoctorNo.ToLower().Contains(f) ||
                    (d.Specialization != null && d.Specialization.ToLower().Contains(f)) ||
                    HospitalData.DepartmentName(d.DepartmentId).ToLower().Contains(f) ||
                    (d.Contact != null && d.Contact.Contains(f)));
            }

            var rows = source
                .OrderBy(d => d.Id)
                .Select(d => new
                {
                    d.Id,
                    No = d.DoctorNo,
                    Name = d.FullName,
                    d.Specialization,
                    Department = HospitalData.DepartmentName(d.DepartmentId),
                    d.Contact,
                    d.Status
                }).ToList();

            // Rebinding a DataGridView straight to a fresh List<> (rather than a
            // BindingList/BindingSource) can leave the grid showing a stale subset
            // of rows after several rebinds - confirmed via live UI Automation on
            // PatientsView that Rows itself is always fully correct; the visible
            // viewport anchor (FirstDisplayedScrollingRowIndex) doesn't reset on its
            // own when a filtered (smaller) result set is replaced by a larger one.
            // Fully clearing the old binding, resetting the scroll anchor, then
            // forcing a repaint avoids it.
            grid.DataSource = null;
            grid.DataSource = rows;
            if (grid.Rows.Count > 0)
                grid.FirstDisplayedScrollingRowIndex = 0;
            grid.Refresh();

            // Never leave a row auto-selected/highlighted after a (re)load.
            grid.ClearSelection();
            if (grid.CurrentCell != null)
                grid.CurrentCell = null;

            string noun = rows.Count == 1 ? "doctor" : "doctors";
            lblCount.Text = $"Showing {rows.Count} of {visible.Count} {noun}";
        }

        // ===================== Row / action routing =====================
        // Exactly one doctor-related interface is open at a time:
        // clicking a row opens Details; Edit/Delete act directly from the list.
        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var idObj = grid.Rows[e.RowIndex].Cells["colId"].Value;
            if (idObj == null) return;
            int id = Convert.ToInt32(idObj);

            switch (grid.Columns[e.ColumnIndex].Name)
            {
                case "colEdit":
                    OpenEditForm(id);
                    break;
                case "colAction":
                    var doc = HospitalData.GetDoctor(id);
                    if (doc == null) return;
                    if (doc.IsActive)
                        DeactivateDoctorFlow(id);
                    else
                        ActivateDoctorFlow(id);
                    break;
                default:
                    ShowDetails(id);
                    break;
            }
        }

        private void ShowDetails(int id)
        {
            var d = HospitalData.GetDoctor(id);
            if (d == null) return;

            bool editRequested;
            using (var dlg = new DoctorDetailsDialog(d))
            {
                dlg.ShowDialog(this);
                editRequested = dlg.EditRequested;
            }

            // Details is fully closed before Edit ever opens - never shown together.
            if (editRequested)
                OpenEditForm(id);
        }

        private void OpenEditForm(int? id)
        {
            Doctor existing = id.HasValue ? HospitalData.GetDoctor(id.Value) : null;

            using (var dlg = new DoctorFormDialog(existing))
            {
                dlg.ShowDialog(this);
                if (dlg.Saved)
                    LoadDoctors(GetSearchText());
            }
        }

        private void DeactivateDoctorFlow(int id)
        {
            var d = HospitalData.GetDoctor(id);
            if (d == null) return;

            int upcoming = HospitalData.UpcomingAppointmentCountForDoctor(id);

            using (var dlg = new DeactivateDoctorDialog(d, upcoming))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                HospitalData.DeactivateDoctor(d, dlg.Reason);
            }

            MessageBox.Show("Doctor deactivated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadDoctors(GetSearchText());
        }

        private void ActivateDoctorFlow(int id)
        {
            var d = HospitalData.GetDoctor(id);
            if (d == null) return;

            if (MessageBox.Show($"Reactivate Dr. {d.FullName}? They will be selectable for appointments and admissions again.",
                "Confirm Reactivate", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            HospitalData.ActivateDoctor(d);
            MessageBox.Show("Doctor reactivated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadDoctors(GetSearchText());
        }

        // ===================== Deactivate Doctor dialog (modal) =====================
        // A reason is mandatory. Picking "Other" requires the free-text box to be filled.
        private class DeactivateDoctorDialog : Form
        {
            private const string OtherOption = "Other";
            private static readonly string[] Reasons =
            {
                "Transferred",
                "No longer affiliated",
                "Retired",
                "On extended leave",
                OtherOption
            };

            private ComboBox cmbReason;
            private TextBox txtOther;
            private Label lblOther;

            public string Reason { get; private set; }

            public DeactivateDoctorDialog(Doctor d, int upcomingAppointments)
            {
                this.Text = "Deactivate Doctor";
                this.Size = new Size(430, 300);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.White;

                Label lblTitle = new Label
                {
                    Text = "Deactivate Dr. " + d.FullName,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Location = new Point(20, 18),
                    AutoSize = true
                };
                this.Controls.Add(lblTitle);

                int y = 52;

                if (upcomingAppointments > 0)
                {
                    Label lblWarn = new Label
                    {
                        Text = $"⚠ This doctor has {upcomingAppointments} upcoming appointment(s). " +
                               "Please reassign or cancel them; deactivating takes the doctor off duty.",
                        Font = new Font("Segoe UI", 8.5F),
                        ForeColor = Color.FromArgb(180, 83, 9),
                        Location = new Point(20, y),
                        Size = new Size(385, 40)
                    };
                    this.Controls.Add(lblWarn);
                    y += 46;
                }

                Label lblReason = new Label
                {
                    Text = "Reason *",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(75, 85, 99),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                this.Controls.Add(lblReason);

                cmbReason = new ComboBox();
                cmbReason.Location = new Point(20, y + 22);
                cmbReason.Size = new Size(385, 28);
                cmbReason.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbReason.Font = new Font("Segoe UI", 10F);
                cmbReason.Items.AddRange(Reasons);
                cmbReason.SelectedIndex = -1;
                cmbReason.SelectedIndexChanged += (s, e) =>
                {
                    bool other = OtherOption.Equals(cmbReason.SelectedItem);
                    lblOther.Visible = other;
                    txtOther.Visible = other;
                    if (other) txtOther.Focus();
                };
                this.Controls.Add(cmbReason);
                y += 58;

                lblOther = new Label
                {
                    Text = "Please specify *",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(75, 85, 99),
                    Location = new Point(20, y),
                    AutoSize = true,
                    Visible = false
                };
                this.Controls.Add(lblOther);

                txtOther = new TextBox();
                txtOther.Location = new Point(20, y + 22);
                txtOther.Size = new Size(385, 28);
                txtOther.Font = new Font("Segoe UI", 10F);
                txtOther.Visible = false;
                this.Controls.Add(txtOther);

                Button btnConfirm = new Button();
                btnConfirm.Text = "Deactivate";
                btnConfirm.Size = new Size(130, 38);
                btnConfirm.Location = new Point(150, 215);
                btnConfirm.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnConfirm.BackColor = Color.FromArgb(220, 38, 38);
                btnConfirm.ForeColor = Color.White;
                btnConfirm.FlatStyle = FlatStyle.Flat;
                btnConfirm.FlatAppearance.BorderSize = 0;
                btnConfirm.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnConfirm.Cursor = Cursors.Hand;
                btnConfirm.Click += BtnConfirm_Click;
                this.Controls.Add(btnConfirm);

                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(100, 38);
                btnCancel.Location = new Point(290, 215);
                btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnCancel.BackColor = Color.FromArgb(229, 231, 235);
                btnCancel.ForeColor = Color.FromArgb(55, 65, 81);
                btnCancel.FlatStyle = FlatStyle.Flat;
                btnCancel.FlatAppearance.BorderSize = 0;
                btnCancel.Cursor = Cursors.Hand;
                btnCancel.DialogResult = DialogResult.Cancel;
                this.Controls.Add(btnCancel);

                this.CancelButton = btnCancel;
            }

            private void BtnConfirm_Click(object sender, EventArgs e)
            {
                if (cmbReason.SelectedItem == null)
                {
                    MessageBox.Show("Please choose a reason for deactivation.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbReason.Focus();
                    return;
                }

                string selected = cmbReason.SelectedItem.ToString();
                if (OtherOption.Equals(selected))
                {
                    if (string.IsNullOrWhiteSpace(txtOther.Text))
                    {
                        MessageBox.Show("Please specify the reason.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtOther.Focus();
                        return;
                    }
                    Reason = txtOther.Text.Trim();
                }
                else
                {
                    Reason = selected;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        // ===================== Doctor Details dialog (modal) =====================
        private class DoctorDetailsDialog : Form
        {
            public bool EditRequested { get; private set; } = false;

            public DoctorDetailsDialog(Doctor d)
            {
                this.Text = "Doctor Details";
                this.Size = new Size(440, 560);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.White;

                Panel headerBar = new Panel();
                headerBar.Dock = DockStyle.Top;
                headerBar.Height = 64;
                headerBar.BackColor = Color.FromArgb(30, 58, 138);
                this.Controls.Add(headerBar);

                Label lblName = new Label
                {
                    Text = "Dr. " + d.FullName,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    Location = new Point(20, 10),
                    AutoSize = true
                };
                headerBar.Controls.Add(lblName);

                Label lblNo = new Label
                {
                    Text = d.DoctorNo + "  •  " + d.Status + (d.IsOnDuty ? "  •  On Duty" : "  •  Off Duty"),
                    ForeColor = Color.FromArgb(191, 219, 254),
                    Font = new Font("Segoe UI", 9.5F),
                    Location = new Point(20, 36),
                    AutoSize = true
                };
                headerBar.Controls.Add(lblNo);

                Panel footer = new Panel();
                footer.Dock = DockStyle.Bottom;
                footer.Height = 58;
                this.Controls.Add(footer);

                var body = new TableLayoutPanel();
                body.Dock = DockStyle.Fill;
                body.ColumnCount = 2;
                body.Padding = new Padding(20, 15, 20, 15);
                body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                body.AutoSize = true;
                this.Controls.Add(body);
                body.BringToFront();

                int apptCount = HospitalData.AppointmentCountForDoctor(d.Id);
                int upcomingCount = HospitalData.UpcomingAppointmentCountForDoctor(d.Id);

                AddRow(body, "Doctor ID", d.DoctorNo);
                AddRow(body, "Full Name", "Dr. " + d.FullName);
                AddRow(body, "Specialization", d.Specialization ?? "-");
                AddRow(body, "Department", HospitalData.DepartmentName(d.DepartmentId));
                AddRow(body, "License Number", string.IsNullOrWhiteSpace(d.LicenseNumber) ? "-" : d.LicenseNumber);
                AddRow(body, "Credentials", string.IsNullOrWhiteSpace(d.Credentials) ? "-" : d.Credentials);
                AddRow(body, "Contact", d.Contact ?? "-");
                AddRow(body, "Status", d.Status);
                if (!d.IsActive && !string.IsNullOrWhiteSpace(d.DeactivationReason))
                    AddRow(body, "Deactivation Reason", d.DeactivationReason);
                AddRow(body, "Duty Status", d.IsOnDuty ? "On Duty" : "Off Duty");
                AddRow(body, "Total Appointments", apptCount.ToString());
                AddRow(body, "Upcoming Appointments", upcomingCount.ToString());

                Button btnEdit = new Button();
                btnEdit.Text = "Edit Doctor";
                btnEdit.Size = new Size(120, 36);
                btnEdit.Location = new Point(this.ClientSize.Width - 250, 11);
                btnEdit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnEdit.BackColor = Color.FromArgb(37, 99, 235);
                btnEdit.ForeColor = Color.White;
                btnEdit.FlatStyle = FlatStyle.Flat;
                btnEdit.FlatAppearance.BorderSize = 0;
                btnEdit.Cursor = Cursors.Hand;
                btnEdit.Click += (s, e) => { EditRequested = true; this.Close(); };
                if (Permissions.Can(Permission.ManageDoctors))
                    footer.Controls.Add(btnEdit);

                Button btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(100, 36);
                btnClose.Location = new Point(this.ClientSize.Width - 120, 11);
                btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnClose.BackColor = Color.FromArgb(229, 231, 235);
                btnClose.ForeColor = Color.FromArgb(55, 65, 81);
                btnClose.FlatStyle = FlatStyle.Flat;
                btnClose.FlatAppearance.BorderSize = 0;
                btnClose.Cursor = Cursors.Hand;
                btnClose.Click += (s, e) => this.Close();
                footer.Controls.Add(btnClose);

                this.CancelButton = btnClose;
            }

            private void AddRow(TableLayoutPanel body, string label, string value)
            {
                var lbl = new Label
                {
                    Text = label,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(75, 85, 99),
                    AutoSize = true,
                    Margin = new Padding(0, 6, 0, 6)
                };
                var val = new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    AutoSize = true,
                    Margin = new Padding(0, 6, 0, 6)
                };
                body.RowCount++;
                body.Controls.Add(lbl, 0, body.RowCount - 1);
                body.Controls.Add(val, 1, body.RowCount - 1);
            }
        }

        // ===================== Add / Update Doctor dialog (modal) =====================
        private class DoctorFormDialog : Form
        {
            private readonly int selectedId;
            private TextBox txtName, txtSpecialization, txtContact, txtLicense, txtCredentials;
            private ComboBox cmbDepartment;
            private CheckBox chkOnDuty;
            private Button btnSave, btnCancel;

            public bool Saved { get; private set; } = false;

            public DoctorFormDialog(Doctor existing)
            {
                selectedId = existing?.Id ?? 0;

                this.Text = existing == null ? "Add New Doctor" : "Update Doctor";
                this.Size = new Size(420, 640);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.White;

                Label lblTitle = new Label
                {
                    Text = existing == null ? "Add New Doctor" : "Update Doctor – " + existing.DoctorNo,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Location = new Point(25, 18),
                    AutoSize = true
                };
                this.Controls.Add(lblTitle);

                int y = 58;
                AddLabel("Full Name *", 25, y);
                txtName = AddTextBox(25, y + 22, 330);
                y += 65;

                AddLabel("Specialization", 25, y);
                txtSpecialization = AddTextBox(25, y + 22, 330);
                y += 65;

                AddLabel("License Number", 25, y);
                txtLicense = AddTextBox(25, y + 22, 330);
                y += 65;

                AddLabel("Credentials", 25, y);
                txtCredentials = AddTextBox(25, y + 22, 330);
                y += 65;

                AddLabel("Department *", 25, y);
                cmbDepartment = new ComboBox();
                cmbDepartment.Location = new Point(25, y + 22);
                cmbDepartment.Size = new Size(330, 28);
                cmbDepartment.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbDepartment.Font = new Font("Segoe UI", 10F);
                cmbDepartment.DataSource = HospitalData.Departments.ToList();
                cmbDepartment.DisplayMember = "Name";
                cmbDepartment.ValueMember = "Id";
                cmbDepartment.SelectedIndex = -1;
                this.Controls.Add(cmbDepartment);
                y += 65;

                AddLabel("Contact", 25, y);
                txtContact = AddTextBox(25, y + 22, 330);
                txtContact.MaxLength = 13;
                AddHint("e.g. 09171234567", 25, y + 52);
                y += 78;

                chkOnDuty = new CheckBox();
                chkOnDuty.Text = "Currently on duty";
                chkOnDuty.Location = new Point(25, y);
                chkOnDuty.AutoSize = true;
                chkOnDuty.Font = new Font("Segoe UI", 10F);
                this.Controls.Add(chkOnDuty);
                y += 45;

                btnSave = new Button();
                btnSave.Text = existing == null ? "Save Doctor" : "Save Changes";
                btnSave.Location = new Point(25, y);
                btnSave.Size = new Size(150, 40);
                btnSave.BackColor = Color.FromArgb(37, 99, 235);
                btnSave.ForeColor = Color.White;
                btnSave.FlatStyle = FlatStyle.Flat;
                btnSave.FlatAppearance.BorderSize = 0;
                btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnSave.Cursor = Cursors.Hand;
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Location = new Point(185, y);
                btnCancel.Size = new Size(110, 40);
                btnCancel.BackColor = Color.FromArgb(229, 231, 235);
                btnCancel.ForeColor = Color.FromArgb(55, 65, 81);
                btnCancel.FlatStyle = FlatStyle.Flat;
                btnCancel.FlatAppearance.BorderSize = 0;
                btnCancel.Font = new Font("Segoe UI", 10F);
                btnCancel.Cursor = Cursors.Hand;
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                if (existing != null)
                {
                    txtName.Text = existing.FullName;
                    txtSpecialization.Text = existing.Specialization ?? "";
                    txtLicense.Text = existing.LicenseNumber ?? "";
                    txtCredentials.Text = existing.Credentials ?? "";
                    txtContact.Text = existing.Contact ?? "";
                    cmbDepartment.SelectedValue = existing.DepartmentId;
                    chkOnDuty.Checked = existing.IsOnDuty;
                }

                this.AcceptButton = btnSave;
                this.CancelButton = btnCancel;
            }

            private void AddLabel(string text, int x, int y)
            {
                Label lbl = new Label();
                lbl.Text = text;
                lbl.Location = new Point(x, y);
                lbl.AutoSize = true;
                lbl.Font = new Font("Segoe UI", 9F);
                lbl.ForeColor = Color.FromArgb(75, 85, 99);
                this.Controls.Add(lbl);
            }

            private void AddHint(string text, int x, int y)
            {
                Label lbl = new Label();
                lbl.Text = text;
                lbl.Location = new Point(x, y);
                lbl.AutoSize = true;
                lbl.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
                lbl.ForeColor = Color.FromArgb(156, 163, 175);
                this.Controls.Add(lbl);
            }

            private TextBox AddTextBox(int x, int y, int width)
            {
                TextBox txt = new TextBox();
                txt.Location = new Point(x, y);
                txt.Size = new Size(width, 28);
                txt.Font = new Font("Segoe UI", 10F);
                this.Controls.Add(txt);
                return txt;
            }

            private bool ValidateForm(out string error, out Control focusTarget)
            {
                error = null;
                focusTarget = null;

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    error = "Full Name is required.";
                    focusTarget = txtName;
                    return false;
                }

                if (cmbDepartment.SelectedValue == null)
                {
                    error = "Please select a department.";
                    focusTarget = cmbDepartment;
                    return false;
                }

                string contact = txtContact.Text.Trim();
                if (!string.IsNullOrWhiteSpace(contact) && !PhoneRegex.IsMatch(contact))
                {
                    error = "Contact must be a valid PH mobile number, e.g. 09171234567 or +639171234567.";
                    focusTarget = txtContact;
                    return false;
                }

                return true;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                if (!ValidateForm(out string error, out Control focusTarget))
                {
                    MessageBox.Show(error, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    focusTarget?.Focus();
                    return;
                }

                int departmentId = Convert.ToInt32(cmbDepartment.SelectedValue);

                try
                {
                    if (selectedId == 0)
                    {
                        var created = HospitalData.AddDoctor(new Doctor
                        {
                            FullName = txtName.Text.Trim(),
                            DepartmentId = departmentId,
                            Specialization = txtSpecialization.Text.Trim(),
                            LicenseNumber = txtLicense.Text.Trim(),
                            Credentials = txtCredentials.Text.Trim(),
                            Contact = txtContact.Text.Trim(),
                            IsOnDuty = chkOnDuty.Checked
                        });
                        MessageBox.Show($"Doctor registered successfully as {created.DoctorNo}.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var d = HospitalData.GetDoctor(selectedId);
                        if (d != null)
                        {
                            d.FullName = txtName.Text.Trim();
                            d.DepartmentId = departmentId;
                            d.Specialization = txtSpecialization.Text.Trim();
                            d.LicenseNumber = txtLicense.Text.Trim();
                            d.Credentials = txtCredentials.Text.Trim();
                            d.Contact = txtContact.Text.Trim();
                            d.IsOnDuty = chkOnDuty.Checked;
                            HospitalData.UpdateDoctor(d);
                            MessageBox.Show("Doctor updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    MessageBox.Show("A database error occurred while saving this doctor:\n" + ex.Message,
                        "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Saved = true;
                this.Close();
            }
        }
    }
}

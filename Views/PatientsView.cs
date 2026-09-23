using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    public class PatientsView : UserControl
    {
        // Philippine mobile format: 09171234567 or +639171234567
        private static readonly Regex PhoneRegex = new Regex(@"^(09\d{9}|\+639\d{9})$");
        private const string SearchPlaceholder = "Search patients by ID, name, or contact...";

        private DataGridView grid;
        private TextBox txtSearch;
        private Button btnSearch, btnClear, btnNew;
        private Label lblCount;
        private Panel header;
        private Label lblList;
        private bool searchPlaceholderActive = true;

        public PatientsView()
        {
            InitializeComponent();

            // Pure UI, no DB - safe (and desirable) to run even at design time
            // so the Designer shows real grid columns and the search placeholder.
            BuildGridColumns();
            SetupSearchPlaceholder();
            WireEvents();
            grid.BringToFront();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
                LoadPatients("");
        }

        // Deliberately kept OUT of InitializeComponent(): the WinForms Designer
        // only tracks event/property wiring made through its own component model.
        // Anything wired here in plain code survives a future Designer save intact;
        // anything left inside InitializeComponent() risks being silently dropped
        // the next time the form is opened and saved in the Designer.
        private void WireEvents()
        {
            btnSearch.Click += BtnSearch_Click;
            btnClear.Click += BtnClear_Click;
            btnNew.Click += BtnNewPatient_Click;
            txtSearch.KeyDown += TxtSearch_KeyDown;
            grid.CellClick += Grid_CellClick;
            grid.CellFormatting += Grid_CellFormatting;
        }

        private void BtnSearch_Click(object sender, EventArgs e) => LoadPatients(GetSearchText());

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearSearchBox();
            LoadPatients("");
        }

        private void BtnNewPatient_Click(object sender, EventArgs e) => OpenEditForm(null);

        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.lblList = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.lblCount = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
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
            this.header.Controls.Add(this.lblCount);
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(15, 15);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(797, 90);
            this.header.TabIndex = 0;
            // 
            // lblList
            // 
            this.lblList.AutoSize = true;
            this.lblList.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblList.Location = new System.Drawing.Point(0, 0);
            this.lblList.Name = "lblList";
            this.lblList.Size = new System.Drawing.Size(107, 25);
            this.lblList.TabIndex = 0;
            this.lblList.Text = "Patient List";
            // 
            // txtSearch
            // 
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSearch.Location = new System.Drawing.Point(0, 36);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(360, 25);
            this.txtSearch.TabIndex = 1;
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
            this.btnNew.Text = "+ New Patient";
            this.btnNew.UseVisualStyleBackColor = false;
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = true;
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblCount.Location = new System.Drawing.Point(0, 72);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(0, 15);
            this.lblCount.TabIndex = 5;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(15, 15);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(797, 361);
            this.grid.TabIndex = 1;
            // 
            // PatientsView
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.grid);
            this.Controls.Add(this.header);
            this.grid.BringToFront();
            this.Name = "PatientsView";
            this.Padding = new System.Windows.Forms.Padding(15);
            this.Size = new System.Drawing.Size(827, 391);
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        private void BuildGridColumns()
        {
            // Must be false: this grid uses manually defined columns (including two
            // unbound Action button columns). Left as the DataGridView default (true),
            // the grid regenerates its own columns from DataSource on every bind,
            // scrambling column order and fighting these definitions.
            grid.AutoGenerateColumns = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "Id", Visible = false });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNo",
                HeaderText = "Patient No.",
                DataPropertyName = "No",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 100
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Full Name",
                DataPropertyName = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 180
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAge",
                HeaderText = "Age",
                DataPropertyName = "Age",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 55
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colGender",
                HeaderText = "Gender",
                DataPropertyName = "Gender",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 85
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colContact",
                HeaderText = "Contact",
                DataPropertyName = "Contact",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 125
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colBlood",
                HeaderText = "Blood Type",
                DataPropertyName = "Blood",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 85
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "Status",
                DataPropertyName = "Status",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 85
            });

            grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit",
                HeaderText = "Actions",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 80,
                FlatStyle = FlatStyle.Flat
            });
            grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 85,
                FlatStyle = FlatStyle.Flat
            });
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            e.CellStyle.ForeColor = e.Value.ToString() == "Active"
                ? Color.FromArgb(5, 150, 105)
                : Color.FromArgb(156, 163, 175);
            e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        // ===================== Search box placeholder =====================
        private void SetupSearchPlaceholder()
        {
            txtSearch.Text = SearchPlaceholder;
            txtSearch.ForeColor = Color.Gray;
            searchPlaceholderActive = true;

            txtSearch.Enter += (s, e) =>
            {
                if (searchPlaceholderActive)
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = Color.FromArgb(17, 24, 39);
                    searchPlaceholderActive = false;
                }
            };
            txtSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                    ClearSearchBox();
            };
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
                LoadPatients(GetSearchText());
            }
        }

        // ===================== List loading =====================
        private void LoadPatients(string filter)
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
                    p.Age,
                    p.Gender,
                    p.Contact,
                    Blood = p.BloodType,
                    p.Status
                }).ToList();

            // Rebinding a DataGridView straight to a fresh List<> (rather than a
            // BindingList/BindingSource) can leave the grid showing a stale subset
            // of rows after several rebinds - confirmed via live UI Automation that
            // Rows itself is always fully correct; the visible viewport anchor
            // (FirstDisplayedScrollingRowIndex) doesn't reset on its own when a
            // filtered (smaller) result set is replaced by a larger one, so the
            // grid can keep painting from a stale scroll offset left over from the
            // previous, smaller bind. Fully clearing the old binding, resetting the
            // scroll anchor, then forcing a repaint avoids it.
            grid.DataSource = null;
            grid.DataSource = rows;
            if (grid.Rows.Count > 0)
                grid.FirstDisplayedScrollingRowIndex = 0;
            grid.Refresh();

            // Never leave a row auto-selected/highlighted after a (re)load.
            grid.ClearSelection();
            if (grid.CurrentCell != null)
                grid.CurrentCell = null;

            string noun = rows.Count == 1 ? "patient" : "patients";
            lblCount.Text = $"Showing {rows.Count} of {active.Count} {noun}";
        }

        // ===================== Row / action routing =====================
        // Exactly one patient-related interface is open at a time:
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
                case "colDelete":
                    DeletePatientFlow(id);
                    break;
                default:
                    ShowDetails(id);
                    break;
            }
        }

        private void ShowDetails(int id)
        {
            var p = HospitalData.GetPatient(id);
            if (p == null) return;

            bool editRequested;
            using (var dlg = new PatientDetailsDialog(p))
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
            Patient existing = id.HasValue ? HospitalData.GetPatient(id.Value) : null;

            using (var dlg = new PatientFormDialog(existing))
            {
                dlg.ShowDialog(this);
                if (dlg.Saved)
                    LoadPatients(GetSearchText());
            }
        }

        private void DeletePatientFlow(int id)
        {
            var p = HospitalData.GetPatient(id);
            if (p == null) return;

            if (HospitalData.HasActiveAdmission(id))
            {
                MessageBox.Show(
                    "This patient currently has an active admission and must be discharged before they can be deleted.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int apptCount = HospitalData.AppointmentCountForPatient(id);
            int admCount = HospitalData.AdmissionCountForPatient(id);

            string message = "Are you sure you want to delete this patient?";
            if (apptCount > 0 || admCount > 0)
            {
                message += $"\n\nThis patient has {apptCount} appointment(s) and {admCount} admission(s) on record. " +
                           "To preserve that history, the patient will be deactivated instead of permanently deleted.";
            }

            if (MessageBox.Show(message, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            HospitalData.DeletePatient(p);
            MessageBox.Show("Patient deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadPatients(GetSearchText());
        }

        // ===================== Patient Details dialog (modal) =====================
        private class PatientDetailsDialog : Form
        {
            public bool EditRequested { get; private set; } = false;

            public PatientDetailsDialog(Patient p)
            {
                this.Text = "Patient Details";
                this.Size = new Size(440, 540);
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
                    Text = p.FullName,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    Location = new Point(20, 10),
                    AutoSize = true
                };
                headerBar.Controls.Add(lblName);

                Label lblNo = new Label
                {
                    Text = p.PatientNo + "  •  " + p.Status,
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

                int apptCount = HospitalData.AppointmentCountForPatient(p.Id);
                int admCount = HospitalData.AdmissionCountForPatient(p.Id);
                bool hasActiveAdmission = HospitalData.HasActiveAdmission(p.Id);

                AddRow(body, "Patient ID", p.PatientNo);
                AddRow(body, "Full Name", p.FullName);
                AddRow(body, "Age", p.Age.ToString());
                AddRow(body, "Gender", p.Gender ?? "-");
                AddRow(body, "Contact", p.Contact ?? "-");
                AddRow(body, "Address", string.IsNullOrWhiteSpace(p.Address) ? "-" : p.Address);
                AddRow(body, "Blood Type", p.BloodType ?? "-");
                AddRow(body, "Registered On", p.RegisteredOn.ToString("MMM dd, yyyy"));
                AddRow(body, "Number of Appointments", apptCount.ToString());
                AddRow(body, "Number of Admissions", admCount.ToString() + (hasActiveAdmission ? " (1 active)" : ""));

                Button btnEdit = new Button();
                btnEdit.Text = "Edit Patient";
                btnEdit.Size = new Size(120, 36);
                btnEdit.Location = new Point(this.ClientSize.Width - 250, 11);
                btnEdit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnEdit.BackColor = Color.FromArgb(37, 99, 235);
                btnEdit.ForeColor = Color.White;
                btnEdit.FlatStyle = FlatStyle.Flat;
                btnEdit.FlatAppearance.BorderSize = 0;
                btnEdit.Cursor = Cursors.Hand;
                btnEdit.Click += (s, e) => { EditRequested = true; this.Close(); };
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

        // ===================== Add / Update Patient dialog (modal) =====================
        private class PatientFormDialog : Form
        {
            private readonly int selectedId;
            private TextBox txtName, txtAge, txtContact, txtAddress;
            private ComboBox cmbGender, cmbBlood;
            private Button btnSave, btnCancel;

            public bool Saved { get; private set; } = false;

            public PatientFormDialog(Patient existing)
            {
                selectedId = existing?.Id ?? 0;

                this.Text = existing == null ? "Add New Patient" : "Update Patient";
                this.Size = new Size(420, 500);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.White;

                Label lblTitle = new Label
                {
                    Text = existing == null ? "Add New Patient" : "Update Patient – " + existing.PatientNo,
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

                AddLabel("Age *", 25, y);
                txtAge = AddTextBox(25, y + 22, 100);

                AddLabel("Gender *", 150, y);
                cmbGender = new ComboBox();
                cmbGender.Location = new Point(150, y + 22);
                cmbGender.Size = new Size(205, 28);
                cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbGender.Font = new Font("Segoe UI", 10F);
                cmbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
                this.Controls.Add(cmbGender);
                y += 65;

                AddLabel("Contact *", 25, y);
                txtContact = AddTextBox(25, y + 22, 330);
                txtContact.MaxLength = 13;
                AddHint("e.g. 09171234567", 25, y + 52);
                y += 78;

                AddLabel("Address", 25, y);
                txtAddress = AddTextBox(25, y + 22, 330);
                y += 65;

                AddLabel("Blood Type", 25, y);
                cmbBlood = new ComboBox();
                cmbBlood.Location = new Point(25, y + 22);
                cmbBlood.Size = new Size(130, 28);
                cmbBlood.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbBlood.Font = new Font("Segoe UI", 10F);
                cmbBlood.Items.AddRange(new object[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" });
                this.Controls.Add(cmbBlood);
                y += 80;

                btnSave = new Button();
                btnSave.Text = existing == null ? "Save Patient" : "Save Changes";
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
                    txtAge.Text = existing.Age.ToString();
                    txtContact.Text = existing.Contact ?? "";
                    txtAddress.Text = existing.Address ?? "";
                    cmbGender.SelectedItem = existing.Gender;
                    cmbBlood.SelectedItem = existing.BloodType;
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

                if (!int.TryParse(txtAge.Text.Trim(), out int age) || age <= 0 || age > 120)
                {
                    error = "Please enter a valid age (1-120).";
                    focusTarget = txtAge;
                    return false;
                }

                if (cmbGender.SelectedIndex == -1)
                {
                    error = "Please select a gender.";
                    focusTarget = cmbGender;
                    return false;
                }

                string contact = txtContact.Text.Trim();
                if (string.IsNullOrWhiteSpace(contact))
                {
                    error = "Contact number is required.";
                    focusTarget = txtContact;
                    return false;
                }
                if (!PhoneRegex.IsMatch(contact))
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

                int age = int.Parse(txtAge.Text.Trim());

                try
                {
                    if (selectedId == 0)
                    {
                        var created = HospitalData.AddPatient(new Patient
                        {
                            FullName = txtName.Text.Trim(),
                            Age = age,
                            Gender = cmbGender.SelectedItem.ToString(),
                            Contact = txtContact.Text.Trim(),
                            Address = txtAddress.Text.Trim(),
                            BloodType = cmbBlood.SelectedItem != null ? cmbBlood.SelectedItem.ToString() : null
                        });
                        MessageBox.Show($"Patient registered successfully as {created.PatientNo}.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var p = HospitalData.GetPatient(selectedId);
                        if (p != null)
                        {
                            p.FullName = txtName.Text.Trim();
                            p.Age = age;
                            p.Gender = cmbGender.SelectedItem.ToString();
                            p.Contact = txtContact.Text.Trim();
                            p.Address = txtAddress.Text.Trim();
                            p.BloodType = cmbBlood.SelectedItem != null ? cmbBlood.SelectedItem.ToString() : null;
                            HospitalData.UpdatePatient(p);
                            MessageBox.Show("Patient updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    MessageBox.Show("A database error occurred while saving this patient:\n" + ex.Message,
                        "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Saved = true;
                this.Close();
            }
        }
    }
}

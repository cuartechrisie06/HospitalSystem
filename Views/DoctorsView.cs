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
        private TextBox txtSearch;
        private Button btnSearch, btnClear, btnNew;
        private Label lblCount;
        private bool searchPlaceholderActive = true;

        public DoctorsView()
        {
            InitializeComponent();

            // Pure UI, no DB - safe (and desirable) to run even at design time
            // so the Designer shows real grid columns and the search placeholder.
            BuildGridColumns();
            SetupSearchPlaceholder();
            WireEvents();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
                LoadDoctors("");
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
            btnNew.Click += BtnNewDoctor_Click;
            txtSearch.KeyDown += TxtSearch_KeyDown;
            grid.CellClick += Grid_CellClick;
            grid.CellFormatting += Grid_CellFormatting;
        }

        private void BtnSearch_Click(object sender, EventArgs e) => LoadDoctors(GetSearchText());

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearSearchBox();
            LoadDoctors("");
        }

        private void BtnNewDoctor_Click(object sender, EventArgs e) => OpenEditForm(null);

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Padding = new Padding(15);

            // ===================== Header: title + search + actions =====================
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 90;
            this.Controls.Add(header);

            Label lblList = new Label();
            lblList.Text = "Doctors";
            lblList.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblList.ForeColor = Color.FromArgb(30, 41, 59);
            lblList.Location = new Point(0, 0);
            lblList.AutoSize = true;
            header.Controls.Add(lblList);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(0, 36);
            txtSearch.Size = new Size(360, 30);
            txtSearch.Font = new Font("Segoe UI", 10F);
            header.Controls.Add(txtSearch);

            btnSearch = new Button();
            btnSearch.Text = "Search";
            btnSearch.Location = new Point(368, 35);
            btnSearch.Size = new Size(85, 32);
            btnSearch.BackColor = Color.FromArgb(37, 99, 235);
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Cursor = Cursors.Hand;
            header.Controls.Add(btnSearch);

            btnClear = new Button();
            btnClear.Text = "Clear";
            btnClear.Location = new Point(461, 35);
            btnClear.Size = new Size(75, 32);
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Cursor = Cursors.Hand;
            header.Controls.Add(btnClear);

            btnNew = new Button();
            btnNew.Text = "+ New Doctor";
            btnNew.Location = new Point(544, 35);
            btnNew.Size = new Size(140, 32);
            btnNew.BackColor = Color.FromArgb(5, 150, 105);
            btnNew.ForeColor = Color.White;
            btnNew.FlatStyle = FlatStyle.Flat;
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnNew.Cursor = Cursors.Hand;
            header.Controls.Add(btnNew);

            lblCount = new Label();
            lblCount.Location = new Point(0, 72);
            lblCount.AutoSize = true;
            lblCount.Font = new Font("Segoe UI", 8.5F);
            lblCount.ForeColor = Color.FromArgb(107, 114, 128);
            header.Controls.Add(lblCount);

            // ===================== Doctor List (full width) =====================
            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.AutoGenerateColumns = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.AllowUserToResizeRows = false;
            this.Controls.Add(grid);
            grid.BringToFront();
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
                HeaderText = "Doctor ID",
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
                MinimumWidth = 160
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSpecialization",
                HeaderText = "Specialization",
                DataPropertyName = "Specialization",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 150
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDepartment",
                HeaderText = "Department",
                DataPropertyName = "Department",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 140
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
                LoadDoctors(GetSearchText());
            }
        }

        // ===================== List loading =====================
        private void LoadDoctors(string filter)
        {
            var active = HospitalData.ActiveDoctors();
            var source = active.AsEnumerable();
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
            lblCount.Text = $"Showing {rows.Count} of {active.Count} {noun}";
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
                case "colDelete":
                    DeleteDoctorFlow(id);
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

        private void DeleteDoctorFlow(int id)
        {
            var d = HospitalData.GetDoctor(id);
            if (d == null) return;

            if (HospitalData.UpcomingAppointmentCountForDoctor(id) > 0)
            {
                MessageBox.Show(
                    "This doctor has upcoming appointment(s) scheduled and cannot be deleted until those are completed or cancelled.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int apptCount = HospitalData.AppointmentCountForDoctor(id);
            int admCount = HospitalData.AdmissionCountForDoctor(id);

            string message = "Are you sure you want to delete this doctor?";
            if (apptCount > 0 || admCount > 0)
            {
                message += $"\n\nThis doctor has {apptCount} appointment(s) and {admCount} admission(s) on record. " +
                           "To preserve that history, the doctor will be deactivated instead of permanently deleted.";
            }

            if (MessageBox.Show(message, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            HospitalData.DeleteDoctor(d);
            MessageBox.Show("Doctor deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadDoctors(GetSearchText());
        }

        // ===================== Doctor Details dialog (modal) =====================
        private class DoctorDetailsDialog : Form
        {
            public bool EditRequested { get; private set; } = false;

            public DoctorDetailsDialog(Doctor d)
            {
                this.Text = "Doctor Details";
                this.Size = new Size(440, 480);
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
                AddRow(body, "Contact", d.Contact ?? "-");
                AddRow(body, "Status", d.Status);
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
            private TextBox txtName, txtSpecialization, txtContact;
            private ComboBox cmbDepartment;
            private CheckBox chkOnDuty;
            private Button btnSave, btnCancel;

            public bool Saved { get; private set; } = false;

            public DoctorFormDialog(Doctor existing)
            {
                selectedId = existing?.Id ?? 0;

                this.Text = existing == null ? "Add New Doctor" : "Update Doctor";
                this.Size = new Size(420, 500);
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // Audit trail of every action in every module. Administrators only: the sidebar hides
    // the button for other roles, and the view refuses to load for them as well.
    public class ActivityLogView : UserControl
    {
        private const string All = "(All)";
        private const int MaxRows = 2000;

        private Panel filters;
        private Label lblTitle;
        private Label lblFrom;
        private DateTimePicker dtFrom;
        private Label lblTo;
        private DateTimePicker dtTo;
        private Label lblModule;
        private ComboBox cmbModule;
        private Label lblUser;
        private ComboBox cmbUser;
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnToday;
        private Button btnExport;
        private Label lblCount;
        private DataGridView grid;
        private Label lblAccessDenied;
        private List<ActivityItem> rows = new List<ActivityItem>();

        public ActivityLogView()
        {
            // Every control and event is set up in InitializeComponent() (Designer format),
            // so the Designer shows the complete screen.
            InitializeComponent();

            if (!DesignTimeHelper.IsDesignMode)
            {
                if (!HospitalData.IsAdmin)
                {
                    ShowAccessDenied();
                    return;
                }
                // Default range: the last 7 days (dates can't be expressed in designer code).
                dtFrom.Value = DateTime.Today.AddDays(-7);
                dtTo.Value = DateTime.Today;
                LoadFilters();
                LoadLog();
            }
        }

        // Designer-generated layout: the filter card on top, the entry count, then the log grid.
        // lblAccessDenied is hidden unless a non-administrator somehow opens the view.
        private void InitializeComponent()
        {
            this.filters = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblFrom = new System.Windows.Forms.Label();
            this.dtFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtTo = new System.Windows.Forms.DateTimePicker();
            this.lblModule = new System.Windows.Forms.Label();
            this.cmbModule = new System.Windows.Forms.ComboBox();
            this.lblUser = new System.Windows.Forms.Label();
            this.cmbUser = new System.Windows.Forms.ComboBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnToday = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.lblCount = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.lblAccessDenied = new System.Windows.Forms.Label();
            this.filters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            //
            // filters
            //
            this.filters.BackColor = System.Drawing.Color.White;
            this.filters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.filters.Controls.Add(this.lblTitle);
            this.filters.Controls.Add(this.lblFrom);
            this.filters.Controls.Add(this.dtFrom);
            this.filters.Controls.Add(this.lblTo);
            this.filters.Controls.Add(this.dtTo);
            this.filters.Controls.Add(this.lblModule);
            this.filters.Controls.Add(this.cmbModule);
            this.filters.Controls.Add(this.lblUser);
            this.filters.Controls.Add(this.cmbUser);
            this.filters.Controls.Add(this.lblSearch);
            this.filters.Controls.Add(this.txtSearch);
            this.filters.Controls.Add(this.btnSearch);
            this.filters.Controls.Add(this.btnToday);
            this.filters.Controls.Add(this.btnExport);
            this.filters.Dock = System.Windows.Forms.DockStyle.Top;
            this.filters.Location = new System.Drawing.Point(10, 10);
            this.filters.Name = "filters";
            this.filters.Padding = new System.Windows.Forms.Padding(15);
            this.filters.Size = new System.Drawing.Size(1004, 105);
            this.filters.TabIndex = 0;
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(15, 8);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(99, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Activity Log";
            //
            // lblFrom
            //
            this.lblFrom.AutoSize = true;
            this.lblFrom.Location = new System.Drawing.Point(15, 42);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(30, 13);
            this.lblFrom.TabIndex = 1;
            this.lblFrom.Text = "From";
            //
            // dtFrom
            //
            this.dtFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFrom.Location = new System.Drawing.Point(15, 60);
            this.dtFrom.Name = "dtFrom";
            this.dtFrom.Size = new System.Drawing.Size(110, 20);
            this.dtFrom.TabIndex = 2;
            //
            // lblTo
            //
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(135, 42);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(20, 13);
            this.lblTo.TabIndex = 3;
            this.lblTo.Text = "To";
            //
            // dtTo
            //
            this.dtTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtTo.Location = new System.Drawing.Point(135, 60);
            this.dtTo.Name = "dtTo";
            this.dtTo.Size = new System.Drawing.Size(110, 20);
            this.dtTo.TabIndex = 4;
            //
            // lblModule
            //
            this.lblModule.AutoSize = true;
            this.lblModule.Location = new System.Drawing.Point(255, 42);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(42, 13);
            this.lblModule.TabIndex = 5;
            this.lblModule.Text = "Module";
            //
            // cmbModule
            //
            this.cmbModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModule.Location = new System.Drawing.Point(255, 60);
            this.cmbModule.Name = "cmbModule";
            this.cmbModule.Size = new System.Drawing.Size(130, 21);
            this.cmbModule.TabIndex = 6;
            //
            // lblUser
            //
            this.lblUser.AutoSize = true;
            this.lblUser.Location = new System.Drawing.Point(395, 42);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(29, 13);
            this.lblUser.TabIndex = 7;
            this.lblUser.Text = "User";
            //
            // cmbUser
            //
            this.cmbUser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUser.Location = new System.Drawing.Point(395, 60);
            this.cmbUser.Name = "cmbUser";
            this.cmbUser.Size = new System.Drawing.Size(120, 21);
            this.cmbUser.TabIndex = 8;
            //
            // lblSearch
            //
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(525, 42);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(41, 13);
            this.lblSearch.TabIndex = 9;
            this.lblSearch.Text = "Search";
            //
            // txtSearch
            //
            this.txtSearch.Location = new System.Drawing.Point(525, 60);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(170, 20);
            this.txtSearch.TabIndex = 10;
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtSearch_KeyDown);
            //
            // btnSearch
            //
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.Location = new System.Drawing.Point(705, 58);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(80, 28);
            this.btnSearch.TabIndex = 11;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = false;
            this.btnSearch.Click += new System.EventHandler(this.BtnSearch_Click);
            //
            // btnToday
            //
            this.btnToday.Location = new System.Drawing.Point(790, 58);
            this.btnToday.Name = "btnToday";
            this.btnToday.Size = new System.Drawing.Size(70, 28);
            this.btnToday.TabIndex = 12;
            this.btnToday.Text = "Today";
            this.btnToday.UseVisualStyleBackColor = true;
            this.btnToday.Click += new System.EventHandler(this.BtnToday_Click);
            //
            // btnExport
            //
            this.btnExport.Location = new System.Drawing.Point(865, 58);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(105, 28);
            this.btnExport.TabIndex = 13;
            this.btnExport.Text = "Export CSV...";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.BtnExport_Click);
            //
            // lblCount
            //
            this.lblCount.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCount.Location = new System.Drawing.Point(10, 115);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(1004, 28);
            this.lblCount.TabIndex = 1;
            this.lblCount.Text = "Entries";
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(10, 143);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1004, 607);
            this.grid.TabIndex = 2;
            //
            // lblAccessDenied
            //
            this.lblAccessDenied.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAccessDenied.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblAccessDenied.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.lblAccessDenied.Location = new System.Drawing.Point(10, 10);
            this.lblAccessDenied.Name = "lblAccessDenied";
            this.lblAccessDenied.Size = new System.Drawing.Size(1004, 740);
            this.lblAccessDenied.TabIndex = 3;
            this.lblAccessDenied.Text = "The activity log is available to administrators only.";
            this.lblAccessDenied.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblAccessDenied.Visible = false;
            //
            // ActivityLogView
            //
            // Fill first, then bottom-to-top: the last one added docks at the very top.
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.lblAccessDenied);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.filters);
            this.Name = "ActivityLogView";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(1024, 760);
            this.filters.ResumeLayout(false);
            this.filters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        private void BtnSearch_Click(object sender, EventArgs e) => LoadLog();

        private void BtnToday_Click(object sender, EventArgs e)
        {
            dtFrom.Value = DateTime.Today;
            dtTo.Value = DateTime.Today;
            LoadLog();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadLog();
            }
        }

        private void ShowAccessDenied()
        {
            filters.Visible = false;
            lblCount.Visible = false;
            grid.Visible = false;
            lblAccessDenied.Visible = true;
        }

        private void LoadFilters()
        {
            var modules = new List<string> { All };
            modules.AddRange(HospitalData.ActivityLogModules());
            cmbModule.DataSource = modules;

            var users = new List<string> { All, HospitalData.SystemUser };
            users.AddRange(HospitalData.Users.Select(u => u.Username));
            cmbUser.DataSource = users;
        }

        private void LoadLog()
        {
            DateTime from = dtFrom.Value.Date;
            DateTime to = dtTo.Value.Date.AddDays(1);   // include the whole "to" day
            if (to <= from)
            {
                MessageBox.Show("The 'To' date must be on or after the 'From' date.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string module = cmbModule.SelectedItem as string;
            string user = cmbUser.SelectedItem as string;

            rows = HospitalData.QueryActivityLog(from, to,
                module == All ? null : module,
                user == All ? null : user,
                txtSearch.Text, MaxRows);

            grid.DataSource = rows.Select(a => new
            {
                Time = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                User = a.Username,
                a.Module,
                a.Action,
                Details = a.Description
            }).ToList();

            if (grid.Columns["Details"] != null)
                grid.Columns["Details"].FillWeight = 300;

            lblCount.Text = rows.Count >= MaxRows
                ? $"Showing the latest {MaxRows} entries. Narrow the filters to see older ones."
                : $"{rows.Count} entr{(rows.Count == 1 ? "y" : "ies")} from {from:MMM dd, yyyy} to {dtTo.Value:MMM dd, yyyy}";
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (rows.Count == 0)
            {
                MessageBox.Show("There is nothing to export for the current filters.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV files (*.csv)|*.csv";
                dlg.FileName = $"activity-log-{dtFrom.Value:yyyyMMdd}-{dtTo.Value:yyyyMMdd}.csv";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Time,User,Module,Action,Details");
                foreach (var a in rows)
                    sb.AppendLine(string.Join(",", new[]
                    {
                        a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), a.Username, a.Module, a.Action, a.Description
                    }.Select(Csv)));
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));

                HospitalData.LogActivity("Activity Log", "Exported",
                    $"Exported {rows.Count} activity log entries ({dtFrom.Value:yyyy-MM-dd} to {dtTo.Value:yyyy-MM-dd})", "📤");
                MessageBox.Show($"Exported {rows.Count} entries to\n{dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}

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

        private DateTimePicker dtFrom, dtTo;
        private ComboBox cmbModule, cmbUser;
        private TextBox txtSearch;
        private Button btnSearch, btnToday, btnExport;
        private Label lblCount;
        private DataGridView grid;
        private List<ActivityItem> rows = new List<ActivityItem>();

        public ActivityLogView()
        {
            InitializeComponent();

            if (!DesignTimeHelper.IsDesignMode)
            {
                if (!HospitalData.IsAdmin)
                {
                    ShowAccessDenied();
                    return;
                }
                LoadFilters();
                LoadLog();
            }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Padding = new Padding(10);

            Panel filters = new Panel();
            filters.Dock = DockStyle.Top;
            filters.Height = 105;
            filters.BackColor = Color.White;
            filters.BorderStyle = BorderStyle.FixedSingle;
            filters.Padding = new Padding(15);
            this.Controls.Add(filters);

            Label title = new Label();
            title.Text = "Activity Log";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.Location = new Point(15, 8);
            title.AutoSize = true;
            filters.Controls.Add(title);

            filters.Controls.Add(MakeLabel("From", 15, 42));
            dtFrom = new DateTimePicker();
            dtFrom.Format = DateTimePickerFormat.Short;
            dtFrom.Location = new Point(15, 60);
            dtFrom.Size = new Size(110, 28);
            dtFrom.Value = DateTime.Today.AddDays(-7);
            filters.Controls.Add(dtFrom);

            filters.Controls.Add(MakeLabel("To", 135, 42));
            dtTo = new DateTimePicker();
            dtTo.Format = DateTimePickerFormat.Short;
            dtTo.Location = new Point(135, 60);
            dtTo.Size = new Size(110, 28);
            dtTo.Value = DateTime.Today;
            filters.Controls.Add(dtTo);

            filters.Controls.Add(MakeLabel("Module", 255, 42));
            cmbModule = new ComboBox();
            cmbModule.Location = new Point(255, 60);
            cmbModule.Size = new Size(130, 28);
            cmbModule.DropDownStyle = ComboBoxStyle.DropDownList;
            filters.Controls.Add(cmbModule);

            filters.Controls.Add(MakeLabel("User", 395, 42));
            cmbUser = new ComboBox();
            cmbUser.Location = new Point(395, 60);
            cmbUser.Size = new Size(120, 28);
            cmbUser.DropDownStyle = ComboBoxStyle.DropDownList;
            filters.Controls.Add(cmbUser);

            filters.Controls.Add(MakeLabel("Search", 525, 42));
            txtSearch = new TextBox();
            txtSearch.Location = new Point(525, 60);
            txtSearch.Size = new Size(170, 28);
            filters.Controls.Add(txtSearch);

            btnSearch = MakeButton("Search", 705, 58, 80);
            btnSearch.BackColor = Color.FromArgb(37, 99, 235);
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Click += (s, e) => LoadLog();
            filters.Controls.Add(btnSearch);

            btnToday = MakeButton("Today", 790, 58, 70);
            btnToday.Click += (s, e) => { dtFrom.Value = DateTime.Today; dtTo.Value = DateTime.Today; LoadLog(); };
            filters.Controls.Add(btnToday);

            btnExport = MakeButton("Export CSV...", 865, 58, 105);
            btnExport.Click += BtnExport_Click;
            filters.Controls.Add(btnExport);

            lblCount = new Label();
            lblCount.Dock = DockStyle.Top;
            lblCount.Height = 28;
            lblCount.TextAlign = ContentAlignment.BottomLeft;
            lblCount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.Controls.Add(lblCount);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            this.Controls.Add(grid);
            grid.BringToFront();     // docks last: fills what the header rows leave
            filters.SendToBack();    // docks first: topmost, above the count label

            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadLog(); } };
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true };
        }

        private static Button MakeButton(string text, int x, int y, int width)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 28) };
        }

        private void ShowAccessDenied()
        {
            Controls.Clear();
            Controls.Add(new Label
            {
                Text = "The activity log is available to administrators only.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28)
            });
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

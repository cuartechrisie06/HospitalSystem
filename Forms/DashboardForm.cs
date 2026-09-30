using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Views;
using HospitalSystem.Models;

namespace HospitalSystem.Forms
{
    public class DashboardForm : Form
    {
        private Panel panelSidebar;
        private Panel panelContent;
        private Label lblUser;
        private Button btnDashboard;
        private Button btnPatients;
        private Button btnDoctors;
        private Button btnAppointments;
        private Button btnAdmissions;
        private Button btnBilling;
        private Button btnActivityLog;
        private Panel sepActivityLog;
        private Button btnSignOut;
        private UserControl currentView;

        public DashboardForm()
        {
            InitializeComponent();

            // Pure UI, no DB - safe (and desirable) to build even at design time
            // so the sidebar actually renders in the Designer.
            BuildSidebarNav();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                ApplyUserAccess();
                ShowOverview();
            }
        }

        // Builds the sidebar nav buttons via the CreateNavButton() helper.
        // Kept out of InitializeComponent(): the Designer's InitializeComponent
        // parser only understands flat control-creation statements, not calls
        // into custom factory methods.
        private void BuildSidebarNav()
        {
            btnDashboard = CreateNavButton("Dashboard", 70);
            btnDashboard.Click += BtnDashboard_Click;
            panelSidebar.Controls.Add(btnDashboard);

            btnPatients = CreateNavButton("Patients", 120);
            btnPatients.Click += BtnPatients_Click;
            panelSidebar.Controls.Add(btnPatients);

            btnDoctors = CreateNavButton("Doctors", 170);
            btnDoctors.Click += BtnDoctors_Click;
            panelSidebar.Controls.Add(btnDoctors);

            btnAppointments = CreateNavButton("Appointments", 220);
            btnAppointments.Click += BtnAppointments_Click;
            panelSidebar.Controls.Add(btnAppointments);

            btnAdmissions = CreateNavButton("Admissions", 270);
            btnAdmissions.Click += BtnAdmissions_Click;
            panelSidebar.Controls.Add(btnAdmissions);

            btnBilling = CreateNavButton("Billing", 320);
            btnBilling.Click += BtnBilling_Click;
            panelSidebar.Controls.Add(btnBilling);

            // Administrators only; shown/hidden by ApplyUserAccess() on every sign-in.
            btnActivityLog = CreateNavButton("Activity Log", 370);
            btnActivityLog.Click += BtnActivityLog_Click;
            btnActivityLog.Visible = false;
            panelSidebar.Controls.Add(btnActivityLog);

            AddSidebarSeparators();
        }

        // Thin divider lines in the sidebar.
        // Purely decorative: these are 1px panels layered into the existing gaps,
        // so no button position, click handler or data path is touched.
        private void AddSidebarSeparators()
        {
            // under the "Hospital System" title (title ends at 60, first button starts at 70)
            panelSidebar.Controls.Add(CreateSeparator(64, true));

            // between the nav buttons - each is 42px tall with an 8px gap after it
            panelSidebar.Controls.Add(CreateSeparator(116, false));
            panelSidebar.Controls.Add(CreateSeparator(166, false));
            panelSidebar.Controls.Add(CreateSeparator(216, false));
            panelSidebar.Controls.Add(CreateSeparator(266, false));
            panelSidebar.Controls.Add(CreateSeparator(316, false));

            sepActivityLog = CreateSeparator(366, false);
            sepActivityLog.Visible = false;
            panelSidebar.Controls.Add(sepActivityLog);

            // above the user / sign-out block pinned at the bottom
            Panel bottomLine = new Panel();
            bottomLine.Height = 1;
            bottomLine.Dock = DockStyle.Bottom;
            bottomLine.BackColor = Color.FromArgb(78, 115, 205);
            panelSidebar.Controls.Add(bottomLine);

            // dock last so it lands above lblUser and btnSignOut, not below them
            bottomLine.BringToFront();
        }

        private Panel CreateSeparator(int top, bool strong)
        {
            Panel line = new Panel();
            line.Height = 1;
            line.Left = 16;
            line.Width = panelSidebar.Width - 32;
            line.Top = top;
            line.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            line.BackColor = strong
                ? Color.FromArgb(78, 115, 205)
                : Color.FromArgb(52, 82, 165);
            return line;
        }

        // Shows who is signed in and which modules their role can open.
        private void ApplyUserAccess()
        {
            var user = HospitalData.CurrentUser;
            lblUser.Text = user != null ? user.DisplayName + "\n" + user.Role : "User";

            bool admin = HospitalData.IsAdmin;
            btnActivityLog.Visible = admin;
            sepActivityLog.Visible = admin;
        }

        private void DashboardForm_Load(object sender, EventArgs e)
        {
            if (DesignTimeHelper.IsDesignMode) return;
            ApplyUserAccess();
            ShowOverview();
        }

        private void BtnDashboard_Click(object sender, EventArgs e) => ShowOverview();
        private void BtnPatients_Click(object sender, EventArgs e) => ShowView(new Views.PatientsView());
        private void BtnDoctors_Click(object sender, EventArgs e) => ShowView(new Views.DoctorsView());
        private void BtnAppointments_Click(object sender, EventArgs e) => ShowView(new Views.AppointmentsView());
        private void BtnAdmissions_Click(object sender, EventArgs e) => ShowView(new Views.AdmissionsView());
        private void BtnBilling_Click(object sender, EventArgs e) => ShowView(new Views.BillingView());

        private void BtnActivityLog_Click(object sender, EventArgs e)
        {
            if (!HospitalData.IsAdmin) return;   // the button is hidden for other roles anyway
            ShowView(new Views.ActivityLogView());
        }

        // Jump from an alert to the module that raised it.
        private void OpenModule(string module)
        {
            switch (module)
            {
                case "Admissions": ShowView(new Views.AdmissionsView()); break;
                case "Doctors": ShowView(new Views.DoctorsView()); break;
                case "Appointments": ShowView(new Views.AppointmentsView()); break;
                case "Billing": ShowView(new Views.BillingView()); break;
                case "Patients": ShowView(new Views.PatientsView()); break;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Hospital System";
            this.Size = new Size(1280, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 750);
            this.BackColor = Color.FromArgb(243, 244, 246);

            // ===== Sidebar =====
            panelSidebar = new Panel();
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Width = 220;
            panelSidebar.BackColor = Color.FromArgb(30, 58, 138);
            this.Controls.Add(panelSidebar);

            Label lblBrand = new Label();
            lblBrand.Text = "  Hospital System";
            lblBrand.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblBrand.ForeColor = Color.White;
            lblBrand.Dock = DockStyle.Top;
            lblBrand.Height = 60;
            lblBrand.TextAlign = ContentAlignment.MiddleLeft;
            panelSidebar.Controls.Add(lblBrand);

            btnSignOut = new Button();
            btnSignOut.Text = "  Sign Out";
            btnSignOut.FlatStyle = FlatStyle.Flat;
            btnSignOut.FlatAppearance.BorderSize = 0;
            btnSignOut.BackColor = Color.FromArgb(30, 58, 138);
            btnSignOut.ForeColor = Color.White;
            btnSignOut.Font = new Font("Segoe UI", 10F);
            btnSignOut.TextAlign = ContentAlignment.MiddleLeft;
            btnSignOut.Height = 45;
            btnSignOut.Dock = DockStyle.Bottom;
            btnSignOut.Cursor = Cursors.Hand;
            btnSignOut.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
            btnSignOut.Click += BtnSignOut_Click;
            panelSidebar.Controls.Add(btnSignOut);

            lblUser = new Label();
            lblUser.Dock = DockStyle.Bottom;
            lblUser.Height = 44;
            lblUser.ForeColor = Color.LightGray;
            lblUser.Font = new Font("Segoe UI", 9F);
            lblUser.TextAlign = ContentAlignment.MiddleCenter;
            lblUser.Text = "User";
            panelSidebar.Controls.Add(lblUser);

            // ===== Content area =====
            panelContent = new Panel();
            panelContent.Dock = DockStyle.Fill;
            panelContent.Padding = new Padding(18);
            panelContent.BackColor = Color.FromArgb(243, 244, 246);
            this.Controls.Add(panelContent);

            this.Load += DashboardForm_Load;
            panelContent.BringToFront();
        }

        private static readonly Color Navy = Color.FromArgb(30, 58, 138);
        private static readonly Color Red = Color.FromArgb(185, 28, 28);
        private static readonly Color Amber = Color.FromArgb(217, 119, 6);
        private static readonly Color Green = Color.FromArgb(22, 101, 52);

        private void ShowOverview()
        {
            // Time-based rules (overdue appointments, long waits) are re-checked on every visit.
            AlertMonitor.Evaluate();

            panelContent.Controls.Clear();

            var overview = new Panel();
            overview.Dock = DockStyle.Fill;
            overview.BackColor = Color.FromArgb(243, 244, 246);
            overview.AutoScroll = true;

            // ===== 1. Summary cards =====
            var tlTop = new TableLayoutPanel();
            tlTop.Dock = DockStyle.Top;
            tlTop.Height = 112;
            tlTop.ColumnCount = 5;
            tlTop.RowCount = 1;
            for (int i = 0; i < 5; i++)
                tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            int occupied = HospitalData.OccupiedBedsCount();
            int totalBeds = HospitalData.TotalBedsCount();
            double rate = HospitalData.OccupancyRate();
            var today = HospitalData.AppointmentsToday();
            int todayOpen = today.Count(a => a.IsOpen);
            int next7 = UpcomingAppointments().Count;
            var pending = HospitalData.PendingAdmissions();
            var alerts = HospitalData.ActiveAlerts();
            int highAlerts = alerts.Count(a => a.Severity == "High");

            tlTop.Controls.Add(CreateSummaryCard("Appointments Today", today.Count.ToString(),
                $"{todayOpen} still to see  •  {next7} in the next 7 days", Navy), 0, 0);
            tlTop.Controls.Add(CreateSummaryCard("Bed Occupancy", $"{occupied}/{totalBeds}",
                $"{rate:0}% occupied  •  {totalBeds - occupied} free",
                rate >= 100 ? Red : rate >= AlertMonitor.HighOccupancyRate * 100 ? Amber : Navy), 1, 0);
            tlTop.Controls.Add(CreateSummaryCard("Admitted Patients", HospitalData.ActiveAdmissions().Count.ToString(),
                "currently in a bed", Navy), 2, 0);
            tlTop.Controls.Add(CreateSummaryCard("Pending Admissions", pending.Count.ToString(),
                pending.Count == 0 ? "no one waiting for a bed" : "longest wait " + HospitalData.FormatDuration(pending[0].WaitingTime),
                pending.Count == 0 ? Navy : Amber), 3, 0);
            tlTop.Controls.Add(CreateSummaryCard("Active Alerts", alerts.Count.ToString(),
                alerts.Count == 0 ? "all clear" : $"{highAlerts} high priority",
                highAlerts > 0 ? Red : alerts.Count > 0 ? Amber : Green), 4, 0);

            // ===== 2. Alerts + Pending admissions =====
            var tlRow2 = MakeRow(240, 55F, 45F);
            tlRow2.Controls.Add(BuildAlertsCard(alerts), 0, 0);
            tlRow2.Controls.Add(BuildPendingAdmissionsCard(pending), 1, 0);

            // ===== 3. Appointments + Bed occupancy by ward =====
            var tlRow3 = MakeRow(240, 55F, 45F);
            tlRow3.Controls.Add(BuildAppointmentsCard(), 0, 0);
            tlRow3.Controls.Add(BuildOccupancyCard(), 1, 0);

            // ===== 4. Doctors on duty + charts =====
            var tlBottom = MakeRow(200, 34F, 33F, 33F);
            tlBottom.Controls.Add(BuildDoctorsCard(), 0, 0);
            tlBottom.Controls.Add(CreateSimpleBarChart("Appointments per Day (Last 7 Days)", GetAppointmentsPerDay()), 1, 0);
            tlBottom.Controls.Add(CreateSimpleBarChart("Admissions per Month", GetAdmissionsPerMonth()), 2, 0);

            // Add all sections (order important for docking)
            overview.Controls.Add(tlBottom);
            overview.Controls.Add(tlRow3);
            overview.Controls.Add(tlRow2);
            overview.Controls.Add(tlTop);

            ShowView(new HostView(overview));
        }

        private static TableLayoutPanel MakeRow(int height, params float[] widths)
        {
            var tl = new TableLayoutPanel();
            tl.Dock = DockStyle.Top;
            tl.Height = height;
            tl.ColumnCount = widths.Length;
            tl.RowCount = 1;
            foreach (var w in widths)
                tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, w));
            tl.Padding = new Padding(0, 10, 0, 0);
            return tl;
        }

        private static Label MakeCardTitle(string text, Color color)
        {
            return new Label
            {
                Text = text,
                UseMnemonic = false,   // show "&" literally
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = color
            };
        }

        private static ListView MakeListView(params (string Header, int Width)[] columns)
        {
            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F)
            };
            foreach (var c in columns)
                lv.Columns.Add(c.Header, c.Width);
            return lv;
        }

        // ---------- Alerts ----------
        private Panel BuildAlertsCard(System.Collections.Generic.List<Alert> alerts)
        {
            var card = CreateCardPanel();
            int acknowledged = HospitalData.AcknowledgedAlerts().Count;
            var title = MakeCardTitle("Alerts" + (acknowledged > 0 ? $"    ({acknowledged} acknowledged, still ongoing)" : ""), Red);

            var hint = new Label
            {
                Text = "Raised automatically by the other modules; each clears itself once fixed.",
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };

            var flAlerts = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };
            // Stretch every alert row to the list width.
            flAlerts.Resize += (s, e) =>
            {
                foreach (Control c in flAlerts.Controls)
                    c.Width = Math.Max(200, flAlerts.ClientSize.Width - 6);
            };

            if (alerts.Count == 0)
            {
                var ok = new Label
                {
                    Text = "✔  All clear — no active alerts",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Green,
                    BackColor = Color.FromArgb(240, 253, 244),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0),
                    Width = 420,
                    Height = 40
                };
                flAlerts.Controls.Add(ok);
            }
            else
            {
                foreach (var a in alerts)
                    flAlerts.Controls.Add(CreateAlertItem(a));
            }

            card.Controls.Add(flAlerts);
            card.Controls.Add(hint);
            card.Controls.Add(title);
            return card;
        }

        private static void SeverityColors(string severity, out Color bg, out Color border)
        {
            switch (severity)
            {
                case "High":
                    bg = Color.FromArgb(254, 226, 226);
                    border = Color.FromArgb(239, 68, 68);
                    break;
                case "Medium":
                    bg = Color.FromArgb(254, 243, 199);
                    border = Color.FromArgb(245, 158, 11);
                    break;
                default:
                    bg = Color.FromArgb(219, 234, 254);
                    border = Color.FromArgb(59, 130, 246);
                    break;
            }
        }

        private Panel CreateAlertItem(Alert a)
        {
            Color bg, border;
            SeverityColors(a.Severity, out bg, out border);

            var p = new Panel
            {
                Width = 420,
                Height = 56,
                Margin = new Padding(0, 0, 0, 5),
                BackColor = bg,
                Cursor = Cursors.Hand
            };

            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(border, 4))
                    e.Graphics.DrawLine(pen, 0, 0, 0, p.Height);
            };

            var lblTitle = new Label
            {
                Text = (a.Severity == "High" ? "⚠ " : "") + a.Title + (a.Module != null ? "   ·   " + a.Module : ""),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize = true,
                Location = new Point(10, 5),
                Cursor = Cursors.Hand
            };

            var lblMsg = new Label
            {
                Text = a.Message,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(70, 70, 70),
                Location = new Point(10, 24),
                Size = new Size(p.Width - 110, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };

            var btnAck = new Button
            {
                Text = "Acknowledge",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Size = new Size(86, 24),
                Location = new Point(p.Width - 94, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = border,
                Cursor = Cursors.Hand
            };
            btnAck.FlatAppearance.BorderColor = border;
            btnAck.FlatAppearance.BorderSize = 1;
            btnAck.Click += (s, e) =>
            {
                HospitalData.AcknowledgeAlert(a.Id);
                ShowOverview();
            };

            Action openDetails = () => ShowAlertDetailsDialog(a);
            p.Click += (s, e) => openDetails();
            lblTitle.Click += (s, e) => openDetails();
            lblMsg.Click += (s, e) => openDetails();

            p.Controls.Add(lblTitle);
            p.Controls.Add(lblMsg);
            p.Controls.Add(btnAck);
            return p;
        }

        private void ShowAlertDetailsDialog(Alert a)
        {
            Color bg, headerColor;
            SeverityColors(a.Severity, out bg, out headerColor);

            using (var dlg = new Form())
            {
                dlg.Text = "Alert Details";
                dlg.Size = new Size(460, 300);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;

                var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = headerColor };
                pnlHeader.Controls.Add(new Label
                {
                    Text = a.Title,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(15, 12),
                    AutoSize = true
                });

                var lblSev = new Label
                {
                    Text = $"Severity: {a.Severity}   •   Raised by: {a.Module ?? "manual (legacy)"}   •   Since {a.CreatedOn:MMM dd, hh:mm tt}",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = headerColor,
                    Location = new Point(18, 65),
                    AutoSize = true
                };

                var lblDesc = new Label
                {
                    Text = a.Message + "\n\nThis alert clears by itself once the condition is fixed in " + (a.Module ?? "the related module") +
                           ". Acknowledging hides it until then.",
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Color.FromArgb(55, 65, 81),
                    Location = new Point(18, 92),
                    Size = new Size(410, 100)
                };

                var btnAck = new Button
                {
                    Text = "Acknowledge",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = headerColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(120, 34),
                    Location = new Point(18, 205)
                };
                btnAck.FlatAppearance.BorderSize = 0;
                btnAck.Click += (s, e) =>
                {
                    HospitalData.AcknowledgeAlert(a.Id);
                    dlg.DialogResult = DialogResult.OK;
                };

                var btnOpen = new Button
                {
                    Text = "Open " + (a.Module ?? "module"),
                    Font = new Font("Segoe UI", 9F),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(140, 34),
                    Location = new Point(146, 205),
                    Enabled = a.Module != null
                };
                btnOpen.Click += (s, e) => dlg.DialogResult = DialogResult.Yes;

                var btnClose = new Button
                {
                    Text = "Close",
                    Font = new Font("Segoe UI", 9F),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(80, 34),
                    Location = new Point(294, 205),
                    DialogResult = DialogResult.Cancel
                };

                dlg.Controls.Add(pnlHeader);
                dlg.Controls.Add(lblSev);
                dlg.Controls.Add(lblDesc);
                dlg.Controls.Add(btnAck);
                dlg.Controls.Add(btnOpen);
                dlg.Controls.Add(btnClose);
                dlg.CancelButton = btnClose;

                var result = dlg.ShowDialog(this);
                if (result == DialogResult.OK) ShowOverview();
                else if (result == DialogResult.Yes) OpenModule(a.Module);
            }
        }

        // ---------- Pending admissions ----------
        private Panel BuildPendingAdmissionsCard(System.Collections.Generic.List<Admission> pending)
        {
            var card = CreateCardPanel();
            int free = HospitalData.AvailableBeds().Count;
            var title = MakeCardTitle("Pending Admissions (waiting for a bed)", pending.Count > 0 ? Amber : Navy);

            var lv = MakeListView(("#", 30), ("Patient", 130), ("Doctor", 110), ("Waiting", 80), ("Reason", 140));
            if (pending.Count == 0)
                lv.Items.Add(new ListViewItem(new[] { "—", "No patients waiting", "—", "—", "—" }));
            else
            {
                int n = 1;
                foreach (var a in pending)
                {
                    var item = new ListViewItem((n++).ToString());
                    item.SubItems.Add(HospitalData.PatientName(a.PatientId));
                    item.SubItems.Add(HospitalData.DoctorName(a.DoctorId));
                    item.SubItems.Add(HospitalData.FormatDuration(a.WaitingTime));
                    item.SubItems.Add(a.Diagnosis ?? "");
                    if (a.WaitingTime >= AlertMonitor.LongWait)
                        item.ForeColor = Red;
                    lv.Items.Add(item);
                }
            }
            lv.DoubleClick += (s, e) => OpenModule("Admissions");

            var footer = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Font = new Font("Segoe UI", 8F),
                ForeColor = pending.Count > 0 && free > 0 ? Red : Color.Gray,
                Text = pending.Count > 0 && free > 0
                    ? $"{free} bed(s) free now — double-click to assign them in Admissions."
                    : "Double-click to open Admissions."
            };

            card.Controls.Add(lv);
            card.Controls.Add(footer);
            card.Controls.Add(title);
            return card;
        }

        // ---------- Appointments ----------
        private static System.Collections.Generic.List<Appointment> UpcomingAppointments()
        {
            return HospitalData.Appointments
                .Where(a => a.ScheduledOn >= DateTime.Today && a.ScheduledOn < DateTime.Today.AddDays(8) && a.Status != "Cancelled")
                .OrderBy(a => a.ScheduledOn)
                .ToList();
        }

        private Panel BuildAppointmentsCard()
        {
            var card = CreateCardPanel();
            var title = MakeCardTitle("Appointments — Today & Next 7 Days", Navy);

            var lv = MakeListView(("Date", 90), ("Time", 70), ("Patient", 130), ("Doctor", 120), ("Status", 80));
            var upcoming = UpcomingAppointments();
            if (upcoming.Count == 0)
                lv.Items.Add(new ListViewItem(new[] { "—", "—", "No upcoming appointments", "—", "—" }));
            else
            {
                var bold = new Font("Segoe UI", 9F, FontStyle.Bold);
                foreach (var a in upcoming)
                {
                    bool isToday = a.ScheduledOn.Date == DateTime.Today;
                    var item = new ListViewItem(isToday ? "Today" : a.ScheduledOn.ToString("ddd MMM dd"));
                    item.SubItems.Add(a.ScheduledOn.ToString("hh:mm tt"));
                    item.SubItems.Add(HospitalData.PatientName(a.PatientId));
                    item.SubItems.Add(HospitalData.DoctorName(a.DoctorId));
                    item.SubItems.Add(a.Status);
                    if (isToday) item.Font = bold;
                    if (a.IsOpen && a.ScheduledOn < DateTime.Now - AlertMonitor.AppointmentGrace)
                        item.ForeColor = Red;   // overdue, not closed yet
                    lv.Items.Add(item);
                }
            }
            lv.DoubleClick += (s, e) => OpenModule("Appointments");

            card.Controls.Add(lv);
            card.Controls.Add(title);
            return card;
        }

        // ---------- Bed occupancy by ward ----------
        private Panel BuildOccupancyCard()
        {
            var card = CreateCardPanel();
            var title = MakeCardTitle("Bed Occupancy by Ward", Navy);

            var wards = HospitalData.Beds
                .GroupBy(b => b.Ward ?? "(no ward)")
                .OrderBy(g => g.Key)
                .Select(g => new { Ward = g.Key, Total = g.Count(), Occupied = g.Count(b => b.IsOccupied) })
                .ToList();
            wards.Add(new { Ward = "All wards", Total = HospitalData.TotalBedsCount(), Occupied = HospitalData.OccupiedBedsCount() });

            var bars = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            bars.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var labelFont = new Font("Segoe UI", 9F))
                using (var boldFont = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var track = new SolidBrush(Color.FromArgb(229, 231, 235)))
                {
                    int rowH = Math.Max(24, Math.Min(34, bars.Height / Math.Max(1, wards.Count)));
                    int labelW = 90, valueW = 90;
                    int barW = Math.Max(40, bars.Width - labelW - valueW - 10);

                    for (int i = 0; i < wards.Count; i++)
                    {
                        var w = wards[i];
                        bool total = i == wards.Count - 1;
                        int y = i * rowH + 4;
                        double r = w.Total == 0 ? 0 : (double)w.Occupied / w.Total;
                        Color fill = r >= 1 ? Red : r >= AlertMonitor.HighOccupancyRate ? Amber : Color.FromArgb(37, 99, 235);

                        g.DrawString(w.Ward, total ? boldFont : labelFont, Brushes.Black, 0, y + 2);
                        g.FillRectangle(track, labelW, y + 4, barW, rowH - 12);
                        using (var brush = new SolidBrush(fill))
                            g.FillRectangle(brush, labelW, y + 4, (int)(barW * r), rowH - 12);
                        g.DrawString($"{w.Occupied}/{w.Total} ({r:P0})", total ? boldFont : labelFont, Brushes.DimGray, labelW + barW + 6, y + 2);
                    }
                }
            };
            bars.Resize += (s, e) => bars.Invalidate();
            bars.DoubleClick += (s, e) => OpenModule("Admissions");

            card.Controls.Add(bars);
            card.Controls.Add(title);
            return card;
        }

        // ---------- Doctors on duty ----------
        private Panel BuildDoctorsCard()
        {
            var card = CreateCardPanel();
            var title = MakeCardTitle("Doctors on Duty", Navy);

            var lv = MakeListView(("Doctor", 130), ("Specialization", 110), ("Department", 100));
            var onDuty = HospitalData.DoctorsOnDuty();
            if (onDuty.Count == 0)
                lv.Items.Add(new ListViewItem(new[] { "—", "No doctors on duty", "—" }));
            else
            {
                foreach (var d in onDuty)
                {
                    var item = new ListViewItem("Dr. " + d.FullName);
                    item.SubItems.Add(d.Specialization);
                    item.SubItems.Add(HospitalData.DepartmentName(d.DepartmentId));
                    lv.Items.Add(item);
                }
            }

            card.Controls.Add(lv);
            card.Controls.Add(title);
            return card;
        }

        private Panel CreateCardPanel()
        {
            var p = new Panel();
            p.Dock = DockStyle.Fill;
            p.Margin = new Padding(5);
            p.BackColor = Color.White;
            p.Padding = new Padding(10);
            p.BorderStyle = BorderStyle.FixedSingle;
            return p;
        }

        private Panel CreateSummaryCard(string title, string value, string subtitle, Color accent)
        {
            var p = new Panel();
            p.Margin = new Padding(5);
            p.BackColor = Color.White;
            p.Padding = new Padding(12, 8, 12, 8);
            p.Dock = DockStyle.Fill;
            p.BorderStyle = BorderStyle.FixedSingle;

            // Colour strip on the left: red/amber when the number needs attention.
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(accent, 4))
                    e.Graphics.DrawLine(pen, 1, 0, 1, p.Height);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Dock = DockStyle.Top,
                Height = 20
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Dock = DockStyle.Bottom,
                Height = 18
            };

            p.Controls.Add(lblValue);
            p.Controls.Add(lblSub);
            p.Controls.Add(lblTitle);
            return p;
        }

        private Panel CreateSimpleBarChart(string title, (string Label, int Value)[] data)
        {
            var p = new Panel();
            p.Margin = new Padding(5);
            p.BackColor = Color.White;
            p.Padding = new Padding(10);
            p.Dock = DockStyle.Fill;
            p.BorderStyle = BorderStyle.FixedSingle;

            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Navy
            };
            p.Controls.Add(lbl);

            var chartArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            chartArea.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int max = data.Max(d => d.Value);
                if (max == 0) max = 1;

                int barCount = data.Length;
                int gap = 7;
                int totalGap = gap * (barCount + 1);
                int barWidth = Math.Max(10, (chartArea.Width - totalGap) / barCount);
                int maxBarHeight = chartArea.Height - 28;

                using (var valueFont = new Font("Segoe UI", 7.5F))
                using (var labelFont = new Font("Segoe UI", 7F))
                using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    for (int i = 0; i < barCount; i++)
                    {
                        int h = (int)(data[i].Value / (float)max * maxBarHeight);
                        int x = gap + i * (barWidth + gap);
                        int y = chartArea.Height - h - 16;

                        g.FillRectangle(brush, x, y, barWidth, h);

                        string val = data[i].Value.ToString();
                        var sz = g.MeasureString(val, valueFont);
                        g.DrawString(val, valueFont, Brushes.DimGray, x + (barWidth - sz.Width) / 2, y - 14);

                        string lab = data[i].Label;
                        var sz2 = g.MeasureString(lab, labelFont);
                        g.DrawString(lab, labelFont, Brushes.Gray, x + (barWidth - sz2.Width) / 2, chartArea.Height - 14);
                    }
                }
            };

            p.Controls.Add(chartArea);
            chartArea.BringToFront();
            return p;
        }

        private (string Label, int Value)[] GetAppointmentsPerDay()
        {
            var result = new (string, int)[7];
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                int count = HospitalData.Appointments.Count(a => a.ScheduledOn.Date == day);
                result[6 - i] = (day.ToString("ddd"), count);
            }
            return result;
        }

        private (string Label, int Value)[] GetAdmissionsPerMonth()
        {
            var result = new (string, int)[6];
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Today.AddMonths(-i);
                int count = HospitalData.Admissions.Count(a => !a.IsPending && a.Status != "Cancelled" &&
                    a.AdmittedOn.Year == month.Year && a.AdmittedOn.Month == month.Month);
                result[5 - i] = (month.ToString("MMM"), count);
            }
            return result;
        }

        private Button CreateNavButton(string text, int top)
        {
            Button btn = new Button();
            btn.Text = "  " + text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(30, 58, 138);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 10F);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Height = 42;
            btn.Width = 220;
            btn.Location = new Point(0, top);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 99, 235);
            return btn;
        }

        private void ShowView(UserControl view)
        {
            if (currentView != null)
            {
                panelContent.Controls.Remove(currentView);
                currentView.Dispose();
            }
            currentView = view;
            currentView.Dock = DockStyle.Fill;
            panelContent.Controls.Add(currentView);
            currentView.BringToFront();
        }

        private void BtnSignOut_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to sign out?", "Sign Out",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                HospitalData.SignOut();   // logs the sign-out
                this.Hide();
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        ApplyUserAccess();
                        ShowOverview();
                        this.Show();
                    }
                    else
                    {
                        Application.Exit();
                    }
                }
            }
        }

        private class HostView : UserControl
        {
            public HostView(Control content)
            {
                this.Dock = DockStyle.Fill;
                content.Dock = DockStyle.Fill;
                this.Controls.Add(content);
            }
        }
    }
}

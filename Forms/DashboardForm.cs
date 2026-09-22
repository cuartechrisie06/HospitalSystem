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
        private Button btnAppointments;
        private Button btnAdmissions;
        private Button btnSignOut;
        private UserControl currentView;

        public DashboardForm()
        {
            InitializeComponent();
            UpdateUserLabel();
            ShowOverview();
        }

        private void UpdateUserLabel()
        {
            lblUser.Text = HospitalData.CurrentUser != null
                ? HospitalData.CurrentUser.DisplayName
                : "User";
        }

        private void DashboardForm_Load(object sender, EventArgs e)
        {
            UpdateUserLabel();
            ShowOverview();
        }

        private void BtnDashboard_Click(object sender, EventArgs e) => ShowOverview();
        private void BtnPatients_Click(object sender, EventArgs e) => ShowView(new Views.PatientsView());
        private void BtnAppointments_Click(object sender, EventArgs e) => ShowView(new Views.AppointmentsView());
        private void BtnAdmissions_Click(object sender, EventArgs e) => ShowView(new Views.AdmissionsView());

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

            btnDashboard = CreateNavButton("Dashboard", 70);
            btnDashboard.Click += BtnDashboard_Click;
            panelSidebar.Controls.Add(btnDashboard);

            btnPatients = CreateNavButton("Patients", 120);
            btnPatients.Click += BtnPatients_Click;
            panelSidebar.Controls.Add(btnPatients);

            btnAppointments = CreateNavButton("Appointments", 170);
            btnAppointments.Click += BtnAppointments_Click;
            panelSidebar.Controls.Add(btnAppointments);

            btnAdmissions = CreateNavButton("Admissions", 220);
            btnAdmissions.Click += BtnAdmissions_Click;
            panelSidebar.Controls.Add(btnAdmissions);

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
            lblUser.Height = 40;
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

        private void ShowOverview()
        {
            panelContent.Controls.Clear();

            var overview = new Panel();
            overview.Dock = DockStyle.Fill;
            overview.BackColor = Color.FromArgb(243, 244, 246);
            overview.AutoScroll = true;

            // ===== 1. TOP: Summary Cards (4) =====
            var tlTop = new TableLayoutPanel();
            tlTop.Dock = DockStyle.Top;
            tlTop.Height = 100;
            tlTop.ColumnCount = 4;
            tlTop.RowCount = 1;
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            int occupied = HospitalData.OccupiedBedsCount();
            int totalBeds = HospitalData.TotalBedsCount();
            double rate = HospitalData.OccupancyRate();

            tlTop.Controls.Add(CreateSummaryCard("Patients", HospitalData.Patients.Count.ToString()), 0, 0);
            tlTop.Controls.Add(CreateSummaryCard("Appointments", HospitalData.Appointments.Count.ToString()), 1, 0);
            tlTop.Controls.Add(CreateSummaryCard("Admissions", HospitalData.ActiveAdmissions().Count.ToString()), 2, 0);
            tlTop.Controls.Add(CreateSummaryCard("Bed Occupancy", $"{occupied}/{totalBeds} ({rate:0}%)"), 3, 0);

            // ===== 2. Doctors on Duty + Emergency Alerts =====
            var tlRow2 = new TableLayoutPanel();
            tlRow2.Dock = DockStyle.Top;
            tlRow2.Height = 200;
            tlRow2.ColumnCount = 2;
            tlRow2.RowCount = 1;
            tlRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlRow2.Padding = new Padding(0, 10, 0, 0);

            // Doctors on Duty
            var pnlDoctors = CreateCardPanel();
            var lblDoctors = new Label
            {
                Text = "Doctors on Duty",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Color.FromArgb(30, 58, 138)
            };

            var lvDoctors = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F)
            };
            lvDoctors.Columns.Add("Doctor", 150);
            lvDoctors.Columns.Add("Specialization", 130);
            lvDoctors.Columns.Add("Department", 110);

            var onDuty = HospitalData.DoctorsOnDuty();
            if (onDuty.Count == 0)
                lvDoctors.Items.Add(new ListViewItem(new[] { "—", "No doctors on duty", "—" }));
            else
            {
                foreach (var d in onDuty)
                {
                    var item = new ListViewItem("Dr. " + d.FullName);
                    item.SubItems.Add(d.Specialization);
                    item.SubItems.Add(HospitalData.DepartmentName(d.DepartmentId));
                    lvDoctors.Items.Add(item);
                }
            }
            pnlDoctors.Controls.Add(lvDoctors);
            pnlDoctors.Controls.Add(lblDoctors);

            // Emergency Alerts
            var pnlAlerts = CreateCardPanel();
            var lblAlerts = new Label
            {
                Text = "Emergency Alerts",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Color.FromArgb(185, 28, 28)
            };

            var flAlerts = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0)
            };

            var alerts = HospitalData.ActiveAlerts();
            if (alerts.Count == 0)
                flAlerts.Controls.Add(new Label { Text = "No active alerts", ForeColor = Color.Gray, AutoSize = true });
            else
                foreach (var a in alerts)
                    flAlerts.Controls.Add(CreateAlertItem(a));

            pnlAlerts.Controls.Add(flAlerts);
            pnlAlerts.Controls.Add(lblAlerts);

            tlRow2.Controls.Add(pnlDoctors, 0, 0);
            tlRow2.Controls.Add(pnlAlerts, 1, 0);

            // ===== 3. Upcoming Appointments + Recent Activity =====
            var tlRow3 = new TableLayoutPanel();
            tlRow3.Dock = DockStyle.Top;
            tlRow3.Height = 210;
            tlRow3.ColumnCount = 2;
            tlRow3.RowCount = 1;
            tlRow3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tlRow3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tlRow3.Padding = new Padding(0, 10, 0, 0);

            // Upcoming Appointments
            var pnlUpcoming = CreateCardPanel();
            var lblUpcoming = new Label
            {
                Text = "Upcoming Appointments (Next 7 Days)",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Color.FromArgb(30, 58, 138)
            };

            var lvUpcoming = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F)
            };
            lvUpcoming.Columns.Add("Date", 140);
            lvUpcoming.Columns.Add("Patient", 160);
            lvUpcoming.Columns.Add("Doctor", 130);

            var upcoming = HospitalData.Appointments
                .Where(a => a.ScheduledOn >= DateTime.Now && a.ScheduledOn <= DateTime.Now.AddDays(7))
                .OrderBy(a => a.ScheduledOn)
                .ToList();

            if (upcoming.Count == 0)
                lvUpcoming.Items.Add(new ListViewItem(new[] { "—", "No upcoming appointments", "—" }));
            else
            {
                foreach (var a in upcoming)
                {
                    var item = new ListViewItem(a.ScheduledOn.ToString("MMM dd, yyyy hh:mm tt"));
                    item.SubItems.Add(HospitalData.PatientName(a.PatientId));
                    item.SubItems.Add(HospitalData.DoctorName(a.DoctorId));
                    lvUpcoming.Items.Add(item);
                }
            }
            pnlUpcoming.Controls.Add(lvUpcoming);
            pnlUpcoming.Controls.Add(lblUpcoming);

            // Recent Activity
            var pnlActivity = CreateCardPanel();
            var lblActivity = new Label
            {
                Text = "Recent Activity",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Color.FromArgb(30, 58, 138)
            };

            var lbActivity = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };

            var recent = new System.Collections.Generic.List<string>();

            recent.AddRange(HospitalData.Appointments
                .Where(a => a.ScheduledOn >= DateTime.Now.AddDays(-7))
                .OrderByDescending(a => a.ScheduledOn)
                .Select(a => $"📅  {a.ScheduledOn:MMM dd} – {HospitalData.PatientName(a.PatientId)}"));

            recent.AddRange(HospitalData.Admissions
                .Where(ad => ad.AdmittedOn >= DateTime.Now.AddDays(-7))
                .OrderByDescending(ad => ad.AdmittedOn)
                .Select(ad => $"🏥  Admission: {HospitalData.PatientName(ad.PatientId)}"));

            recent.AddRange(HospitalData.Patients
                .Where(p => p.RegisteredOn >= DateTime.Now.AddDays(-7))
                .OrderByDescending(p => p.RegisteredOn)
                .Select(p => $"👤  New patient: {p.FullName}"));

            if (recent.Count == 0)
                lbActivity.Items.Add("No recent activity");
            else
                foreach (var s in recent.Take(10))
                    lbActivity.Items.Add(s);

            pnlActivity.Controls.Add(lbActivity);
            pnlActivity.Controls.Add(lblActivity);

            tlRow3.Controls.Add(pnlUpcoming, 0, 0);
            tlRow3.Controls.Add(pnlActivity, 1, 0);

            // ===== 4. Charts =====
            var tlBottom = new TableLayoutPanel();
            tlBottom.Dock = DockStyle.Top;
            tlBottom.Height = 190;
            tlBottom.ColumnCount = 2;
            tlBottom.RowCount = 1;
            tlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlBottom.Padding = new Padding(0, 10, 0, 0);

            tlBottom.Controls.Add(CreateSimpleBarChart("Appointments per Day (Last 7 Days)", GetAppointmentsPerDay()), 0, 0);
            tlBottom.Controls.Add(CreateSimpleBarChart("Admissions per Month", GetAdmissionsPerMonth()), 1, 0);

            // Add all sections (order important for docking)
            overview.Controls.Add(tlBottom);
            overview.Controls.Add(tlRow3);
            overview.Controls.Add(tlRow2);
            overview.Controls.Add(tlTop);

            ShowView(new HostView(overview));
        }

        private Panel CreateAlertItem(Alert a)
        {
            Color bg, border;
            switch (a.Severity)
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

            var p = new Panel
            {
                Width = 420,
                Height = 48,
                Margin = new Padding(0, 0, 0, 5),
                BackColor = bg,
                Padding = new Padding(8)
            };

            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(border, 4))
                    e.Graphics.DrawLine(pen, 0, 0, 0, p.Height);
            };

            var lblTitle = new Label
            {
                Text = a.Title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize = true,
                Location = new Point(10, 3)
            };

            var lblMsg = new Label
            {
                Text = a.Message,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(10, 22)
            };

            p.Controls.Add(lblTitle);
            p.Controls.Add(lblMsg);
            return p;
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

        private Panel CreateSummaryCard(string title, string value)
        {
            var p = new Panel();
            p.Margin = new Padding(5);
            p.BackColor = Color.White;
            p.Padding = new Padding(12, 10, 12, 10);
            p.Dock = DockStyle.Fill;
            p.BorderStyle = BorderStyle.FixedSingle;

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),         
                ForeColor = Color.FromArgb(55, 65, 81),                    
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),           
                ForeColor = Color.FromArgb(30, 58, 138),                   
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            p.Controls.Add(lblValue);
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
                ForeColor = Color.FromArgb(30, 58, 138)
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

                for (int i = 0; i < barCount; i++)
                {
                    int h = (int)(data[i].Value / (float)max * maxBarHeight);
                    int x = gap + i * (barWidth + gap);
                    int y = chartArea.Height - h - 16;

                    using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                        g.FillRectangle(brush, x, y, barWidth, h);

                    string val = data[i].Value.ToString();
                    var sz = g.MeasureString(val, new Font("Segoe UI", 7.5F));
                    g.DrawString(val, new Font("Segoe UI", 7.5F), Brushes.DimGray,
                        x + (barWidth - sz.Width) / 2, y - 14);

                    string lab = data[i].Label;
                    var sz2 = g.MeasureString(lab, new Font("Segoe UI", 7F));
                    g.DrawString(lab, new Font("Segoe UI", 7F), Brushes.Gray,
                        x + (barWidth - sz2.Width) / 2, chartArea.Height - 14);
                }
            };

            p.Controls.Add(chartArea);
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
                int count = HospitalData.Admissions.Count(a =>
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
                HospitalData.CurrentUser = null;
                this.Hide();
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        UpdateUserLabel();
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
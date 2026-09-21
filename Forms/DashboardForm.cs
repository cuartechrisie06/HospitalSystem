using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Views;

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
            ShowOverview();
        }

        // Designer-friendly event handlers moved out of lambda expressions
        private void DashboardForm_Load(object sender, EventArgs e)
        {
            // Show the dashboard overview by default at runtime
            ShowOverview();
        }

        private void BtnDashboard_Click(object sender, EventArgs e)
        {
            ShowOverview();
        }

        private void BtnPatients_Click(object sender, EventArgs e)
        {
            ShowView(new Views.PatientsView());
        }

        private void BtnAppointments_Click(object sender, EventArgs e)
        {
            ShowView(new Views.AppointmentsView());
        }

        private void BtnAdmissions_Click(object sender, EventArgs e)
        {
            ShowView(new Views.AdmissionsView());
        }

        private void InitializeComponent()
        {
            this.Text = "Hospital System";
            this.Size = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(950, 620);
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

            // Sign out at bottom
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

            // User name above sign out
            lblUser = new Label();
            lblUser.Dock = DockStyle.Bottom;
            lblUser.Height = 40;
            lblUser.ForeColor = Color.LightGray;
            lblUser.Font = new Font("Segoe UI", 9F);
            lblUser.TextAlign = ContentAlignment.MiddleCenter;
            lblUser.Text = HospitalData.CurrentUser != null
                ? HospitalData.CurrentUser.DisplayName
                : "User";
            panelSidebar.Controls.Add(lblUser);

            // ===== Content area =====
            panelContent = new Panel();
            panelContent.Dock = DockStyle.Fill;
            panelContent.Padding = new Padding(25, 20, 25, 20);
            panelContent.BackColor = Color.FromArgb(243, 244, 246);
            this.Controls.Add(panelContent);

            this.Load += DashboardForm_Load;
            panelContent.BringToFront();
        }

        // Render the dashboard overview with summary cards, upcoming appointments, small charts and recent activity
        private void ShowOverview()
        {
            // Clear any direct controls previously added to panelContent to avoid layering
            panelContent.Controls.Clear();

            // Create a container panel for the dashboard so it is hosted as a single view
            var overview = new Panel();
            overview.Dock = DockStyle.Fill;
            overview.BackColor = Color.FromArgb(243, 244, 246);
            overview.AutoScroll = true;

            // Top: summary cards
            var tlTop = new TableLayoutPanel();
            tlTop.Dock = DockStyle.Top;
            tlTop.Height = 120;
            tlTop.ColumnCount = 3;
            tlTop.RowCount = 1;
            tlTop.ColumnStyles.Clear();
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            tlTop.Controls.Add(CreateSummaryCard("Patients", HospitalData.Patients.Count.ToString()), 0, 0);
            tlTop.Controls.Add(CreateSummaryCard("Appointments", HospitalData.Appointments.Count.ToString()), 1, 0);
            tlTop.Controls.Add(CreateSummaryCard("Admissions", HospitalData.Admissions.Count.ToString()), 2, 0);

            // Middle: two columns - upcoming appointments and recent activity
            var tlMiddle = new TableLayoutPanel();
            tlMiddle.Dock = DockStyle.Fill;
            tlMiddle.Padding = new Padding(0, 15, 0, 0);
            tlMiddle.ColumnCount = 2;
            tlMiddle.RowCount = 1;
            tlMiddle.ColumnStyles.Clear();
            tlMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tlMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // Upcoming appointments list (next 7 days)
            var lvUpcoming = new ListView();
            lvUpcoming.Dock = DockStyle.Fill;
            lvUpcoming.View = View.Details;
            lvUpcoming.FullRowSelect = true;
            lvUpcoming.Columns.Add("Date", 140);
            lvUpcoming.Columns.Add("Patient", 200);
            lvUpcoming.Columns.Add("Doctor", 160);

            var upcoming = HospitalData.Appointments
                .Where(a => a.ScheduledOn >= DateTime.Now && a.ScheduledOn <= DateTime.Now.AddDays(7))
                .OrderBy(a => a.ScheduledOn)
                .ToList();

            foreach (var a in upcoming)
            {
                var item = new ListViewItem(a.ScheduledOn.ToString("g"));
                item.SubItems.Add(HospitalData.PatientName(a.PatientId));
                item.SubItems.Add(HospitalData.DoctorName(a.DoctorId));
                lvUpcoming.Items.Add(item);
            }

            var pnlUpcoming = new Panel();
            pnlUpcoming.Dock = DockStyle.Fill;
            var lblUpcoming = new Label();
            lblUpcoming.Text = "Upcoming Appointments";
            lblUpcoming.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblUpcoming.Height = 28;
            lblUpcoming.Dock = DockStyle.Top;
            pnlUpcoming.Controls.Add(lvUpcoming);
            pnlUpcoming.Controls.Add(lblUpcoming);

            // Recent activity feed (simple list)
            var lbActivity = new ListBox();
            lbActivity.Dock = DockStyle.Fill;
            lbActivity.Font = new Font("Segoe UI", 9F);

            // Populate recent activity: new patients, appointments, admissions (last 7 days)
            var recentPatients = HospitalData.Patients
                .Where(p => p.RegisteredOn >= DateTime.Now.AddDays(-7))
                .OrderByDescending(p => p.RegisteredOn)
                .Select(p => $"New patient: {p}")
                .ToList();

            var recentAppointments = HospitalData.Appointments
                .Where(a => a.ScheduledOn >= DateTime.Now.AddDays(-7))
                .OrderByDescending(a => a.ScheduledOn)
                .Select(a => $"Appointment: {a.ScheduledOn:g} - {HospitalData.PatientName(a.PatientId)}")
                .ToList();

            var recentAdmissions = HospitalData.Admissions
                .Where(ad => ad.AdmittedOn >= DateTime.Now.AddDays(-7))
                .OrderByDescending(ad => ad.AdmittedOn)
                .Select(ad => $"Admission: {HospitalData.PatientName(ad.PatientId)}")
                .ToList();

            foreach (var s in recentAppointments) lbActivity.Items.Add(s);
            foreach (var s in recentAdmissions) lbActivity.Items.Add(s);
            foreach (var s in recentPatients) lbActivity.Items.Add(s);

            var pnlActivity = new Panel();
            pnlActivity.Dock = DockStyle.Fill;
            var lblActivity = new Label();
            lblActivity.Text = "Recent Activity";
            lblActivity.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblActivity.Height = 28;
            lblActivity.Dock = DockStyle.Top;
            pnlActivity.Controls.Add(lbActivity);
            pnlActivity.Controls.Add(lblActivity);

            tlMiddle.Controls.Add(pnlUpcoming, 0, 0);
            tlMiddle.Controls.Add(pnlActivity, 1, 0);

            // Bottom: small placeholder charts (simple panels with labels)
            var tlBottom = new TableLayoutPanel();
            tlBottom.Dock = DockStyle.Bottom;
            tlBottom.Height = 160;
            tlBottom.ColumnCount = 2;
            tlBottom.RowCount = 1;
            tlBottom.ColumnStyles.Clear();
            tlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var pnlChart1 = CreateChartPlaceholder("Appointments per Day (7d)");
            var pnlChart2 = CreateChartPlaceholder("Admissions per Month");

            tlBottom.Controls.Add(pnlChart1, 0, 0);
            tlBottom.Controls.Add(pnlChart2, 1, 0);

            // Add sections to overview container (order helps docking behavior)
            overview.Controls.Add(tlTop);
            overview.Controls.Add(tlBottom);
            overview.Controls.Add(tlMiddle);

            // Host the overview in the panelContent so it is treated as a single view
            ShowView(new HostView(overview));
        }

        private Panel CreateSummaryCard(string title, string value)
        {
            var p = new Panel();
            p.Margin = new Padding(8);
            p.BackColor = Color.White;
            p.Padding = new Padding(12);
            p.Dock = DockStyle.Fill;

            var lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 9F);
            lblTitle.ForeColor = Color.Gray;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 20;

            var lblValue = new Label();
            lblValue.Text = value;
            lblValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblValue.ForeColor = Color.FromArgb(30, 58, 138);
            lblValue.Dock = DockStyle.Fill;
            lblValue.TextAlign = ContentAlignment.MiddleLeft;

            p.Controls.Add(lblValue);
            p.Controls.Add(lblTitle);
            return p;
        }

        private Panel CreateChartPlaceholder(string title)
        {
            var p = new Panel();
            p.Margin = new Padding(8);
            p.BackColor = Color.White;
            p.Padding = new Padding(12);
            p.Dock = DockStyle.Fill;

            var lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbl.Dock = DockStyle.Top;
            lbl.Height = 20;

            var placeholder = new Label();
            placeholder.Text = "[Chart]";
            placeholder.Font = new Font("Segoe UI", 12F);
            placeholder.ForeColor = Color.Gray;
            placeholder.Dock = DockStyle.Fill;
            placeholder.TextAlign = ContentAlignment.MiddleCenter;

            p.Controls.Add(placeholder);
            p.Controls.Add(lbl);
            return p;
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

        // Removed legacy ShowOverviewHost/CreateStatCard implementations to avoid
        // duplicate dashboard render paths. The active ShowOverview() above
        // uses responsive TableLayoutPanels and CreateSummaryCard/CreateChartPlaceholder.

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
                        lblUser.Text = HospitalData.CurrentUser.DisplayName;
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

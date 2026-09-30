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
        private Label lblBrand;
        private Panel panelProfile;
        private Label lblProfileName;
        private Label lblProfileRole;
        private Button btnDashboard;
        private Button btnPatients;
        private Button btnDoctors;
        private Button btnAppointments;
        private Button btnAdmissions;
        private Button btnBilling;
        private Button btnActivityLog;
        private Panel sepProfile;
        private Panel sepNav1;
        private Panel sepNav2;
        private Panel sepNav3;
        private Panel sepNav4;
        private Panel sepNav5;
        private Panel sepActivityLog;
        private Panel sepBottom;
        private Button btnSignOut;

        private string profileInitials = "";
        private Color profileRoleColor = Color.FromArgb(107, 114, 128);
        private UserControl currentView;

        public DashboardForm()
        {
            // Every control is created in InitializeComponent() so the Designer shows the
            // whole sidebar; only the data-driven dashboard content is built at runtime.
            InitializeComponent();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
            {
                ApplyUserAccess();
                ShowOverview();
            }
        }

        // Signed-in user at the top of the sidebar: initials avatar, name, and a role badge
        // (amber for administrators, teal for nurses) so it's always clear whose view this is.
        private void PanelProfile_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var circle = new Rectangle(16, 17, 44, 44);
            using (var fill = new SolidBrush(profileRoleColor))
                g.FillEllipse(fill, circle);
            using (var font = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(profileInitials, font, Brushes.White, circle, center);
        }

        private static string Initials(string name)
        {
            var parts = (name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            return parts.Length == 1
                ? parts[0].Substring(0, 1).ToUpper()
                : (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
        }

        // Shows who is signed in and which modules their role can open.
        private void ApplyUserAccess()
        {
            var user = HospitalData.CurrentUser;
            bool admin = HospitalData.IsAdmin;

            lblProfileName.Text = user != null ? user.DisplayName : "Not signed in";
            lblProfileRole.Text = user != null ? user.Role : "";
            profileRoleColor = admin ? Color.FromArgb(217, 119, 6) : Color.FromArgb(13, 148, 136);
            lblProfileRole.BackColor = profileRoleColor;
            profileInitials = Initials(user != null ? user.DisplayName : null);
            panelProfile.Invalidate();

            btnActivityLog.Visible = Permissions.Can(Permission.ViewActivityLog);
            sepActivityLog.Visible = btnActivityLog.Visible;
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

        // Designer-generated layout. Sidebar, top to bottom: brand (0-60), user profile (60-138),
        // nav buttons every 50px from y=146 (42px button + 8px gap, 1px separator in the gap),
        // and Sign Out docked at the bottom. Activity Log (y=446) is hidden for non-admins at runtime.
        private void InitializeComponent()
        {
            this.panelSidebar = new System.Windows.Forms.Panel();
            this.btnActivityLog = new System.Windows.Forms.Button();
            this.btnBilling = new System.Windows.Forms.Button();
            this.btnAdmissions = new System.Windows.Forms.Button();
            this.btnAppointments = new System.Windows.Forms.Button();
            this.btnDoctors = new System.Windows.Forms.Button();
            this.btnPatients = new System.Windows.Forms.Button();
            this.btnDashboard = new System.Windows.Forms.Button();
            this.sepActivityLog = new System.Windows.Forms.Panel();
            this.sepNav5 = new System.Windows.Forms.Panel();
            this.sepNav4 = new System.Windows.Forms.Panel();
            this.sepNav3 = new System.Windows.Forms.Panel();
            this.sepNav2 = new System.Windows.Forms.Panel();
            this.sepNav1 = new System.Windows.Forms.Panel();
            this.sepProfile = new System.Windows.Forms.Panel();
            this.panelProfile = new System.Windows.Forms.Panel();
            this.lblProfileRole = new System.Windows.Forms.Label();
            this.lblProfileName = new System.Windows.Forms.Label();
            this.sepBottom = new System.Windows.Forms.Panel();
            this.btnSignOut = new System.Windows.Forms.Button();
            this.lblBrand = new System.Windows.Forms.Label();
            this.panelContent = new System.Windows.Forms.Panel();
            this.panelSidebar.SuspendLayout();
            this.panelProfile.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelSidebar
            // 
            this.panelSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.panelSidebar.Controls.Add(this.btnActivityLog);
            this.panelSidebar.Controls.Add(this.btnBilling);
            this.panelSidebar.Controls.Add(this.btnAdmissions);
            this.panelSidebar.Controls.Add(this.btnAppointments);
            this.panelSidebar.Controls.Add(this.btnDoctors);
            this.panelSidebar.Controls.Add(this.btnPatients);
            this.panelSidebar.Controls.Add(this.btnDashboard);
            this.panelSidebar.Controls.Add(this.sepActivityLog);
            this.panelSidebar.Controls.Add(this.sepNav5);
            this.panelSidebar.Controls.Add(this.sepNav4);
            this.panelSidebar.Controls.Add(this.sepNav3);
            this.panelSidebar.Controls.Add(this.sepNav2);
            this.panelSidebar.Controls.Add(this.sepNav1);
            this.panelSidebar.Controls.Add(this.sepProfile);
            this.panelSidebar.Controls.Add(this.panelProfile);
            this.panelSidebar.Controls.Add(this.sepBottom);
            this.panelSidebar.Controls.Add(this.btnSignOut);
            this.panelSidebar.Controls.Add(this.lblBrand);
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelSidebar.Name = "panelSidebar";
            this.panelSidebar.Size = new System.Drawing.Size(220, 749);
            this.panelSidebar.TabIndex = 0;
            // 
            // btnActivityLog
            // 
            this.btnActivityLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnActivityLog.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnActivityLog.FlatAppearance.BorderSize = 0;
            this.btnActivityLog.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnActivityLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActivityLog.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnActivityLog.ForeColor = System.Drawing.Color.White;
            this.btnActivityLog.Location = new System.Drawing.Point(0, 446);
            this.btnActivityLog.Name = "btnActivityLog";
            this.btnActivityLog.Size = new System.Drawing.Size(220, 42);
            this.btnActivityLog.TabIndex = 15;
            this.btnActivityLog.Text = "  Activity Log";
            this.btnActivityLog.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnActivityLog.UseVisualStyleBackColor = false;
            this.btnActivityLog.Click += new System.EventHandler(this.BtnActivityLog_Click);
            // 
            // btnBilling
            // 
            this.btnBilling.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnBilling.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBilling.FlatAppearance.BorderSize = 0;
            this.btnBilling.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnBilling.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBilling.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnBilling.ForeColor = System.Drawing.Color.White;
            this.btnBilling.Location = new System.Drawing.Point(0, 396);
            this.btnBilling.Name = "btnBilling";
            this.btnBilling.Size = new System.Drawing.Size(220, 42);
            this.btnBilling.TabIndex = 13;
            this.btnBilling.Text = "  Billing";
            this.btnBilling.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBilling.UseVisualStyleBackColor = false;
            this.btnBilling.Click += new System.EventHandler(this.BtnBilling_Click);
            // 
            // btnAdmissions
            // 
            this.btnAdmissions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnAdmissions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdmissions.FlatAppearance.BorderSize = 0;
            this.btnAdmissions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnAdmissions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdmissions.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAdmissions.ForeColor = System.Drawing.Color.White;
            this.btnAdmissions.Location = new System.Drawing.Point(0, 346);
            this.btnAdmissions.Name = "btnAdmissions";
            this.btnAdmissions.Size = new System.Drawing.Size(220, 42);
            this.btnAdmissions.TabIndex = 11;
            this.btnAdmissions.Text = "  Admissions";
            this.btnAdmissions.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAdmissions.UseVisualStyleBackColor = false;
            this.btnAdmissions.Click += new System.EventHandler(this.BtnAdmissions_Click);
            // 
            // btnAppointments
            // 
            this.btnAppointments.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnAppointments.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAppointments.FlatAppearance.BorderSize = 0;
            this.btnAppointments.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnAppointments.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAppointments.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAppointments.ForeColor = System.Drawing.Color.White;
            this.btnAppointments.Location = new System.Drawing.Point(0, 296);
            this.btnAppointments.Name = "btnAppointments";
            this.btnAppointments.Size = new System.Drawing.Size(220, 42);
            this.btnAppointments.TabIndex = 9;
            this.btnAppointments.Text = "  Appointments";
            this.btnAppointments.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAppointments.UseVisualStyleBackColor = false;
            this.btnAppointments.Click += new System.EventHandler(this.BtnAppointments_Click);
            // 
            // btnDoctors
            // 
            this.btnDoctors.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnDoctors.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDoctors.FlatAppearance.BorderSize = 0;
            this.btnDoctors.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnDoctors.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDoctors.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDoctors.ForeColor = System.Drawing.Color.White;
            this.btnDoctors.Location = new System.Drawing.Point(0, 246);
            this.btnDoctors.Name = "btnDoctors";
            this.btnDoctors.Size = new System.Drawing.Size(220, 42);
            this.btnDoctors.TabIndex = 7;
            this.btnDoctors.Text = "  Doctors";
            this.btnDoctors.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDoctors.UseVisualStyleBackColor = false;
            this.btnDoctors.Click += new System.EventHandler(this.BtnDoctors_Click);
            // 
            // btnPatients
            // 
            this.btnPatients.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnPatients.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPatients.FlatAppearance.BorderSize = 0;
            this.btnPatients.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnPatients.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPatients.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPatients.ForeColor = System.Drawing.Color.White;
            this.btnPatients.Location = new System.Drawing.Point(0, 196);
            this.btnPatients.Name = "btnPatients";
            this.btnPatients.Size = new System.Drawing.Size(220, 42);
            this.btnPatients.TabIndex = 5;
            this.btnPatients.Text = "  Patients";
            this.btnPatients.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnPatients.UseVisualStyleBackColor = false;
            this.btnPatients.Click += new System.EventHandler(this.BtnPatients_Click);
            // 
            // btnDashboard
            // 
            this.btnDashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnDashboard.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDashboard.FlatAppearance.BorderSize = 0;
            this.btnDashboard.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDashboard.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDashboard.ForeColor = System.Drawing.Color.White;
            this.btnDashboard.Location = new System.Drawing.Point(0, 146);
            this.btnDashboard.Name = "btnDashboard";
            this.btnDashboard.Size = new System.Drawing.Size(220, 42);
            this.btnDashboard.TabIndex = 3;
            this.btnDashboard.Text = "  Dashboard";
            this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.UseVisualStyleBackColor = false;
            this.btnDashboard.Click += new System.EventHandler(this.BtnDashboard_Click);
            // 
            // sepActivityLog
            // 
            this.sepActivityLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepActivityLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(82)))), ((int)(((byte)(165)))));
            this.sepActivityLog.Location = new System.Drawing.Point(16, 442);
            this.sepActivityLog.Name = "sepActivityLog";
            this.sepActivityLog.Size = new System.Drawing.Size(188, 1);
            this.sepActivityLog.TabIndex = 14;
            // 
            // sepNav5
            // 
            this.sepNav5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepNav5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(82)))), ((int)(((byte)(165)))));
            this.sepNav5.Location = new System.Drawing.Point(16, 392);
            this.sepNav5.Name = "sepNav5";
            this.sepNav5.Size = new System.Drawing.Size(188, 1);
            this.sepNav5.TabIndex = 12;
            // 
            // sepNav4
            // 
            this.sepNav4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepNav4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(82)))), ((int)(((byte)(165)))));
            this.sepNav4.Location = new System.Drawing.Point(16, 342);
            this.sepNav4.Name = "sepNav4";
            this.sepNav4.Size = new System.Drawing.Size(188, 1);
            this.sepNav4.TabIndex = 10;
            // 
            // sepNav3
            // 
            this.sepNav3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepNav3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(82)))), ((int)(((byte)(165)))));
            this.sepNav3.Location = new System.Drawing.Point(16, 292);
            this.sepNav3.Name = "sepNav3";
            this.sepNav3.Size = new System.Drawing.Size(188, 1);
            this.sepNav3.TabIndex = 8;
            // 
            // sepNav2
            // 
            this.sepNav2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepNav2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(82)))), ((int)(((byte)(165)))));
            this.sepNav2.Location = new System.Drawing.Point(16, 242);
            this.sepNav2.Name = "sepNav2";
            this.sepNav2.Size = new System.Drawing.Size(188, 1);
            this.sepNav2.TabIndex = 6;
            // 
            // sepNav1
            // 
            this.sepNav1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepNav1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(82)))), ((int)(((byte)(165)))));
            this.sepNav1.Location = new System.Drawing.Point(16, 192);
            this.sepNav1.Name = "sepNav1";
            this.sepNav1.Size = new System.Drawing.Size(188, 1);
            this.sepNav1.TabIndex = 4;
            // 
            // sepProfile
            // 
            this.sepProfile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sepProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(115)))), ((int)(((byte)(205)))));
            this.sepProfile.Location = new System.Drawing.Point(16, 141);
            this.sepProfile.Name = "sepProfile";
            this.sepProfile.Size = new System.Drawing.Size(188, 1);
            this.sepProfile.TabIndex = 2;
            // 
            // panelProfile
            // 
            this.panelProfile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(64)))), ((int)(((byte)(150)))));
            this.panelProfile.Controls.Add(this.lblProfileRole);
            this.panelProfile.Controls.Add(this.lblProfileName);
            this.panelProfile.Location = new System.Drawing.Point(0, 60);
            this.panelProfile.Name = "panelProfile";
            this.panelProfile.Size = new System.Drawing.Size(220, 78);
            this.panelProfile.TabIndex = 1;
            this.panelProfile.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelProfile_Paint);
            // 
            // lblProfileRole
            // 
            this.lblProfileRole.AutoSize = true;
            this.lblProfileRole.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblProfileRole.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblProfileRole.ForeColor = System.Drawing.Color.White;
            this.lblProfileRole.Location = new System.Drawing.Point(70, 42);
            this.lblProfileRole.Name = "lblProfileRole";
            this.lblProfileRole.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.lblProfileRole.Size = new System.Drawing.Size(42, 17);
            this.lblProfileRole.TabIndex = 1;
            this.lblProfileRole.Text = "Role";
            // 
            // lblProfileName
            // 
            this.lblProfileName.AutoEllipsis = true;
            this.lblProfileName.BackColor = System.Drawing.Color.Transparent;
            this.lblProfileName.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblProfileName.ForeColor = System.Drawing.Color.White;
            this.lblProfileName.Location = new System.Drawing.Point(70, 16);
            this.lblProfileName.Name = "lblProfileName";
            this.lblProfileName.Size = new System.Drawing.Size(140, 22);
            this.lblProfileName.TabIndex = 0;
            this.lblProfileName.Text = "User Name";
            // 
            // sepBottom
            // 
            this.sepBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(115)))), ((int)(((byte)(205)))));
            this.sepBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sepBottom.Location = new System.Drawing.Point(0, 703);
            this.sepBottom.Name = "sepBottom";
            this.sepBottom.Size = new System.Drawing.Size(220, 1);
            this.sepBottom.TabIndex = 16;
            // 
            // btnSignOut
            // 
            this.btnSignOut.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.btnSignOut.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSignOut.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnSignOut.FlatAppearance.BorderSize = 0;
            this.btnSignOut.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnSignOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSignOut.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSignOut.ForeColor = System.Drawing.Color.White;
            this.btnSignOut.Location = new System.Drawing.Point(0, 704);
            this.btnSignOut.Name = "btnSignOut";
            this.btnSignOut.Size = new System.Drawing.Size(220, 45);
            this.btnSignOut.TabIndex = 17;
            this.btnSignOut.Text = "  Sign Out";
            this.btnSignOut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSignOut.UseVisualStyleBackColor = false;
            this.btnSignOut.Click += new System.EventHandler(this.BtnSignOut_Click);
            // 
            // lblBrand
            // 
            this.lblBrand.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBrand.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblBrand.ForeColor = System.Drawing.Color.White;
            this.lblBrand.Location = new System.Drawing.Point(0, 0);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new System.Drawing.Size(220, 60);
            this.lblBrand.TabIndex = 0;
            this.lblBrand.Text = "  Hospital System";
            this.lblBrand.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelContent
            // 
            this.panelContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.Location = new System.Drawing.Point(220, 0);
            this.panelContent.Name = "panelContent";
            this.panelContent.Padding = new System.Windows.Forms.Padding(18);
            this.panelContent.Size = new System.Drawing.Size(1044, 749);
            this.panelContent.TabIndex = 1;
            // 
            // DashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.ClientSize = new System.Drawing.Size(1264, 749);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelSidebar);
            this.MinimumSize = new System.Drawing.Size(1100, 718);
            this.Name = "DashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hospital System";
            this.Load += new System.EventHandler(this.DashboardForm_Load);
            this.panelSidebar.ResumeLayout(false);
            this.panelProfile.ResumeLayout(false);
            this.panelProfile.PerformLayout();
            this.ResumeLayout(false);

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

            bool seesFinancials = Permissions.Can(Permission.ViewFinancials);

            // ===== 0. Whose view this is =====
            var banner = BuildRoleBanner(seesFinancials);

            // ===== 1. Summary cards (administrators also get the money owed to the hospital) =====
            int occupied = HospitalData.OccupiedBedsCount();
            int totalBeds = HospitalData.TotalBedsCount();
            double rate = HospitalData.OccupancyRate();
            var today = HospitalData.AppointmentsToday();
            int todayOpen = today.Count(a => a.IsOpen);
            int next7 = UpcomingAppointments().Count;
            var pending = HospitalData.PendingAdmissions();
            var alerts = VisibleAlerts(HospitalData.ActiveAlerts(), seesFinancials);
            int highAlerts = alerts.Count(a => a.Severity == "High");

            var cards = new System.Collections.Generic.List<Panel>
            {
                CreateSummaryCard("Today's Visits", today.Count.ToString(),
                    $"{todayOpen} to see  •  {next7} this week", Navy),
                CreateSummaryCard("Bed Occupancy", $"{occupied}/{totalBeds}",
                    $"{rate:0}% occupied  •  {totalBeds - occupied} free",
                    rate >= 100 ? Red : rate >= AlertMonitor.HighOccupancyRate * 100 ? Amber : Navy),
                CreateSummaryCard("Admitted Patients", HospitalData.ActiveAdmissions().Count.ToString(),
                    "currently in a bed", Navy),
                CreateSummaryCard("Pending Admissions", pending.Count.ToString(),
                    pending.Count == 0 ? "no one waiting" : "longest wait " + HospitalData.FormatDuration(pending[0].WaitingTime),
                    pending.Count == 0 ? Navy : Amber),
                CreateSummaryCard("Active Alerts", alerts.Count.ToString(),
                    alerts.Count == 0 ? "all clear" : $"{highAlerts} high priority",
                    highAlerts > 0 ? Red : alerts.Count > 0 ? Amber : Green)
            };
            if (seesFinancials)
            {
                decimal patientsOwe = HospitalData.OutstandingBalance(), hmosOwe = HospitalData.OutstandingHmo();
                cards.Add(CreateSummaryCard("Outstanding", (patientsOwe + hmosOwe).ToString("N0"),
                    $"patients {patientsOwe:N0}  •  HMO {hmosOwe:N0}", patientsOwe + hmosOwe > 0 ? Amber : Green));
            }

            var tlTop = new TableLayoutPanel();
            tlTop.Dock = DockStyle.Top;
            tlTop.Height = 112;
            tlTop.ColumnCount = cards.Count;
            tlTop.RowCount = 1;
            for (int i = 0; i < cards.Count; i++)
            {
                tlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / cards.Count));
                tlTop.Controls.Add(cards[i], i, 0);
            }

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
            overview.Controls.Add(banner);

            ShowView(new HostView(overview));
        }

        // Billing alerts are about money owed to the hospital, which only administrators handle.
        private static System.Collections.Generic.List<Alert> VisibleAlerts(System.Collections.Generic.List<Alert> alerts, bool seesFinancials)
        {
            return seesFinancials ? alerts : alerts.Where(a => a.Module != "Billing").ToList();
        }

        private Panel BuildRoleBanner(bool admin)
        {
            var user = HospitalData.CurrentUser;
            int hour = DateTime.Now.Hour;
            string greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
            Color accent = admin ? Color.FromArgb(217, 119, 6) : Color.FromArgb(13, 148, 136);

            var banner = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(5, 0, 5, 8) };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += (s, e) =>
            {
                using (var pen = new Pen(accent, 4))
                    e.Graphics.DrawLine(pen, 1, 0, 1, inner.Height);
            };

            inner.Controls.Add(new Label
            {
                Text = admin
                    ? "Administrator view  ·  full hospital overview, billing totals and the activity log"
                    : "Nurse view  ·  clinical overview; billing totals, payments and admin tools are handled by administrators",
                UseMnemonic = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(75, 85, 99)
            });
            inner.Controls.Add(new Label
            {
                Text = $"{greeting}, {(user != null ? user.DisplayName : "")}",
                Dock = DockStyle.Left,
                Width = 320,
                Padding = new Padding(12, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = accent
            });

            banner.Controls.Add(inner);
            return banner;
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
            int acknowledged = VisibleAlerts(HospitalData.AcknowledgedAlerts(), Permissions.Can(Permission.ViewFinancials)).Count;
            var title = MakeCardTitle("Alerts" + (acknowledged > 0 ? $"    ({acknowledged} acknowledged, still ongoing)" : ""), Red);

            var hint = new Label
            {
                Text = Permissions.Can(Permission.ViewFinancials)
                    ? "Raised automatically by the other modules; each clears itself once fixed."
                    : "Clinical alerts, raised automatically; each clears itself once fixed.",
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

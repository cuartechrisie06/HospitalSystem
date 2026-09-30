using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // The dashboard overview: who is signed in, summary cards, alerts, the admission waiting
    // list, appointments, bed occupancy by ward, doctors on duty and two charts.
    //
    // The whole layout lives in InitializeComponent() (Designer format) so the Designer shows
    // every card and list; LoadData() only fills in the numbers, rows and alert items.
    public class DashboardView : UserControl
    {
        // Raised when the user asks to jump to a module (from an alert or by double-clicking a list).
        public event Action<string> ModuleRequested;

        private Panel scrollPanel;
        private Panel bannerPanel;
        private Panel bannerInner;
        private Label lblGreeting;
        private Label lblRoleView;
        private TableLayoutPanel cardsTable;
        private Panel cardAppointments;
        private Label lblCardAppointmentsTitle;
        private Label lblCardAppointmentsValue;
        private Label lblCardAppointmentsSub;
        private Panel cardOccupancy;
        private Label lblCardOccupancyTitle;
        private Label lblCardOccupancyValue;
        private Label lblCardOccupancySub;
        private Panel cardAdmitted;
        private Label lblCardAdmittedTitle;
        private Label lblCardAdmittedValue;
        private Label lblCardAdmittedSub;
        private Panel cardPending;
        private Label lblCardPendingTitle;
        private Label lblCardPendingValue;
        private Label lblCardPendingSub;
        private Panel cardAlerts;
        private Label lblCardAlertsTitle;
        private Label lblCardAlertsValue;
        private Label lblCardAlertsSub;
        private Panel cardOutstanding;
        private Label lblCardOutstandingTitle;
        private Label lblCardOutstandingValue;
        private Label lblCardOutstandingSub;
        private TableLayoutPanel rowAlerts;
        private Panel alertsCard;
        private Label lblAlertsTitle;
        private Label lblAlertsHint;
        private FlowLayoutPanel flAlerts;
        private Label lblNoAlerts;
        private Panel pendingCard;
        private Label lblPendingTitle;
        private ListView lvPending;
        private ColumnHeader colPendingNo;
        private ColumnHeader colPendingPatient;
        private ColumnHeader colPendingDoctor;
        private ColumnHeader colPendingWaiting;
        private ColumnHeader colPendingReason;
        private Label lblPendingFooter;
        private TableLayoutPanel rowAppointments;
        private Panel appointmentsCard;
        private Label lblAppointmentsTitle;
        private ListView lvAppointments;
        private ColumnHeader colApptDate;
        private ColumnHeader colApptTime;
        private ColumnHeader colApptPatient;
        private ColumnHeader colApptDoctor;
        private ColumnHeader colApptStatus;
        private Panel occupancyCard;
        private Label lblOccupancyTitle;
        private Panel occupancyBars;
        private TableLayoutPanel rowBottom;
        private Panel doctorsCard;
        private Label lblDoctorsTitle;
        private ListView lvDoctors;
        private ColumnHeader colDocName;
        private ColumnHeader colDocSpecialization;
        private ColumnHeader colDocDepartment;
        private Panel chartAppointmentsCard;
        private Label lblChartAppointments;
        private Panel chartAppointments;
        private Panel chartAdmissionsCard;
        private Label lblChartAdmissions;
        private Panel chartAdmissions;

        private static readonly Color Navy = Color.FromArgb(30, 58, 138);
        private static readonly Color Red = Color.FromArgb(185, 28, 28);
        private static readonly Color Amber = Color.FromArgb(217, 119, 6);
        private static readonly Color Green = Color.FromArgb(22, 101, 52);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);

        // State the paint handlers draw from (set by LoadData).
        private readonly Dictionary<Control, Color> cardAccents = new Dictionary<Control, Color>();
        private Color bannerAccent = Color.FromArgb(30, 58, 138);
        private List<WardOccupancy> wards = new List<WardOccupancy>();
        private List<KeyValuePair<string, int>> appointmentsPerDay = new List<KeyValuePair<string, int>>();
        private List<KeyValuePair<string, int>> admissionsPerMonth = new List<KeyValuePair<string, int>>();
        private readonly Font boldRow = new Font("Segoe UI", 9F, FontStyle.Bold);

        private class WardOccupancy
        {
            public string Ward;
            public int Total, Occupied;
        }

        public DashboardView()
        {
            InitializeComponent();

            // The Designer instantiates this class to render it at design time;
            // data loading must never run then, or it tries to open a DB connection.
            if (!DesignTimeHelper.IsDesignMode)
                LoadData();
        }

        // Designer-generated layout, top to bottom inside a scrolling panel: the role banner,
        // six summary cards, Alerts | Pending Admissions, Appointments | Bed Occupancy by Ward,
        // and Doctors on Duty | two bar charts.
        private void InitializeComponent()
        {
            this.scrollPanel = new System.Windows.Forms.Panel();
            this.bannerPanel = new System.Windows.Forms.Panel();
            this.bannerInner = new System.Windows.Forms.Panel();
            this.lblGreeting = new System.Windows.Forms.Label();
            this.lblRoleView = new System.Windows.Forms.Label();
            this.cardsTable = new System.Windows.Forms.TableLayoutPanel();
            this.cardAppointments = new System.Windows.Forms.Panel();
            this.lblCardAppointmentsTitle = new System.Windows.Forms.Label();
            this.lblCardAppointmentsValue = new System.Windows.Forms.Label();
            this.lblCardAppointmentsSub = new System.Windows.Forms.Label();
            this.cardOccupancy = new System.Windows.Forms.Panel();
            this.lblCardOccupancyTitle = new System.Windows.Forms.Label();
            this.lblCardOccupancyValue = new System.Windows.Forms.Label();
            this.lblCardOccupancySub = new System.Windows.Forms.Label();
            this.cardAdmitted = new System.Windows.Forms.Panel();
            this.lblCardAdmittedTitle = new System.Windows.Forms.Label();
            this.lblCardAdmittedValue = new System.Windows.Forms.Label();
            this.lblCardAdmittedSub = new System.Windows.Forms.Label();
            this.cardPending = new System.Windows.Forms.Panel();
            this.lblCardPendingTitle = new System.Windows.Forms.Label();
            this.lblCardPendingValue = new System.Windows.Forms.Label();
            this.lblCardPendingSub = new System.Windows.Forms.Label();
            this.cardAlerts = new System.Windows.Forms.Panel();
            this.lblCardAlertsTitle = new System.Windows.Forms.Label();
            this.lblCardAlertsValue = new System.Windows.Forms.Label();
            this.lblCardAlertsSub = new System.Windows.Forms.Label();
            this.cardOutstanding = new System.Windows.Forms.Panel();
            this.lblCardOutstandingTitle = new System.Windows.Forms.Label();
            this.lblCardOutstandingValue = new System.Windows.Forms.Label();
            this.lblCardOutstandingSub = new System.Windows.Forms.Label();
            this.rowAlerts = new System.Windows.Forms.TableLayoutPanel();
            this.alertsCard = new System.Windows.Forms.Panel();
            this.lblAlertsTitle = new System.Windows.Forms.Label();
            this.lblAlertsHint = new System.Windows.Forms.Label();
            this.flAlerts = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNoAlerts = new System.Windows.Forms.Label();
            this.pendingCard = new System.Windows.Forms.Panel();
            this.lblPendingTitle = new System.Windows.Forms.Label();
            this.lvPending = new System.Windows.Forms.ListView();
            this.colPendingNo = new System.Windows.Forms.ColumnHeader();
            this.colPendingPatient = new System.Windows.Forms.ColumnHeader();
            this.colPendingDoctor = new System.Windows.Forms.ColumnHeader();
            this.colPendingWaiting = new System.Windows.Forms.ColumnHeader();
            this.colPendingReason = new System.Windows.Forms.ColumnHeader();
            this.lblPendingFooter = new System.Windows.Forms.Label();
            this.rowAppointments = new System.Windows.Forms.TableLayoutPanel();
            this.appointmentsCard = new System.Windows.Forms.Panel();
            this.lblAppointmentsTitle = new System.Windows.Forms.Label();
            this.lvAppointments = new System.Windows.Forms.ListView();
            this.colApptDate = new System.Windows.Forms.ColumnHeader();
            this.colApptTime = new System.Windows.Forms.ColumnHeader();
            this.colApptPatient = new System.Windows.Forms.ColumnHeader();
            this.colApptDoctor = new System.Windows.Forms.ColumnHeader();
            this.colApptStatus = new System.Windows.Forms.ColumnHeader();
            this.occupancyCard = new System.Windows.Forms.Panel();
            this.lblOccupancyTitle = new System.Windows.Forms.Label();
            this.occupancyBars = new System.Windows.Forms.Panel();
            this.rowBottom = new System.Windows.Forms.TableLayoutPanel();
            this.doctorsCard = new System.Windows.Forms.Panel();
            this.lblDoctorsTitle = new System.Windows.Forms.Label();
            this.lvDoctors = new System.Windows.Forms.ListView();
            this.colDocName = new System.Windows.Forms.ColumnHeader();
            this.colDocSpecialization = new System.Windows.Forms.ColumnHeader();
            this.colDocDepartment = new System.Windows.Forms.ColumnHeader();
            this.chartAppointmentsCard = new System.Windows.Forms.Panel();
            this.lblChartAppointments = new System.Windows.Forms.Label();
            this.chartAppointments = new System.Windows.Forms.Panel();
            this.chartAdmissionsCard = new System.Windows.Forms.Panel();
            this.lblChartAdmissions = new System.Windows.Forms.Label();
            this.chartAdmissions = new System.Windows.Forms.Panel();
            this.scrollPanel.SuspendLayout();
            this.bannerPanel.SuspendLayout();
            this.bannerInner.SuspendLayout();
            this.cardsTable.SuspendLayout();
            this.cardAppointments.SuspendLayout();
            this.cardOccupancy.SuspendLayout();
            this.cardAdmitted.SuspendLayout();
            this.cardPending.SuspendLayout();
            this.cardAlerts.SuspendLayout();
            this.cardOutstanding.SuspendLayout();
            this.rowAlerts.SuspendLayout();
            this.alertsCard.SuspendLayout();
            this.flAlerts.SuspendLayout();
            this.pendingCard.SuspendLayout();
            this.rowAppointments.SuspendLayout();
            this.appointmentsCard.SuspendLayout();
            this.occupancyCard.SuspendLayout();
            this.rowBottom.SuspendLayout();
            this.doctorsCard.SuspendLayout();
            this.chartAppointmentsCard.SuspendLayout();
            this.chartAdmissionsCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // scrollPanel
            // Rows are docked to the top; the last one added (bannerPanel) sits highest.
            // 
            this.scrollPanel.AutoScroll = true;
            this.scrollPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.scrollPanel.Controls.Add(this.rowBottom);
            this.scrollPanel.Controls.Add(this.rowAppointments);
            this.scrollPanel.Controls.Add(this.rowAlerts);
            this.scrollPanel.Controls.Add(this.cardsTable);
            this.scrollPanel.Controls.Add(this.bannerPanel);
            this.scrollPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scrollPanel.Name = "scrollPanel";
            this.scrollPanel.Size = new System.Drawing.Size(1024, 760);
            this.scrollPanel.TabIndex = 0;
            // 
            // bannerPanel
            // 
            this.bannerPanel.Controls.Add(this.bannerInner);
            this.bannerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.bannerPanel.Name = "bannerPanel";
            this.bannerPanel.Padding = new System.Windows.Forms.Padding(5, 0, 5, 8);
            this.bannerPanel.Size = new System.Drawing.Size(1024, 50);
            this.bannerPanel.TabIndex = 1;
            // 
            // bannerInner
            // 
            this.bannerInner.BackColor = System.Drawing.Color.White;
            this.bannerInner.Controls.Add(this.lblRoleView);
            this.bannerInner.Controls.Add(this.lblGreeting);
            this.bannerInner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bannerInner.Name = "bannerInner";
            this.bannerInner.TabIndex = 2;
            this.bannerInner.Paint += new System.Windows.Forms.PaintEventHandler(this.BannerInner_Paint);
            // 
            // lblGreeting
            // 
            this.lblGreeting.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblGreeting.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblGreeting.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblGreeting.Name = "lblGreeting";
            this.lblGreeting.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblGreeting.Size = new System.Drawing.Size(320, 42);
            this.lblGreeting.TabIndex = 3;
            this.lblGreeting.Text = "Good day";
            this.lblGreeting.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRoleView
            // 
            this.lblRoleView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoleView.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRoleView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.lblRoleView.Name = "lblRoleView";
            this.lblRoleView.TabIndex = 4;
            this.lblRoleView.Text = "Role view  ·  what this role sees on the dashboard";
            this.lblRoleView.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRoleView.UseMnemonic = false;
            // 
            // cardsTable
            // Six summary cards. The Outstanding card is removed at runtime for roles without financial access.
            // 
            this.cardsTable.ColumnCount = 6;
            this.cardsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cardsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cardsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cardsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cardsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cardsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cardsTable.Controls.Add(this.cardAppointments, 0, 0);
            this.cardsTable.Controls.Add(this.cardOccupancy, 1, 0);
            this.cardsTable.Controls.Add(this.cardAdmitted, 2, 0);
            this.cardsTable.Controls.Add(this.cardPending, 3, 0);
            this.cardsTable.Controls.Add(this.cardAlerts, 4, 0);
            this.cardsTable.Controls.Add(this.cardOutstanding, 5, 0);
            this.cardsTable.Dock = System.Windows.Forms.DockStyle.Top;
            this.cardsTable.Name = "cardsTable";
            this.cardsTable.RowCount = 1;
            this.cardsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cardsTable.Size = new System.Drawing.Size(1024, 112);
            this.cardsTable.TabIndex = 5;
            // 
            // cardAppointments
            // 
            this.cardAppointments.BackColor = System.Drawing.Color.White;
            this.cardAppointments.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardAppointments.Controls.Add(this.lblCardAppointmentsValue);
            this.cardAppointments.Controls.Add(this.lblCardAppointmentsSub);
            this.cardAppointments.Controls.Add(this.lblCardAppointmentsTitle);
            this.cardAppointments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardAppointments.Margin = new System.Windows.Forms.Padding(5);
            this.cardAppointments.Name = "cardAppointments";
            this.cardAppointments.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cardAppointments.TabIndex = 6;
            this.cardAppointments.Paint += new System.Windows.Forms.PaintEventHandler(this.Card_Paint);
            // 
            // lblCardAppointmentsTitle
            // 
            this.lblCardAppointmentsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCardAppointmentsTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCardAppointmentsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblCardAppointmentsTitle.Name = "lblCardAppointmentsTitle";
            this.lblCardAppointmentsTitle.Size = new System.Drawing.Size(134, 20);
            this.lblCardAppointmentsTitle.TabIndex = 7;
            this.lblCardAppointmentsTitle.Text = "Today's Visits";
            // 
            // lblCardAppointmentsValue
            // 
            this.lblCardAppointmentsValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardAppointmentsValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCardAppointmentsValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblCardAppointmentsValue.Name = "lblCardAppointmentsValue";
            this.lblCardAppointmentsValue.TabIndex = 8;
            this.lblCardAppointmentsValue.Text = "0";
            this.lblCardAppointmentsValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCardAppointmentsSub
            // 
            this.lblCardAppointmentsSub.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCardAppointmentsSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCardAppointmentsSub.ForeColor = System.Drawing.Color.Gray;
            this.lblCardAppointmentsSub.Name = "lblCardAppointmentsSub";
            this.lblCardAppointmentsSub.Size = new System.Drawing.Size(134, 18);
            this.lblCardAppointmentsSub.TabIndex = 9;
            this.lblCardAppointmentsSub.Text = "to see  •  this week";
            // 
            // cardOccupancy
            // 
            this.cardOccupancy.BackColor = System.Drawing.Color.White;
            this.cardOccupancy.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardOccupancy.Controls.Add(this.lblCardOccupancyValue);
            this.cardOccupancy.Controls.Add(this.lblCardOccupancySub);
            this.cardOccupancy.Controls.Add(this.lblCardOccupancyTitle);
            this.cardOccupancy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardOccupancy.Margin = new System.Windows.Forms.Padding(5);
            this.cardOccupancy.Name = "cardOccupancy";
            this.cardOccupancy.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cardOccupancy.TabIndex = 10;
            this.cardOccupancy.Paint += new System.Windows.Forms.PaintEventHandler(this.Card_Paint);
            // 
            // lblCardOccupancyTitle
            // 
            this.lblCardOccupancyTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCardOccupancyTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCardOccupancyTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblCardOccupancyTitle.Name = "lblCardOccupancyTitle";
            this.lblCardOccupancyTitle.Size = new System.Drawing.Size(134, 20);
            this.lblCardOccupancyTitle.TabIndex = 11;
            this.lblCardOccupancyTitle.Text = "Bed Occupancy";
            // 
            // lblCardOccupancyValue
            // 
            this.lblCardOccupancyValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardOccupancyValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCardOccupancyValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblCardOccupancyValue.Name = "lblCardOccupancyValue";
            this.lblCardOccupancyValue.TabIndex = 12;
            this.lblCardOccupancyValue.Text = "0";
            this.lblCardOccupancyValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCardOccupancySub
            // 
            this.lblCardOccupancySub.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCardOccupancySub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCardOccupancySub.ForeColor = System.Drawing.Color.Gray;
            this.lblCardOccupancySub.Name = "lblCardOccupancySub";
            this.lblCardOccupancySub.Size = new System.Drawing.Size(134, 18);
            this.lblCardOccupancySub.TabIndex = 13;
            this.lblCardOccupancySub.Text = "% occupied  •  free";
            // 
            // cardAdmitted
            // 
            this.cardAdmitted.BackColor = System.Drawing.Color.White;
            this.cardAdmitted.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardAdmitted.Controls.Add(this.lblCardAdmittedValue);
            this.cardAdmitted.Controls.Add(this.lblCardAdmittedSub);
            this.cardAdmitted.Controls.Add(this.lblCardAdmittedTitle);
            this.cardAdmitted.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardAdmitted.Margin = new System.Windows.Forms.Padding(5);
            this.cardAdmitted.Name = "cardAdmitted";
            this.cardAdmitted.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cardAdmitted.TabIndex = 14;
            this.cardAdmitted.Paint += new System.Windows.Forms.PaintEventHandler(this.Card_Paint);
            // 
            // lblCardAdmittedTitle
            // 
            this.lblCardAdmittedTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCardAdmittedTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCardAdmittedTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblCardAdmittedTitle.Name = "lblCardAdmittedTitle";
            this.lblCardAdmittedTitle.Size = new System.Drawing.Size(134, 20);
            this.lblCardAdmittedTitle.TabIndex = 15;
            this.lblCardAdmittedTitle.Text = "Admitted Patients";
            // 
            // lblCardAdmittedValue
            // 
            this.lblCardAdmittedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardAdmittedValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCardAdmittedValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblCardAdmittedValue.Name = "lblCardAdmittedValue";
            this.lblCardAdmittedValue.TabIndex = 16;
            this.lblCardAdmittedValue.Text = "0";
            this.lblCardAdmittedValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCardAdmittedSub
            // 
            this.lblCardAdmittedSub.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCardAdmittedSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCardAdmittedSub.ForeColor = System.Drawing.Color.Gray;
            this.lblCardAdmittedSub.Name = "lblCardAdmittedSub";
            this.lblCardAdmittedSub.Size = new System.Drawing.Size(134, 18);
            this.lblCardAdmittedSub.TabIndex = 17;
            this.lblCardAdmittedSub.Text = "currently in a bed";
            // 
            // cardPending
            // 
            this.cardPending.BackColor = System.Drawing.Color.White;
            this.cardPending.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardPending.Controls.Add(this.lblCardPendingValue);
            this.cardPending.Controls.Add(this.lblCardPendingSub);
            this.cardPending.Controls.Add(this.lblCardPendingTitle);
            this.cardPending.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardPending.Margin = new System.Windows.Forms.Padding(5);
            this.cardPending.Name = "cardPending";
            this.cardPending.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cardPending.TabIndex = 18;
            this.cardPending.Paint += new System.Windows.Forms.PaintEventHandler(this.Card_Paint);
            // 
            // lblCardPendingTitle
            // 
            this.lblCardPendingTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCardPendingTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCardPendingTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblCardPendingTitle.Name = "lblCardPendingTitle";
            this.lblCardPendingTitle.Size = new System.Drawing.Size(134, 20);
            this.lblCardPendingTitle.TabIndex = 19;
            this.lblCardPendingTitle.Text = "Pending Admissions";
            // 
            // lblCardPendingValue
            // 
            this.lblCardPendingValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardPendingValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCardPendingValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblCardPendingValue.Name = "lblCardPendingValue";
            this.lblCardPendingValue.TabIndex = 20;
            this.lblCardPendingValue.Text = "0";
            this.lblCardPendingValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCardPendingSub
            // 
            this.lblCardPendingSub.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCardPendingSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCardPendingSub.ForeColor = System.Drawing.Color.Gray;
            this.lblCardPendingSub.Name = "lblCardPendingSub";
            this.lblCardPendingSub.Size = new System.Drawing.Size(134, 18);
            this.lblCardPendingSub.TabIndex = 21;
            this.lblCardPendingSub.Text = "waiting for a bed";
            // 
            // cardAlerts
            // 
            this.cardAlerts.BackColor = System.Drawing.Color.White;
            this.cardAlerts.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardAlerts.Controls.Add(this.lblCardAlertsValue);
            this.cardAlerts.Controls.Add(this.lblCardAlertsSub);
            this.cardAlerts.Controls.Add(this.lblCardAlertsTitle);
            this.cardAlerts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardAlerts.Margin = new System.Windows.Forms.Padding(5);
            this.cardAlerts.Name = "cardAlerts";
            this.cardAlerts.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cardAlerts.TabIndex = 22;
            this.cardAlerts.Paint += new System.Windows.Forms.PaintEventHandler(this.Card_Paint);
            // 
            // lblCardAlertsTitle
            // 
            this.lblCardAlertsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCardAlertsTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCardAlertsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblCardAlertsTitle.Name = "lblCardAlertsTitle";
            this.lblCardAlertsTitle.Size = new System.Drawing.Size(134, 20);
            this.lblCardAlertsTitle.TabIndex = 23;
            this.lblCardAlertsTitle.Text = "Active Alerts";
            // 
            // lblCardAlertsValue
            // 
            this.lblCardAlertsValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardAlertsValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCardAlertsValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblCardAlertsValue.Name = "lblCardAlertsValue";
            this.lblCardAlertsValue.TabIndex = 24;
            this.lblCardAlertsValue.Text = "0";
            this.lblCardAlertsValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCardAlertsSub
            // 
            this.lblCardAlertsSub.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCardAlertsSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCardAlertsSub.ForeColor = System.Drawing.Color.Gray;
            this.lblCardAlertsSub.Name = "lblCardAlertsSub";
            this.lblCardAlertsSub.Size = new System.Drawing.Size(134, 18);
            this.lblCardAlertsSub.TabIndex = 25;
            this.lblCardAlertsSub.Text = "high priority";
            // 
            // cardOutstanding
            // 
            this.cardOutstanding.BackColor = System.Drawing.Color.White;
            this.cardOutstanding.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardOutstanding.Controls.Add(this.lblCardOutstandingValue);
            this.cardOutstanding.Controls.Add(this.lblCardOutstandingSub);
            this.cardOutstanding.Controls.Add(this.lblCardOutstandingTitle);
            this.cardOutstanding.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardOutstanding.Margin = new System.Windows.Forms.Padding(5);
            this.cardOutstanding.Name = "cardOutstanding";
            this.cardOutstanding.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cardOutstanding.TabIndex = 26;
            this.cardOutstanding.Paint += new System.Windows.Forms.PaintEventHandler(this.Card_Paint);
            // 
            // lblCardOutstandingTitle
            // 
            this.lblCardOutstandingTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCardOutstandingTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCardOutstandingTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblCardOutstandingTitle.Name = "lblCardOutstandingTitle";
            this.lblCardOutstandingTitle.Size = new System.Drawing.Size(134, 20);
            this.lblCardOutstandingTitle.TabIndex = 27;
            this.lblCardOutstandingTitle.Text = "Outstanding";
            // 
            // lblCardOutstandingValue
            // 
            this.lblCardOutstandingValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardOutstandingValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCardOutstandingValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblCardOutstandingValue.Name = "lblCardOutstandingValue";
            this.lblCardOutstandingValue.TabIndex = 28;
            this.lblCardOutstandingValue.Text = "0";
            this.lblCardOutstandingValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCardOutstandingSub
            // 
            this.lblCardOutstandingSub.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCardOutstandingSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCardOutstandingSub.ForeColor = System.Drawing.Color.Gray;
            this.lblCardOutstandingSub.Name = "lblCardOutstandingSub";
            this.lblCardOutstandingSub.Size = new System.Drawing.Size(134, 18);
            this.lblCardOutstandingSub.TabIndex = 29;
            this.lblCardOutstandingSub.Text = "patients  •  HMO (administrators only)";
            // 
            // rowAlerts
            // 
            this.rowAlerts.ColumnCount = 2;
            this.rowAlerts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.rowAlerts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rowAlerts.Controls.Add(this.alertsCard, 0, 0);
            this.rowAlerts.Controls.Add(this.pendingCard, 1, 0);
            this.rowAlerts.Dock = System.Windows.Forms.DockStyle.Top;
            this.rowAlerts.Name = "rowAlerts";
            this.rowAlerts.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.rowAlerts.RowCount = 1;
            this.rowAlerts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rowAlerts.Size = new System.Drawing.Size(1024, 240);
            this.rowAlerts.TabIndex = 30;
            // 
            // alertsCard
            // 
            this.alertsCard.BackColor = System.Drawing.Color.White;
            this.alertsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.alertsCard.Controls.Add(this.flAlerts);
            this.alertsCard.Controls.Add(this.lblAlertsHint);
            this.alertsCard.Controls.Add(this.lblAlertsTitle);
            this.alertsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alertsCard.Margin = new System.Windows.Forms.Padding(5);
            this.alertsCard.Name = "alertsCard";
            this.alertsCard.Padding = new System.Windows.Forms.Padding(10);
            this.alertsCard.TabIndex = 31;
            // 
            // lblAlertsTitle
            // 
            this.lblAlertsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAlertsTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAlertsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.lblAlertsTitle.Name = "lblAlertsTitle";
            this.lblAlertsTitle.Size = new System.Drawing.Size(200, 26);
            this.lblAlertsTitle.TabIndex = 32;
            this.lblAlertsTitle.Text = "Alerts";
            this.lblAlertsTitle.UseMnemonic = false;
            // 
            // lblAlertsHint
            // 
            this.lblAlertsHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAlertsHint.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblAlertsHint.ForeColor = System.Drawing.Color.Gray;
            this.lblAlertsHint.Name = "lblAlertsHint";
            this.lblAlertsHint.Size = new System.Drawing.Size(200, 18);
            this.lblAlertsHint.TabIndex = 33;
            this.lblAlertsHint.Text = "Raised automatically by the other modules; each clears itself once fixed.";
            // 
            // flAlerts
            // One row per active alert is added at runtime; lblNoAlerts shows when there are none.
            // 
            this.flAlerts.AutoScroll = true;
            this.flAlerts.Controls.Add(this.lblNoAlerts);
            this.flAlerts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flAlerts.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flAlerts.Name = "flAlerts";
            this.flAlerts.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.flAlerts.TabIndex = 34;
            this.flAlerts.WrapContents = false;
            this.flAlerts.Resize += new System.EventHandler(this.FlAlerts_Resize);
            // 
            // lblNoAlerts
            // 
            this.lblNoAlerts.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(253)))), ((int)(((byte)(244)))));
            this.lblNoAlerts.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblNoAlerts.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(101)))), ((int)(((byte)(52)))));
            this.lblNoAlerts.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.lblNoAlerts.Name = "lblNoAlerts";
            this.lblNoAlerts.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNoAlerts.Size = new System.Drawing.Size(420, 40);
            this.lblNoAlerts.TabIndex = 35;
            this.lblNoAlerts.Text = "✔  All clear — no active alerts";
            this.lblNoAlerts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pendingCard
            // 
            this.pendingCard.BackColor = System.Drawing.Color.White;
            this.pendingCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pendingCard.Controls.Add(this.lvPending);
            this.pendingCard.Controls.Add(this.lblPendingFooter);
            this.pendingCard.Controls.Add(this.lblPendingTitle);
            this.pendingCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pendingCard.Margin = new System.Windows.Forms.Padding(5);
            this.pendingCard.Name = "pendingCard";
            this.pendingCard.Padding = new System.Windows.Forms.Padding(10);
            this.pendingCard.TabIndex = 36;
            // 
            // lblPendingTitle
            // 
            this.lblPendingTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPendingTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPendingTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblPendingTitle.Name = "lblPendingTitle";
            this.lblPendingTitle.Size = new System.Drawing.Size(200, 26);
            this.lblPendingTitle.TabIndex = 37;
            this.lblPendingTitle.Text = "Pending Admissions (waiting for a bed)";
            this.lblPendingTitle.UseMnemonic = false;
            // 
            // lvPending
            // 
            this.lvPending.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvPending.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colPendingNo,
            this.colPendingPatient,
            this.colPendingDoctor,
            this.colPendingWaiting,
            this.colPendingReason});
            this.lvPending.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvPending.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lvPending.FullRowSelect = true;
            this.lvPending.GridLines = true;
            this.lvPending.HideSelection = false;
            this.lvPending.Name = "lvPending";
            this.lvPending.TabIndex = 38;
            this.lvPending.UseCompatibleStateImageBehavior = false;
            this.lvPending.View = System.Windows.Forms.View.Details;
            this.lvPending.DoubleClick += new System.EventHandler(this.LvPending_DoubleClick);
            // 
            // colPendingNo
            // 
            this.colPendingNo.Text = "#";
            this.colPendingNo.Width = 30;
            // 
            // colPendingPatient
            // 
            this.colPendingPatient.Text = "Patient";
            this.colPendingPatient.Width = 130;
            // 
            // colPendingDoctor
            // 
            this.colPendingDoctor.Text = "Doctor";
            this.colPendingDoctor.Width = 110;
            // 
            // colPendingWaiting
            // 
            this.colPendingWaiting.Text = "Waiting";
            this.colPendingWaiting.Width = 80;
            // 
            // colPendingReason
            // 
            this.colPendingReason.Text = "Reason";
            this.colPendingReason.Width = 140;
            // 
            // lblPendingFooter
            // 
            this.lblPendingFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblPendingFooter.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblPendingFooter.ForeColor = System.Drawing.Color.Gray;
            this.lblPendingFooter.Name = "lblPendingFooter";
            this.lblPendingFooter.Size = new System.Drawing.Size(200, 20);
            this.lblPendingFooter.TabIndex = 39;
            this.lblPendingFooter.Text = "Double-click to open Admissions.";
            // 
            // rowAppointments
            // 
            this.rowAppointments.ColumnCount = 2;
            this.rowAppointments.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.rowAppointments.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rowAppointments.Controls.Add(this.appointmentsCard, 0, 0);
            this.rowAppointments.Controls.Add(this.occupancyCard, 1, 0);
            this.rowAppointments.Dock = System.Windows.Forms.DockStyle.Top;
            this.rowAppointments.Name = "rowAppointments";
            this.rowAppointments.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.rowAppointments.RowCount = 1;
            this.rowAppointments.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rowAppointments.Size = new System.Drawing.Size(1024, 240);
            this.rowAppointments.TabIndex = 40;
            // 
            // appointmentsCard
            // 
            this.appointmentsCard.BackColor = System.Drawing.Color.White;
            this.appointmentsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.appointmentsCard.Controls.Add(this.lvAppointments);
            this.appointmentsCard.Controls.Add(this.lblAppointmentsTitle);
            this.appointmentsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.appointmentsCard.Margin = new System.Windows.Forms.Padding(5);
            this.appointmentsCard.Name = "appointmentsCard";
            this.appointmentsCard.Padding = new System.Windows.Forms.Padding(10);
            this.appointmentsCard.TabIndex = 41;
            // 
            // lblAppointmentsTitle
            // 
            this.lblAppointmentsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAppointmentsTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAppointmentsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblAppointmentsTitle.Name = "lblAppointmentsTitle";
            this.lblAppointmentsTitle.Size = new System.Drawing.Size(200, 26);
            this.lblAppointmentsTitle.TabIndex = 42;
            this.lblAppointmentsTitle.Text = "Appointments — Today & Next 7 Days";
            this.lblAppointmentsTitle.UseMnemonic = false;
            // 
            // lvAppointments
            // 
            this.lvAppointments.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvAppointments.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colApptDate,
            this.colApptTime,
            this.colApptPatient,
            this.colApptDoctor,
            this.colApptStatus});
            this.lvAppointments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvAppointments.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lvAppointments.FullRowSelect = true;
            this.lvAppointments.GridLines = true;
            this.lvAppointments.HideSelection = false;
            this.lvAppointments.Name = "lvAppointments";
            this.lvAppointments.TabIndex = 43;
            this.lvAppointments.UseCompatibleStateImageBehavior = false;
            this.lvAppointments.View = System.Windows.Forms.View.Details;
            this.lvAppointments.DoubleClick += new System.EventHandler(this.LvAppointments_DoubleClick);
            // 
            // colApptDate
            // 
            this.colApptDate.Text = "Date";
            this.colApptDate.Width = 90;
            // 
            // colApptTime
            // 
            this.colApptTime.Text = "Time";
            this.colApptTime.Width = 70;
            // 
            // colApptPatient
            // 
            this.colApptPatient.Text = "Patient";
            this.colApptPatient.Width = 130;
            // 
            // colApptDoctor
            // 
            this.colApptDoctor.Text = "Doctor";
            this.colApptDoctor.Width = 120;
            // 
            // colApptStatus
            // 
            this.colApptStatus.Text = "Status";
            this.colApptStatus.Width = 80;
            // 
            // occupancyCard
            // 
            this.occupancyCard.BackColor = System.Drawing.Color.White;
            this.occupancyCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.occupancyCard.Controls.Add(this.occupancyBars);
            this.occupancyCard.Controls.Add(this.lblOccupancyTitle);
            this.occupancyCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.occupancyCard.Margin = new System.Windows.Forms.Padding(5);
            this.occupancyCard.Name = "occupancyCard";
            this.occupancyCard.Padding = new System.Windows.Forms.Padding(10);
            this.occupancyCard.TabIndex = 44;
            // 
            // lblOccupancyTitle
            // 
            this.lblOccupancyTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOccupancyTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblOccupancyTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblOccupancyTitle.Name = "lblOccupancyTitle";
            this.lblOccupancyTitle.Size = new System.Drawing.Size(200, 26);
            this.lblOccupancyTitle.TabIndex = 45;
            this.lblOccupancyTitle.Text = "Bed Occupancy by Ward";
            this.lblOccupancyTitle.UseMnemonic = false;
            // 
            // occupancyBars
            // One bar per ward, drawn at runtime.
            // 
            this.occupancyBars.BackColor = System.Drawing.Color.White;
            this.occupancyBars.Dock = System.Windows.Forms.DockStyle.Fill;
            this.occupancyBars.Name = "occupancyBars";
            this.occupancyBars.TabIndex = 46;
            this.occupancyBars.Paint += new System.Windows.Forms.PaintEventHandler(this.OccupancyBars_Paint);
            this.occupancyBars.DoubleClick += new System.EventHandler(this.OccupancyBars_DoubleClick);
            this.occupancyBars.Resize += new System.EventHandler(this.PaintedPanel_Resize);
            // 
            // rowBottom
            // 
            this.rowBottom.ColumnCount = 3;
            this.rowBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.rowBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.rowBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.rowBottom.Controls.Add(this.doctorsCard, 0, 0);
            this.rowBottom.Controls.Add(this.chartAppointmentsCard, 1, 0);
            this.rowBottom.Controls.Add(this.chartAdmissionsCard, 2, 0);
            this.rowBottom.Dock = System.Windows.Forms.DockStyle.Top;
            this.rowBottom.Name = "rowBottom";
            this.rowBottom.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.rowBottom.RowCount = 1;
            this.rowBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rowBottom.Size = new System.Drawing.Size(1024, 200);
            this.rowBottom.TabIndex = 47;
            // 
            // doctorsCard
            // 
            this.doctorsCard.BackColor = System.Drawing.Color.White;
            this.doctorsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.doctorsCard.Controls.Add(this.lvDoctors);
            this.doctorsCard.Controls.Add(this.lblDoctorsTitle);
            this.doctorsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.doctorsCard.Margin = new System.Windows.Forms.Padding(5);
            this.doctorsCard.Name = "doctorsCard";
            this.doctorsCard.Padding = new System.Windows.Forms.Padding(10);
            this.doctorsCard.TabIndex = 48;
            // 
            // lblDoctorsTitle
            // 
            this.lblDoctorsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDoctorsTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblDoctorsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblDoctorsTitle.Name = "lblDoctorsTitle";
            this.lblDoctorsTitle.Size = new System.Drawing.Size(200, 26);
            this.lblDoctorsTitle.TabIndex = 49;
            this.lblDoctorsTitle.Text = "Doctors on Duty";
            this.lblDoctorsTitle.UseMnemonic = false;
            // 
            // lvDoctors
            // 
            this.lvDoctors.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvDoctors.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colDocName,
            this.colDocSpecialization,
            this.colDocDepartment});
            this.lvDoctors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDoctors.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lvDoctors.FullRowSelect = true;
            this.lvDoctors.GridLines = true;
            this.lvDoctors.HideSelection = false;
            this.lvDoctors.Name = "lvDoctors";
            this.lvDoctors.TabIndex = 50;
            this.lvDoctors.UseCompatibleStateImageBehavior = false;
            this.lvDoctors.View = System.Windows.Forms.View.Details;
            // 
            // colDocName
            // 
            this.colDocName.Text = "Doctor";
            this.colDocName.Width = 130;
            // 
            // colDocSpecialization
            // 
            this.colDocSpecialization.Text = "Specialization";
            this.colDocSpecialization.Width = 110;
            // 
            // colDocDepartment
            // 
            this.colDocDepartment.Text = "Department";
            this.colDocDepartment.Width = 100;
            // 
            // chartAppointmentsCard
            // 
            this.chartAppointmentsCard.BackColor = System.Drawing.Color.White;
            this.chartAppointmentsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.chartAppointmentsCard.Controls.Add(this.chartAppointments);
            this.chartAppointmentsCard.Controls.Add(this.lblChartAppointments);
            this.chartAppointmentsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartAppointmentsCard.Margin = new System.Windows.Forms.Padding(5);
            this.chartAppointmentsCard.Name = "chartAppointmentsCard";
            this.chartAppointmentsCard.Padding = new System.Windows.Forms.Padding(10);
            this.chartAppointmentsCard.TabIndex = 51;
            // 
            // lblChartAppointments
            // 
            this.lblChartAppointments.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChartAppointments.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblChartAppointments.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblChartAppointments.Name = "lblChartAppointments";
            this.lblChartAppointments.Size = new System.Drawing.Size(200, 22);
            this.lblChartAppointments.TabIndex = 52;
            this.lblChartAppointments.Text = "Appointments per Day (Last 7 Days)";
            this.lblChartAppointments.UseMnemonic = false;
            // 
            // chartAppointments
            // Bar chart, drawn at runtime.
            // 
            this.chartAppointments.BackColor = System.Drawing.Color.White;
            this.chartAppointments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartAppointments.Name = "chartAppointments";
            this.chartAppointments.TabIndex = 53;
            this.chartAppointments.Paint += new System.Windows.Forms.PaintEventHandler(this.ChartAppointments_Paint);
            this.chartAppointments.Resize += new System.EventHandler(this.PaintedPanel_Resize);
            // 
            // chartAdmissionsCard
            // 
            this.chartAdmissionsCard.BackColor = System.Drawing.Color.White;
            this.chartAdmissionsCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.chartAdmissionsCard.Controls.Add(this.chartAdmissions);
            this.chartAdmissionsCard.Controls.Add(this.lblChartAdmissions);
            this.chartAdmissionsCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartAdmissionsCard.Margin = new System.Windows.Forms.Padding(5);
            this.chartAdmissionsCard.Name = "chartAdmissionsCard";
            this.chartAdmissionsCard.Padding = new System.Windows.Forms.Padding(10);
            this.chartAdmissionsCard.TabIndex = 54;
            // 
            // lblChartAdmissions
            // 
            this.lblChartAdmissions.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChartAdmissions.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblChartAdmissions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblChartAdmissions.Name = "lblChartAdmissions";
            this.lblChartAdmissions.Size = new System.Drawing.Size(200, 22);
            this.lblChartAdmissions.TabIndex = 55;
            this.lblChartAdmissions.Text = "Admissions per Month";
            this.lblChartAdmissions.UseMnemonic = false;
            // 
            // chartAdmissions
            // Bar chart, drawn at runtime.
            // 
            this.chartAdmissions.BackColor = System.Drawing.Color.White;
            this.chartAdmissions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartAdmissions.Name = "chartAdmissions";
            this.chartAdmissions.TabIndex = 56;
            this.chartAdmissions.Paint += new System.Windows.Forms.PaintEventHandler(this.ChartAdmissions_Paint);
            this.chartAdmissions.Resize += new System.EventHandler(this.PaintedPanel_Resize);
            // 
            // DashboardView
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.scrollPanel);
            this.Name = "DashboardView";
            this.Size = new System.Drawing.Size(1024, 760);
            this.scrollPanel.ResumeLayout(false);
            this.bannerPanel.ResumeLayout(false);
            this.bannerInner.ResumeLayout(false);
            this.cardsTable.ResumeLayout(false);
            this.cardAppointments.ResumeLayout(false);
            this.cardOccupancy.ResumeLayout(false);
            this.cardAdmitted.ResumeLayout(false);
            this.cardPending.ResumeLayout(false);
            this.cardAlerts.ResumeLayout(false);
            this.cardOutstanding.ResumeLayout(false);
            this.rowAlerts.ResumeLayout(false);
            this.alertsCard.ResumeLayout(false);
            this.flAlerts.ResumeLayout(false);
            this.pendingCard.ResumeLayout(false);
            this.rowAppointments.ResumeLayout(false);
            this.appointmentsCard.ResumeLayout(false);
            this.occupancyCard.ResumeLayout(false);
            this.rowBottom.ResumeLayout(false);
            this.doctorsCard.ResumeLayout(false);
            this.chartAppointmentsCard.ResumeLayout(false);
            this.chartAdmissionsCard.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        // ===================== Loading =====================
        public void LoadData()
        {
            // Time-based rules (overdue appointments, long waits) are re-checked on every visit.
            AlertMonitor.Evaluate();

            bool seesFinancials = Permissions.Can(Permission.ViewFinancials);
            LoadBanner(seesFinancials);
            LoadCards(seesFinancials);
            LoadAlerts(seesFinancials);
            LoadPendingAdmissions();
            LoadAppointments();
            LoadOccupancy();
            LoadDoctors();
            LoadCharts();
        }

        private void LoadBanner(bool admin)
        {
            var user = HospitalData.CurrentUser;
            int hour = DateTime.Now.Hour;
            string greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";

            bannerAccent = admin ? Amber : Teal;
            lblGreeting.ForeColor = bannerAccent;
            lblGreeting.Text = greeting + ", " + (user != null ? user.DisplayName : "");
            lblRoleView.Text = admin
                ? "Administrator view  ·  full hospital overview, billing totals and the activity log"
                : "Nurse view  ·  clinical overview; billing totals, payments and admin tools are handled by administrators";
            bannerInner.Invalidate();
        }

        private void LoadCards(bool seesFinancials)
        {
            int occupied = HospitalData.OccupiedBedsCount();
            int totalBeds = HospitalData.TotalBedsCount();
            double rate = HospitalData.OccupancyRate();
            var today = HospitalData.AppointmentsToday();
            var pending = HospitalData.PendingAdmissions();
            var alerts = VisibleAlerts(HospitalData.ActiveAlerts(), seesFinancials);
            int highAlerts = alerts.Count(a => a.Severity == "High");

            SetCard(cardAppointments, lblCardAppointmentsValue, lblCardAppointmentsSub, today.Count.ToString(),
                $"{today.Count(a => a.IsOpen)} to see  •  {UpcomingAppointments().Count} this week", Navy);
            SetCard(cardOccupancy, lblCardOccupancyValue, lblCardOccupancySub, $"{occupied}/{totalBeds}",
                $"{rate:0}% occupied  •  {totalBeds - occupied} free",
                rate >= 100 ? Red : rate >= AlertMonitor.HighOccupancyRate * 100 ? Amber : Navy);
            SetCard(cardAdmitted, lblCardAdmittedValue, lblCardAdmittedSub, HospitalData.ActiveAdmissions().Count.ToString(),
                "currently in a bed", Navy);
            SetCard(cardPending, lblCardPendingValue, lblCardPendingSub, pending.Count.ToString(),
                pending.Count == 0 ? "no one waiting" : "longest wait " + HospitalData.FormatDuration(pending[0].WaitingTime),
                pending.Count == 0 ? Navy : Amber);
            SetCard(cardAlerts, lblCardAlertsValue, lblCardAlertsSub, alerts.Count.ToString(),
                alerts.Count == 0 ? "all clear" : $"{highAlerts} high priority",
                highAlerts > 0 ? Red : alerts.Count > 0 ? Amber : Green);

            if (seesFinancials)
            {
                decimal patientsOwe = HospitalData.OutstandingBalance(), hmosOwe = HospitalData.OutstandingHmo();
                SetCard(cardOutstanding, lblCardOutstandingValue, lblCardOutstandingSub, (patientsOwe + hmosOwe).ToString("N0"),
                    $"patients {patientsOwe:N0}  •  HMO {hmosOwe:N0}", patientsOwe + hmosOwe > 0 ? Amber : Green);
            }
            else if (cardsTable.Controls.Contains(cardOutstanding))
            {
                // Money owed to the hospital is for administrators: drop the card and share the row five ways.
                cardsTable.Controls.Remove(cardOutstanding);
                cardsTable.ColumnStyles.RemoveAt(cardsTable.ColumnStyles.Count - 1);
                cardsTable.ColumnCount = 5;
                foreach (ColumnStyle style in cardsTable.ColumnStyles)
                    style.Width = 20F;
            }
        }

        private void SetCard(Panel card, Label value, Label sub, string text, string subtitle, Color accent)
        {
            value.Text = text;
            value.ForeColor = accent;
            sub.Text = subtitle;
            cardAccents[card] = accent;   // colour strip on the left: red/amber when the number needs attention
            card.Invalidate();
        }

        // Billing alerts are about money owed to the hospital, which only administrators handle.
        private static List<Alert> VisibleAlerts(List<Alert> alerts, bool seesFinancials)
        {
            return seesFinancials ? alerts : alerts.Where(a => a.Module != "Billing").ToList();
        }

        private void LoadAlerts(bool seesFinancials)
        {
            var alerts = VisibleAlerts(HospitalData.ActiveAlerts(), seesFinancials);
            int acknowledged = VisibleAlerts(HospitalData.AcknowledgedAlerts(), seesFinancials).Count;

            lblAlertsTitle.Text = "Alerts" + (acknowledged > 0 ? $"    ({acknowledged} acknowledged, still ongoing)" : "");
            lblAlertsHint.Text = seesFinancials
                ? "Raised automatically by the other modules; each clears itself once fixed."
                : "Clinical alerts, raised automatically; each clears itself once fixed.";

            // Replace the alert rows; lblNoAlerts is part of the designed layout and just toggles.
            flAlerts.SuspendLayout();
            foreach (var old in flAlerts.Controls.Cast<Control>().Where(c => c != lblNoAlerts).ToList())
            {
                flAlerts.Controls.Remove(old);
                old.Dispose();
            }
            lblNoAlerts.Visible = alerts.Count == 0;
            foreach (var a in alerts)
                flAlerts.Controls.Add(CreateAlertItem(a));
            flAlerts.ResumeLayout();
            FlAlerts_Resize(flAlerts, EventArgs.Empty);
        }

        private void LoadPendingAdmissions()
        {
            var pending = HospitalData.PendingAdmissions();
            int free = HospitalData.AvailableBeds().Count;

            lblPendingTitle.ForeColor = pending.Count > 0 ? Amber : Navy;
            lvPending.Items.Clear();
            if (pending.Count == 0)
                lvPending.Items.Add(new ListViewItem(new[] { "—", "No patients waiting", "—", "—", "—" }));

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
                lvPending.Items.Add(item);
            }

            bool bedsWaiting = pending.Count > 0 && free > 0;
            lblPendingFooter.ForeColor = bedsWaiting ? Red : Color.Gray;
            lblPendingFooter.Text = bedsWaiting
                ? $"{free} bed(s) free now — double-click to assign them in Admissions."
                : "Double-click to open Admissions.";
        }

        private static List<Appointment> UpcomingAppointments()
        {
            return HospitalData.Appointments
                .Where(a => a.ScheduledOn >= DateTime.Today && a.ScheduledOn < DateTime.Today.AddDays(8) && a.Status != "Cancelled")
                .OrderBy(a => a.ScheduledOn)
                .ToList();
        }

        private void LoadAppointments()
        {
            var upcoming = UpcomingAppointments();
            lvAppointments.Items.Clear();
            if (upcoming.Count == 0)
                lvAppointments.Items.Add(new ListViewItem(new[] { "—", "—", "No upcoming appointments", "—", "—" }));

            foreach (var a in upcoming)
            {
                bool isToday = a.ScheduledOn.Date == DateTime.Today;
                var item = new ListViewItem(isToday ? "Today" : a.ScheduledOn.ToString("ddd MMM dd"));
                item.SubItems.Add(a.ScheduledOn.ToString("hh:mm tt"));
                item.SubItems.Add(HospitalData.PatientName(a.PatientId));
                item.SubItems.Add(HospitalData.DoctorName(a.DoctorId));
                item.SubItems.Add(a.Status);
                if (isToday) item.Font = boldRow;
                if (a.IsOpen && a.ScheduledOn < DateTime.Now - AlertMonitor.AppointmentGrace)
                    item.ForeColor = Red;   // overdue, not closed yet
                lvAppointments.Items.Add(item);
            }
        }

        private void LoadOccupancy()
        {
            wards = HospitalData.Beds
                .GroupBy(b => b.Ward ?? "(no ward)")
                .OrderBy(g => g.Key)
                .Select(g => new WardOccupancy { Ward = g.Key, Total = g.Count(), Occupied = g.Count(b => b.IsOccupied) })
                .ToList();
            wards.Add(new WardOccupancy { Ward = "All wards", Total = HospitalData.TotalBedsCount(), Occupied = HospitalData.OccupiedBedsCount() });
            occupancyBars.Invalidate();
        }

        private void LoadDoctors()
        {
            var onDuty = HospitalData.DoctorsOnDuty();
            lvDoctors.Items.Clear();
            if (onDuty.Count == 0)
                lvDoctors.Items.Add(new ListViewItem(new[] { "—", "No doctors on duty", "—" }));

            foreach (var d in onDuty)
            {
                var item = new ListViewItem("Dr. " + d.FullName);
                item.SubItems.Add(d.Specialization);
                item.SubItems.Add(HospitalData.DepartmentName(d.DepartmentId));
                lvDoctors.Items.Add(item);
            }
        }

        private void LoadCharts()
        {
            appointmentsPerDay = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(i - 6))
                .Select(day => new KeyValuePair<string, int>(day.ToString("ddd"),
                    HospitalData.Appointments.Count(a => a.ScheduledOn.Date == day)))
                .ToList();

            admissionsPerMonth = Enumerable.Range(0, 6)
                .Select(i => DateTime.Today.AddMonths(i - 5))
                .Select(month => new KeyValuePair<string, int>(month.ToString("MMM"),
                    HospitalData.Admissions.Count(a => !a.IsPending && a.Status != "Cancelled" &&
                        a.AdmittedOn.Year == month.Year && a.AdmittedOn.Month == month.Month)))
                .ToList();

            chartAppointments.Invalidate();
            chartAdmissions.Invalidate();
        }

        // ===================== Painting =====================
        private void BannerInner_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(bannerAccent, 4))
                e.Graphics.DrawLine(pen, 1, 0, 1, bannerInner.Height);
        }

        private void Card_Paint(object sender, PaintEventArgs e)
        {
            var card = (Control)sender;
            Color accent;
            if (!cardAccents.TryGetValue(card, out accent)) accent = Navy;
            using (var pen = new Pen(accent, 4))
                e.Graphics.DrawLine(pen, 1, 0, 1, card.Height);
        }

        private void PaintedPanel_Resize(object sender, EventArgs e) => ((Control)sender).Invalidate();

        private void OccupancyBars_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var labelFont = new Font("Segoe UI", 9F))
            using (var boldFont = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var track = new SolidBrush(Color.FromArgb(229, 231, 235)))
            {
                int rowH = Math.Max(24, Math.Min(34, occupancyBars.Height / Math.Max(1, wards.Count)));
                int labelW = 90, valueW = 90;
                int barW = Math.Max(40, occupancyBars.Width - labelW - valueW - 10);

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
        }

        private void ChartAppointments_Paint(object sender, PaintEventArgs e) => DrawBarChart(e.Graphics, chartAppointments, appointmentsPerDay);

        private void ChartAdmissions_Paint(object sender, PaintEventArgs e) => DrawBarChart(e.Graphics, chartAdmissions, admissionsPerMonth);

        private static void DrawBarChart(Graphics g, Control area, List<KeyValuePair<string, int>> data)
        {
            if (data.Count == 0) return;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int max = Math.Max(1, data.Max(d => d.Value));
            int gap = 7;
            int barWidth = Math.Max(10, (area.Width - gap * (data.Count + 1)) / data.Count);
            int maxBarHeight = area.Height - 28;

            using (var valueFont = new Font("Segoe UI", 7.5F))
            using (var labelFont = new Font("Segoe UI", 7F))
            using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                for (int i = 0; i < data.Count; i++)
                {
                    int h = (int)(data[i].Value / (float)max * maxBarHeight);
                    int x = gap + i * (barWidth + gap);
                    int y = area.Height - h - 16;

                    g.FillRectangle(brush, x, y, barWidth, h);

                    string val = data[i].Value.ToString();
                    var sz = g.MeasureString(val, valueFont);
                    g.DrawString(val, valueFont, Brushes.DimGray, x + (barWidth - sz.Width) / 2, y - 14);

                    var sz2 = g.MeasureString(data[i].Key, labelFont);
                    g.DrawString(data[i].Key, labelFont, Brushes.Gray, x + (barWidth - sz2.Width) / 2, area.Height - 14);
                }
            }
        }

        // ===================== Navigation =====================
        private void OpenModule(string module)
        {
            if (module != null && ModuleRequested != null)
                ModuleRequested(module);
        }

        private void LvPending_DoubleClick(object sender, EventArgs e) => OpenModule("Admissions");
        private void LvAppointments_DoubleClick(object sender, EventArgs e) => OpenModule("Appointments");
        private void OccupancyBars_DoubleClick(object sender, EventArgs e) => OpenModule("Admissions");

        // ===================== Alert rows (one per active alert, built at runtime) =====================
        // Stretch every alert row to the list width.
        private void FlAlerts_Resize(object sender, EventArgs e)
        {
            foreach (Control c in flAlerts.Controls)
                c.Width = Math.Max(200, flAlerts.ClientSize.Width - 6);
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
                LoadData();
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
                if (result == DialogResult.OK) LoadData();
                else if (result == DialogResult.Yes) OpenModule(a.Module);
            }
        }
    }
}

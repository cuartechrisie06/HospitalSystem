using System;
using System.Drawing;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;
using HospitalSystem.Services;

namespace HospitalSystem.Views
{
    // Printable reports: daily census for everyone, revenue for roles that can view financials.
    public class ReportsView : UserControl
    {
        private const string CensusReport = "Daily census";
        private const string RevenueReport = "Revenue by day";

        private Panel header;
        private Label lblTitle;
        private Label lblReport;
        private ComboBox cmbReport;
        private Label lblFrom;
        private DateTimePicker dtFrom;
        private Label lblTo;
        private DateTimePicker dtTo;
        private Button btnGenerate;
        private Button btnPrint;
        private Label lblSubtitle;
        private DataGridView grid;
        private Label lblNotes;

        private ReportTable current;

        private readonly IPermissionGuard permissionGuard;
        private readonly IActivityLogger activityLog;
        private readonly IReportService reportService;

        // Used by the Designer to render the layout; the app uses the constructor below.
        public ReportsView()
        {
            InitializeComponent();
        }

        public ReportsView(IPermissionGuard permissionGuard, IActivityLogger activityLog, IReportService reportService) : this()
        {
            this.permissionGuard = permissionGuard;
            this.activityLog = activityLog;
            this.reportService = reportService;

            cmbReport.Items.Add(CensusReport);
            if (permissionGuard.Can(Permission.ViewFinancials))
                cmbReport.Items.Add(RevenueReport);
            cmbReport.SelectedIndex = 0;

            // Default period: the last 7 days (dates can't be expressed in designer code).
            dtFrom.Value = DateTime.Today.AddDays(-6);
            dtTo.Value = DateTime.Today;
            Generate();
        }

        private void BtnGenerate_Click(object sender, EventArgs e) => Generate();

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (current == null) return;
            using (var printer = new ReportPrinter(activityLog, current))
                printer.ShowPreview(FindForm());
        }

        private void Generate()
        {
            current = (string)cmbReport.SelectedItem == RevenueReport
                ? reportService.Revenue(dtFrom.Value.Date, dtTo.Value.Date)
                : reportService.DailyCensus(dtFrom.Value.Date, dtTo.Value.Date);
            ShowReport(current);
        }

        private void ShowReport(ReportTable report)
        {
            lblSubtitle.Text = report.Title + "  •  " + report.Subtitle;

            grid.Columns.Clear();
            grid.Rows.Clear();
            for (int i = 0; i < report.Columns.Length; i++)
            {
                int col = grid.Columns.Add("c" + i, report.Columns[i]);
                grid.Columns[col].SortMode = DataGridViewColumnSortMode.NotSortable;
                grid.Columns[col].FillWeight = i == 0 ? 200 : 100;
                if (report.RightAligned[i])
                    grid.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            foreach (var row in report.Rows)
                grid.Rows.Add(row);

            if (report.TotalRow != null)
            {
                int total = grid.Rows.Add(report.TotalRow);
                grid.Rows[total].DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
                grid.Rows[total].DefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
            }

            lblNotes.Text = string.Join(Environment.NewLine, report.Notes);
        }

        // Designer-generated layout: header (title, report picker, period, buttons) above the result grid and notes.
        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblReport = new System.Windows.Forms.Label();
            this.cmbReport = new System.Windows.Forms.ComboBox();
            this.lblFrom = new System.Windows.Forms.Label();
            this.dtFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtTo = new System.Windows.Forms.DateTimePicker();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.lblNotes = new System.Windows.Forms.Label();
            this.header.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            //
            // header
            //
            this.header.BackColor = System.Drawing.Color.White;
            this.header.Controls.Add(this.lblTitle);
            this.header.Controls.Add(this.lblReport);
            this.header.Controls.Add(this.cmbReport);
            this.header.Controls.Add(this.lblFrom);
            this.header.Controls.Add(this.dtFrom);
            this.header.Controls.Add(this.lblTo);
            this.header.Controls.Add(this.dtTo);
            this.header.Controls.Add(this.btnGenerate);
            this.header.Controls.Add(this.btnPrint);
            this.header.Controls.Add(this.lblSubtitle);
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(0, 0);
            this.header.Name = "header";
            this.header.Padding = new System.Windows.Forms.Padding(20);
            this.header.Size = new System.Drawing.Size(1000, 130);
            this.header.TabIndex = 0;
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this.lblTitle.Location = new System.Drawing.Point(20, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Reports";
            //
            // lblReport
            //
            this.lblReport.AutoSize = true;
            this.lblReport.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReport.Location = new System.Drawing.Point(22, 62);
            this.lblReport.Name = "lblReport";
            this.lblReport.TabIndex = 1;
            this.lblReport.Text = "Report";
            //
            // cmbReport
            //
            this.cmbReport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReport.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbReport.Location = new System.Drawing.Point(22, 82);
            this.cmbReport.Name = "cmbReport";
            this.cmbReport.Size = new System.Drawing.Size(180, 25);
            this.cmbReport.TabIndex = 2;
            //
            // lblFrom
            //
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFrom.Location = new System.Drawing.Point(222, 62);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.TabIndex = 3;
            this.lblFrom.Text = "From";
            //
            // dtFrom
            //
            this.dtFrom.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFrom.Location = new System.Drawing.Point(222, 82);
            this.dtFrom.Name = "dtFrom";
            this.dtFrom.Size = new System.Drawing.Size(130, 25);
            this.dtFrom.TabIndex = 4;
            //
            // lblTo
            //
            this.lblTo.AutoSize = true;
            this.lblTo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTo.Location = new System.Drawing.Point(366, 62);
            this.lblTo.Name = "lblTo";
            this.lblTo.TabIndex = 5;
            this.lblTo.Text = "To";
            //
            // dtTo
            //
            this.dtTo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtTo.Location = new System.Drawing.Point(366, 82);
            this.dtTo.Name = "dtTo";
            this.dtTo.Size = new System.Drawing.Size(130, 25);
            this.dtTo.TabIndex = 6;
            //
            // btnGenerate
            //
            this.btnGenerate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnGenerate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGenerate.FlatAppearance.BorderSize = 0;
            this.btnGenerate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerate.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnGenerate.ForeColor = System.Drawing.Color.White;
            this.btnGenerate.Location = new System.Drawing.Point(514, 79);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(110, 32);
            this.btnGenerate.TabIndex = 7;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = false;
            this.btnGenerate.Click += new System.EventHandler(this.BtnGenerate_Click);
            //
            // btnPrint
            //
            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(185)))), ((int)(((byte)(129)))));
            this.btnPrint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrint.FlatAppearance.BorderSize = 0;
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.Location = new System.Drawing.Point(634, 79);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(110, 32);
            this.btnPrint.TabIndex = 8;
            this.btnPrint.Text = "Print...";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblSubtitle.Location = new System.Drawing.Point(124, 22);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.TabIndex = 9;
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.grid.Location = new System.Drawing.Point(0, 130);
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1000, 470);
            this.grid.TabIndex = 1;
            //
            // lblNotes
            //
            this.lblNotes.BackColor = System.Drawing.Color.White;
            this.lblNotes.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblNotes.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblNotes.Location = new System.Drawing.Point(0, 600);
            this.lblNotes.Name = "lblNotes";
            this.lblNotes.Padding = new System.Windows.Forms.Padding(20, 8, 20, 8);
            this.lblNotes.Size = new System.Drawing.Size(1000, 100);
            this.lblNotes.TabIndex = 2;
            //
            // ReportsView
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.grid);
            this.Controls.Add(this.lblNotes);
            this.Controls.Add(this.header);
            this.Name = "ReportsView";
            this.Size = new System.Drawing.Size(1000, 700);
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}

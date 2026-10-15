using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Models;
using HospitalSystem.Services;

namespace HospitalSystem.Views
{
    // Prints any ReportTable: title, period, the table (header repeated on every page), totals and notes.
    public class ReportPrinter : IDisposable
    {
        private readonly ReportTable report;
        private readonly IActivityLogger activityLog;
        private readonly PrintDocument document = new PrintDocument();
        private readonly Font titleFont = new Font("Segoe UI", 14F, FontStyle.Bold);
        private readonly Font bodyFont = new Font("Segoe UI", 9F);
        private readonly Font boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private int nextRow;
        private int nextNote;
        private bool totalDone;
        private int pageNo;

        public ReportPrinter(IActivityLogger activityLog, ReportTable report)
        {
            this.activityLog = activityLog;
            this.report = report;
            document.DocumentName = report.Title;
            document.DefaultPageSettings.Landscape = report.Columns.Length > 6;
            document.BeginPrint += (s, e) => { nextRow = 0; nextNote = 0; totalDone = false; pageNo = 0; };
            document.PrintPage += Document_PrintPage;
            // Log real printouts only; the on-screen preview also "prints" (to a preview controller).
            document.EndPrint += (s, e) =>
            {
                if (e.PrintAction == PrintAction.PrintToPrinter && !e.Cancel)
                    activityLog.Log("Reports", "Printed", $"Printed {report.Title} for {report.Subtitle} ({pageNo} page(s))", "🖨");
            };
        }

        public void ShowPreview(IWin32Window owner)
        {
            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = document;
                preview.Width = 1000;
                preview.Height = 800;
                preview.ShowDialog(owner);
            }
        }

        private void Document_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            Rectangle m = e.MarginBounds;
            float y = m.Top;
            pageNo++;

            // First column (dates) gets twice the width of the numeric ones.
            int cols = report.Columns.Length;
            float unit = m.Width / (float)(cols + 1);
            Func<int, float> left = i => m.Left + (i == 0 ? 0 : unit * (i + 1));
            Func<int, float> width = i => i == 0 ? unit * 2 : unit;
            float rowHeight = bodyFont.GetHeight(g) + 6;

            if (pageNo == 1)
            {
                g.DrawString(report.Title, titleFont, Brushes.Black, m.Left, y);
                y += titleFont.GetHeight(g) + 2;
                g.DrawString(report.Subtitle + "    •    Printed " + DateTime.Now.ToString("MMM dd, yyyy h:mm tt"), bodyFont, Brushes.DimGray, m.Left, y);
                y += rowHeight * 1.5f;
            }

            Action<string[], Font, bool> drawRow = (cells, font, shade) =>
            {
                if (shade) g.FillRectangle(Brushes.Gainsboro, m.Left, y, m.Width, rowHeight);
                for (int i = 0; i < cols; i++)
                {
                    bool right = report.RightAligned != null && report.RightAligned[i];
                    var format = new StringFormat { Alignment = right ? StringAlignment.Far : StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                    g.DrawString(cells[i] ?? "", font, Brushes.Black, new RectangleF(left(i) + 2, y + 3, width(i) - 6, rowHeight), format);
                }
                y += rowHeight;
            };

            // Column header on every page.
            g.DrawLine(Pens.Gray, m.Left, y, m.Right, y);
            drawRow(report.Columns, boldFont, false);
            g.DrawLine(Pens.Gray, m.Left, y, m.Right, y);

            float bottom = m.Bottom - rowHeight;
            while (nextRow < report.Rows.Count && y + rowHeight <= bottom)
                drawRow(report.Rows[nextRow++], bodyFont, false);

            if (nextRow >= report.Rows.Count)
            {
                if (!totalDone && report.TotalRow != null)
                {
                    if (y + rowHeight > bottom) { FinishPage(e, g, m); return; }
                    drawRow(report.TotalRow, boldFont, true);
                    y += rowHeight / 2;
                }
                totalDone = true;

                while (nextNote < report.Notes.Count && y + rowHeight <= bottom)
                {
                    g.DrawString(report.Notes[nextNote++], bodyFont, Brushes.Black, new RectangleF(m.Left, y, m.Width, rowHeight * 2));
                    y += rowHeight;
                }
            }

            FinishPage(e, g, m);
        }

        private void FinishPage(PrintPageEventArgs e, Graphics g, Rectangle m)
        {
            g.DrawString("Page " + pageNo, bodyFont, Brushes.DimGray, m.Right - 60, m.Bottom + 10);
            e.HasMorePages = nextRow < report.Rows.Count || !totalDone || nextNote < report.Notes.Count;
        }

        public void Dispose()
        {
            document.Dispose();
            titleFont.Dispose();
            bodyFont.Dispose();
            boldFont.Dispose();
        }
    }
}

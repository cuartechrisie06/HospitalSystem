using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // Statement of account for one bill: itemized charges by category, then discounts,
    // VAT, HMO coverage, payments and the balance. Paginates across as many pages as needed.
    public class BillStatementPrinter : IDisposable
    {
        private enum RowStyle { Title, Info, Heading, ColumnHeader, Item, Line, Subtotal, Total, Spacer }

        private class Row
        {
            public RowStyle Style;
            public string Text;
            public string Qty, UnitPrice, Amount;
        }

        private readonly Bill bill;
        private readonly List<Row> rows;
        private readonly PrintDocument document = new PrintDocument();
        private readonly Font titleFont = new Font("Segoe UI", 14F, FontStyle.Bold);
        private readonly Font headingFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        private readonly Font bodyFont = new Font("Segoe UI", 9F);
        private readonly Font boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private int nextRow;
        private int pageNo;

        public BillStatementPrinter(Bill bill)
        {
            this.bill = bill;
            rows = BuildRows();
            document.DocumentName = "Statement " + bill.BillNo;
            document.BeginPrint += (s, e) => { nextRow = 0; pageNo = 0; };
            document.PrintPage += Document_PrintPage;
        }

        public void ShowPreview(IWin32Window owner)
        {
            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = document;
                preview.Width = 900;
                preview.Height = 1000;
                preview.ShowDialog(owner);
            }
        }

        private static string Money(decimal value) => value < 0 ? "(" + (-value).ToString("N2") + ")" : value.ToString("N2");

        private List<Row> BuildRows()
        {
            var list = new List<Row>();
            var patient = HospitalData.GetPatient(bill.PatientId);

            list.Add(new Row { Style = RowStyle.Title, Text = "STATEMENT OF ACCOUNT" });
            list.Add(new Row { Style = RowStyle.Info, Text = "Bill No.: " + bill.BillNo + "        Date: " + bill.BillDate.ToString("yyyy-MM-dd") + "        Status: " + bill.Status });
            list.Add(new Row { Style = RowStyle.Info, Text = "Patient: " + HospitalData.PatientName(bill.PatientId) +
                (patient != null ? "  (" + patient.PatientNo + ")" : "") });
            if (bill.AdmissionId.HasValue)
            {
                var adm = HospitalData.Admissions.FirstOrDefault(a => a.Id == bill.AdmissionId.Value);
                if (adm != null)
                    list.Add(new Row { Style = RowStyle.Info, Text = "Admission: " + adm.AdmissionNo + "  " + HospitalData.BedLabel(adm.BedId) +
                        "  Admitted " + adm.AdmittedOn.ToString("yyyy-MM-dd") +
                        (adm.DischargedOn.HasValue ? ", discharged " + adm.DischargedOn.Value.ToString("yyyy-MM-dd") : "") +
                        "  (" + adm.DaysStayed + " day(s))" });
            }
            else if (bill.AppointmentId.HasValue)
            {
                list.Add(new Row { Style = RowStyle.Info, Text = "Appointment: A-" + bill.AppointmentId.Value.ToString("D4") });
            }
            list.Add(new Row { Style = RowStyle.Spacer });

            list.Add(new Row { Style = RowStyle.Heading, Text = "Itemized charges" });
            list.Add(new Row { Style = RowStyle.ColumnHeader, Text = "Description", Qty = "Qty", UnitPrice = "Unit price", Amount = "Amount" });
            foreach (var group in bill.Items.GroupBy(i => i.Category).OrderBy(g => g.Key))
            {
                list.Add(new Row { Style = RowStyle.Subtotal, Text = Bill.CategoryLabel(group.Key) });
                foreach (var item in group.OrderBy(i => i.Id))
                    list.Add(new Row
                    {
                        Style = RowStyle.Item,
                        Text = "    " + item.Description,
                        Qty = item.Quantity.ToString(),
                        UnitPrice = item.UnitPrice.ToString("N2"),
                        Amount = item.Amount.ToString("N2")
                    });
            }
            list.Add(new Row { Style = RowStyle.Spacer });

            list.Add(new Row { Style = RowStyle.Heading, Text = "Summary" });
            foreach (var line in bill.GetBreakdown())
                list.Add(new Row
                {
                    Style = line.Kind == BreakdownLineKind.Total ? RowStyle.Total
                        : line.Kind == BreakdownLineKind.Subtotal ? RowStyle.Subtotal
                        : RowStyle.Line,
                    Text = line.Label,
                    Amount = Money(line.Amount)
                });

            if (bill.Payments.Count > 0)
            {
                list.Add(new Row { Style = RowStyle.Spacer });
                list.Add(new Row { Style = RowStyle.Heading, Text = "Payments" });
                foreach (var p in bill.Payments.OrderBy(p => p.PaymentDate))
                    list.Add(new Row
                    {
                        Style = RowStyle.Line,
                        Text = p.PaymentDate.ToString("yyyy-MM-dd HH:mm") + "  " + p.MethodLabel +
                            (string.IsNullOrEmpty(p.Details) ? "" : "  (" + p.Details + ")") +
                            (string.IsNullOrEmpty(p.ReferenceNo) ? "" : "  Ref " + p.ReferenceNo),
                        Amount = p.Amount.ToString("N2")
                    });
            }

            list.Add(new Row { Style = RowStyle.Spacer });
            list.Add(new Row { Style = RowStyle.Info, Text = "Printed " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") +
                (HospitalData.CurrentUser != null ? " by " + HospitalData.CurrentUser.DisplayName : "") });
            return list;
        }

        private Font FontFor(RowStyle style)
        {
            switch (style)
            {
                case RowStyle.Title: return titleFont;
                case RowStyle.Heading: return headingFont;
                case RowStyle.ColumnHeader:
                case RowStyle.Subtotal:
                case RowStyle.Total: return boldFont;
                default: return bodyFont;
            }
        }

        private void Document_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            Rectangle m = e.MarginBounds;
            float y = m.Top;
            pageNo++;

            // Right-aligned columns: amount at the margin, unit price and qty to its left.
            float amountRight = m.Right;
            float unitRight = m.Right - 110;
            float qtyRight = m.Right - 210;
            float labelWidth = m.Width - 120;   // leave room for the amount column
            var right = new StringFormat { Alignment = StringAlignment.Far };

            while (nextRow < rows.Count)
            {
                var row = rows[nextRow];
                Font font = FontFor(row.Style);
                float textWidth = row.Style == RowStyle.Item || row.Style == RowStyle.ColumnHeader ? qtyRight - 50 - m.Left : labelWidth;
                float height = row.Style == RowStyle.Spacer
                    ? font.GetHeight(g) * 0.8f
                    : Math.Max(font.GetHeight(g), g.MeasureString(row.Text ?? "", font, (int)textWidth).Height) + 4;

                // Keep at least one row per page so an over-tall row can't loop forever.
                if (y + height > m.Bottom - 20 && y > m.Top)
                    break;

                if (row.Style == RowStyle.Total)
                    g.FillRectangle(Brushes.Gainsboro, m.Left, y, m.Width, height);
                if (row.Style == RowStyle.ColumnHeader || row.Style == RowStyle.Total)
                    g.DrawLine(Pens.Gray, m.Left, y, m.Right, y);

                if (row.Text != null)
                    g.DrawString(row.Text, font, Brushes.Black, new RectangleF(m.Left, y + 2, textWidth, height));
                if (row.Qty != null)
                    g.DrawString(row.Qty, font, Brushes.Black, qtyRight, y + 2, right);
                if (row.UnitPrice != null)
                    g.DrawString(row.UnitPrice, font, Brushes.Black, unitRight, y + 2, right);
                if (row.Amount != null)
                    g.DrawString(row.Amount, font, Brushes.Black, amountRight, y + 2, right);

                y += height;
                nextRow++;
            }

            g.DrawString(bill.BillNo + "  -  page " + pageNo, bodyFont, Brushes.Gray, m.Right, m.Bottom, right);
            e.HasMorePages = nextRow < rows.Count;
        }

        public void Dispose()
        {
            document.Dispose();
            titleFont.Dispose();
            headingFont.Dispose();
            bodyFont.Dispose();
            boldFont.Dispose();
        }
    }
}

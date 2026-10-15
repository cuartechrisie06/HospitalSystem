using System.Collections.Generic;

namespace HospitalSystem.Models
{
    // A finished report: shown in a grid on screen and printed by ReportPrinter.
    public class ReportTable
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string[] Columns { get; set; }

        // Numeric columns are right-aligned on screen and on paper.
        public bool[] RightAligned { get; set; }

        public List<string[]> Rows { get; } = new List<string[]>();

        // Shown last, highlighted; null when the report has no totals.
        public string[] TotalRow { get; set; }

        // Lines printed under the table (current snapshot figures, explanations).
        public List<string> Notes { get; } = new List<string>();
    }
}

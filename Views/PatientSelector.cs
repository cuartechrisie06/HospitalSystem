using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // The common embedded picker used by Billing and Admissions. Its columns and search
    // behavior deliberately match the Appointment screen's patient table.
    public sealed class PatientSelector : UserControl
    {
        private const string Placeholder = "Search active patients by ID, name, or contact...";
        private readonly TextBox search = new TextBox();
        private readonly DataGridView grid = new DataGridView();
        private IEnumerable<Patient> source = Enumerable.Empty<Patient>();
        private bool placeholderActive = true;

        public Patient SelectedPatient { get; private set; }
        public event EventHandler PatientSelected;

        public PatientSelector()
        {
            var heading = new Label { Text = "1. Search & select an active patient", Dock = DockStyle.Top, Height = 22,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81) };
            search.Dock = DockStyle.Top;
            search.Font = new Font("Segoe UI", 10F);
            search.ForeColor = Color.Gray;
            search.Text = Placeholder;
            search.Enter += (s, e) => { if (placeholderActive) { placeholderActive = false; search.Text = ""; search.ForeColor = Color.FromArgb(17, 24, 39); } };
            search.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(search.Text)) { placeholderActive = true; search.Text = Placeholder; search.ForeColor = Color.Gray; } };
            search.TextChanged += (s, e) => { if (!placeholderActive) LoadRows(); };
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellClick += (s, e) => SelectRow(e.RowIndex);
            Controls.Add(grid);
            Controls.Add(search);
            Controls.Add(heading);
        }

        public void SetPatients(IEnumerable<Patient> patients)
        {
            source = patients.ToList();
            SelectedPatient = null;
            LoadRows();
        }

        private void LoadRows()
        {
            string filter = placeholderActive ? "" : search.Text.Trim().ToLowerInvariant();
            grid.DataSource = source.Where(p => string.IsNullOrEmpty(filter) || p.FullName.ToLowerInvariant().Contains(filter) ||
                    p.PatientNo.ToLowerInvariant().Contains(filter) || (!string.IsNullOrEmpty(p.Contact) && p.Contact.Contains(filter)))
                .Select(p => new { p.Id, No = p.PatientNo, Name = p.FullName, Age = p.Age.HasValue ? p.Age.Value.ToString() : "-", p.Gender, p.Contact })
                .ToList();
            if (grid.Columns["Id"] != null) grid.Columns["Id"].Visible = false;
            grid.ClearSelection();
        }

        private void SelectRow(int rowIndex)
        {
            if (rowIndex < 0) return;
            int id = Convert.ToInt32(grid.Rows[rowIndex].Cells["Id"].Value);
            SelectedPatient = source.FirstOrDefault(p => p.Id == id);
            if (SelectedPatient != null) PatientSelected?.Invoke(this, EventArgs.Empty);
        }
    }
}

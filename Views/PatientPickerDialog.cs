using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HospitalSystem.Models;

namespace HospitalSystem.Views
{
    // Shared searchable patient table for workflows that need a patient selection.
    public sealed class PatientPickerDialog : Form
    {
        private readonly List<Patient> patients;
        private readonly TextBox search = new TextBox();
        private readonly DataGridView grid = new DataGridView();

        public Patient SelectedPatient { get; private set; }

        public PatientPickerDialog(IEnumerable<Patient> patients, string title)
        {
            this.patients = patients.ToList();
            Text = title;
            Size = new Size(700, 460);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;

            var heading = new Label { Text = "Search & select an active patient", Dock = DockStyle.Top, Height = 28,
                Padding = new Padding(10, 8, 0, 0), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            search.Dock = DockStyle.Top;
            search.Margin = new Padding(10);
            search.Font = new Font("Segoe UI", 10F);
            search.TextChanged += (s, e) => LoadRows();

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellDoubleClick += (s, e) => SelectCurrent();

            var select = new Button { Text = "Select Patient", Dock = DockStyle.Right, Width = 120, DialogResult = DialogResult.None };
            select.Click += (s, e) => SelectCurrent();
            var cancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = 90, DialogResult = DialogResult.Cancel };
            var buttons = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(10) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(select);

            Controls.Add(grid);
            Controls.Add(search);
            Controls.Add(heading);
            Controls.Add(buttons);
            AcceptButton = select;
            CancelButton = cancel;
            LoadRows();
        }

        private void LoadRows()
        {
            string filter = search.Text.Trim().ToLowerInvariant();
            var rows = patients.Where(p => string.IsNullOrEmpty(filter) ||
                    p.FullName.ToLowerInvariant().Contains(filter) || p.PatientNo.ToLowerInvariant().Contains(filter) ||
                    (!string.IsNullOrEmpty(p.Contact) && p.Contact.Contains(filter)))
                .Select(p => new { p.Id, No = p.PatientNo, Name = p.FullName, Age = p.Age.HasValue ? p.Age.Value.ToString() : "-", p.Gender, p.Contact })
                .ToList();
            grid.DataSource = rows;
            if (grid.Columns["Id"] != null) grid.Columns["Id"].Visible = false;
            grid.ClearSelection();
        }

        private void SelectCurrent()
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Please select a patient from the table.", "Select Patient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int id = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
            SelectedPatient = patients.FirstOrDefault(p => p.Id == id);
            if (SelectedPatient == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

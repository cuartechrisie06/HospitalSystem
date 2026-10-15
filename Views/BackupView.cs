using System;
using System.Drawing;
using System.Windows.Forms;
using HospitalSystem.Services;

namespace HospitalSystem.Views
{
    // Administrators save the whole database to a file and load it back.
    public class BackupView : UserControl
    {
        private Panel header;
        private Label lblTitle;
        private Label lblBackupTitle;
        private Label lblBackupText;
        private Button btnBackup;
        private Label lblRestoreTitle;
        private Label lblRestoreText;
        private Button btnRestore;
        private Label lblStatus;

        private readonly IBackupService backupService;
        private readonly IReloadable data;

        // Used by the Designer to render the layout; the app uses the constructor below.
        public BackupView()
        {
            InitializeComponent();
        }

        // data: reloaded after a restore so every screen shows the restored records.
        public BackupView(IBackupService backupService, IReloadable data) : this()
        {
            this.backupService = backupService;
            this.data = data;
        }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save database backup";
                dlg.Filter = "SQL backup (*.sql)|*.sql";
                dlg.FileName = "hospital-backup-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".sql";
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                Cursor = Cursors.WaitCursor;
                try
                {
                    backupService.Backup(dlg.FileName);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
                lblStatus.Text = "Backup saved: " + dlg.FileName;
                MessageBox.Show("Backup saved to:\n" + dlg.FileName, "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Restore database from backup";
                dlg.Filter = "SQL backup (*.sql)|*.sql";
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                if (MessageBox.Show("Restoring replaces ALL current data (patients, appointments, admissions, bills, users and the " +
                                    "activity log) with the data in:\n\n" + dlg.FileName + "\n\nAnything entered since that backup " +
                                    "will be lost. Make sure no one else is using the system. Continue?",
                        "Restore database", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;

                Cursor = Cursors.WaitCursor;
                try
                {
                    backupService.Restore(dlg.FileName);
                    data.Reload();
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
                lblStatus.Text = "Restored from: " + dlg.FileName;
                MessageBox.Show("The database was restored. Other workstations should restart the app.", "Restore",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Designer-generated layout: title, then a backup section and a restore section.
        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblBackupTitle = new System.Windows.Forms.Label();
            this.lblBackupText = new System.Windows.Forms.Label();
            this.btnBackup = new System.Windows.Forms.Button();
            this.lblRestoreTitle = new System.Windows.Forms.Label();
            this.lblRestoreText = new System.Windows.Forms.Label();
            this.btnRestore = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.header.SuspendLayout();
            this.SuspendLayout();
            //
            // header
            //
            this.header.BackColor = System.Drawing.Color.White;
            this.header.Controls.Add(this.lblTitle);
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(0, 0);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(900, 64);
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
            this.lblTitle.Text = "Backup && Restore";
            //
            // lblBackupTitle
            //
            this.lblBackupTitle.AutoSize = true;
            this.lblBackupTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblBackupTitle.Location = new System.Drawing.Point(24, 90);
            this.lblBackupTitle.Name = "lblBackupTitle";
            this.lblBackupTitle.TabIndex = 1;
            this.lblBackupTitle.Text = "Back up";
            //
            // lblBackupText
            //
            this.lblBackupText.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblBackupText.Location = new System.Drawing.Point(24, 118);
            this.lblBackupText.Name = "lblBackupText";
            this.lblBackupText.Size = new System.Drawing.Size(600, 40);
            this.lblBackupText.TabIndex = 2;
            this.lblBackupText.Text = "Saves every record (patients, doctors, appointments, admissions, bills, users and the activity log) to one .sql file. Keep backups somewhere other than this computer.";
            //
            // btnBackup
            //
            this.btnBackup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnBackup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBackup.FlatAppearance.BorderSize = 0;
            this.btnBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackup.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnBackup.ForeColor = System.Drawing.Color.White;
            this.btnBackup.Location = new System.Drawing.Point(24, 166);
            this.btnBackup.Name = "btnBackup";
            this.btnBackup.Size = new System.Drawing.Size(180, 38);
            this.btnBackup.TabIndex = 3;
            this.btnBackup.Text = "Create backup...";
            this.btnBackup.UseVisualStyleBackColor = false;
            this.btnBackup.Click += new System.EventHandler(this.BtnBackup_Click);
            //
            // lblRestoreTitle
            //
            this.lblRestoreTitle.AutoSize = true;
            this.lblRestoreTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblRestoreTitle.Location = new System.Drawing.Point(24, 240);
            this.lblRestoreTitle.Name = "lblRestoreTitle";
            this.lblRestoreTitle.TabIndex = 4;
            this.lblRestoreTitle.Text = "Restore";
            //
            // lblRestoreText
            //
            this.lblRestoreText.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblRestoreText.Location = new System.Drawing.Point(24, 268);
            this.lblRestoreText.Name = "lblRestoreText";
            this.lblRestoreText.Size = new System.Drawing.Size(600, 40);
            this.lblRestoreText.TabIndex = 5;
            this.lblRestoreText.Text = "Replaces ALL current data with a backup file. Anything entered after that backup is lost, so make sure no one else is using the system first.";
            //
            // btnRestore
            //
            this.btnRestore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.btnRestore.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestore.FlatAppearance.BorderSize = 0;
            this.btnRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestore.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRestore.ForeColor = System.Drawing.Color.White;
            this.btnRestore.Location = new System.Drawing.Point(24, 316);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(180, 38);
            this.btnRestore.TabIndex = 6;
            this.btnRestore.Text = "Restore from file...";
            this.btnRestore.UseVisualStyleBackColor = false;
            this.btnRestore.Click += new System.EventHandler(this.BtnRestore_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblStatus.Location = new System.Drawing.Point(24, 380);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.TabIndex = 7;
            //
            // BackupView
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(246)))));
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.lblRestoreText);
            this.Controls.Add(this.lblRestoreTitle);
            this.Controls.Add(this.btnBackup);
            this.Controls.Add(this.lblBackupText);
            this.Controls.Add(this.lblBackupTitle);
            this.Controls.Add(this.header);
            this.Name = "BackupView";
            this.Size = new System.Drawing.Size(900, 600);
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}

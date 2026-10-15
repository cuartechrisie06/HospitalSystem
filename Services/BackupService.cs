using System.IO;
using System.Text;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    public interface IBackupService
    {
        // Saves the whole database to a .sql file.
        void Backup(string path);

        // Replaces ALL current data with the file's. Callers reload their data afterwards.
        void Restore(string path);
    }

    public class BackupService : IBackupService
    {
        private readonly IBackupRepository repository;
        private readonly IUserSession session;
        private readonly IPermissionGuard guard;
        private readonly IActivityLogger log;

        public BackupService(IBackupRepository repository, IUserSession session, IPermissionGuard guard, IActivityLogger log)
        {
            this.repository = repository;
            this.session = session;
            this.guard = guard;
            this.log = log;
        }

        public void Backup(string path)
        {
            guard.Demand(Permission.ManageBackups, "back up the database");
            string by = session.CurrentUser != null ? session.CurrentUser.DisplayName : ActivityItem.SystemUser;

            // Write to a temporary file first so a failed backup never leaves a half-written file behind.
            string temp = path + ".tmp";
            using (var writer = new StreamWriter(temp, false, new UTF8Encoding(false)))
                repository.Export(writer, by);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);

            log.Log("Maintenance", "Backup", $"Database backed up to {Path.GetFileName(path)}", "💾");
        }

        public void Restore(string path)
        {
            guard.Demand(Permission.ManageBackups, "restore the database");
            repository.Import(File.ReadAllText(path, Encoding.UTF8));

            // Logged after the restore, so the entry survives the activity log being replaced.
            log.Log("Maintenance", "Restore", $"Database restored from {Path.GetFileName(path)}", "♻");
        }
    }
}

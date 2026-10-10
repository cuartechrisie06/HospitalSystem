using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    // Every action in every module is recorded through here.
    public interface IActivityLogger
    {
        // username defaults to the signed-in user, or "System" for automatic actions.
        void Log(string module, string action, string description, string icon, string username = null);

        // Raised after each entry with its module. AlertMonitor listens to this so alerts follow
        // what happens in the modules, without the logger having to know about alerts.
        event Action<string> Logged;
    }

    // Reading the log back: only the activity log screen needs this.
    public interface IActivityLogReader
    {
        List<ActivityItem> Query(DateTime? from, DateTime? to, string module, string username, string search, int limit);
        List<string> Modules();
    }

    public class ActivityLogger : IActivityLogger
    {
        private readonly IActivityLogRepository repository;

        // Set after construction: the session itself logs sign-ins through this logger.
        public IUserSession Session { get; set; }

        public ActivityLogger(IActivityLogRepository repository)
        {
            this.repository = repository;
        }

        public event Action<string> Logged;

        public void Log(string module, string action, string description, string icon, string username = null)
        {
            var user = Session != null ? Session.CurrentUser : null;
            var item = new ActivityItem
            {
                Module = module,
                Action = action,
                Description = description,
                Icon = icon,
                Timestamp = DateTime.Now,
                Username = username ?? (user != null ? user.Username : ActivityItem.SystemUser)
            };

            try
            {
                repository.Insert(item);
            }
            catch (MySqlException)
            {
                // The action itself already succeeded; a logging failure must not undo or block it.
            }

            Logged?.Invoke(module);
        }
    }

    public class ActivityLogReader : IActivityLogReader
    {
        private readonly IActivityLogRepository repository;
        private readonly IPermissionGuard guard;

        public ActivityLogReader(IActivityLogRepository repository, IPermissionGuard guard)
        {
            this.repository = repository;
            this.guard = guard;
        }

        public List<ActivityItem> Query(DateTime? from, DateTime? to, string module, string username, string search, int limit)
        {
            guard.Demand(Permission.ViewActivityLog, "view the activity log");
            return repository.Query(from, to, module, username, search, limit);
        }

        public List<string> Modules() => repository.Modules();
    }
}

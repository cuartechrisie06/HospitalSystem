using System;
using System.Collections.Generic;
using System.Linq;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    // Alerts are only ever raised and resolved by AlertMonitor from conditions in the
    // other modules; staff can acknowledge one, which hides it until the condition
    // clears (and raises it again if the condition comes back later).
    public interface IAlertService
    {
        // Active + Acknowledged.
        List<Alert> Open();
        List<Alert> Active();
        List<Alert> Acknowledged();

        // Staff saw it and are handling it. Old manual alerts (no trigger) are closed outright.
        void Acknowledge(int alertId);
    }

    // Raising, updating and resolving: only AlertMonitor does this, so it's kept off IAlertService.
    public interface IAlertWriter
    {
        Alert Raise(string sourceKey, string module, string title, string message, string severity);

        // Same condition still holding, but its details changed (e.g. 1 ICU bed left -> 0).
        void Update(Alert a, string title, string message, string severity);

        // The condition cleared: close the alert (history stays in the alerts table).
        void AutoResolve(Alert a);
    }

    public class AlertService : IAlertService, IAlertWriter, IReloadable
    {
        private readonly IAlertRepository repository;
        private readonly IUserSession session;
        private readonly IActivityLogger log;
        private readonly List<Alert> alerts;

        public AlertService(IAlertRepository repository, IUserSession session, IActivityLogger log)
        {
            this.repository = repository;
            this.session = session;
            this.log = log;
            alerts = repository.GetOpen();
        }

        public void Reload()
        {
            alerts.Clear();
            alerts.AddRange(repository.GetOpen());
        }

        public List<Alert> Open() => alerts.ToList();

        public List<Alert> Active() =>
            alerts.Where(a => a.IsActive).OrderBy(a => a.SeverityRank).ThenByDescending(a => a.CreatedOn).ToList();

        public List<Alert> Acknowledged() =>
            alerts.Where(a => a.IsAcknowledged).OrderBy(a => a.SeverityRank).ThenByDescending(a => a.CreatedOn).ToList();

        public Alert Raise(string sourceKey, string module, string title, string message, string severity)
        {
            var a = new Alert
            {
                SourceKey = sourceKey,
                Module = module,
                Title = title,
                Message = message,
                Severity = severity,
                Status = AlertStatus.Active,
                IsAuto = true,
                CreatedOn = DateTime.Now
            };

            repository.Insert(a);
            alerts.Add(a);
            log.Log(AlertMonitor.ModuleName, "Raised", $"[{severity}] {title}: {message} (from {module})", "⚠️", ActivityItem.SystemUser);
            return a;
        }

        public void Update(Alert a, string title, string message, string severity)
        {
            bool escalated = SeverityRankOf(severity) < a.SeverityRank;
            a.Title = title;
            a.Message = message;
            a.Severity = severity;
            // A worse situation than the one that was acknowledged needs attention again.
            if (escalated && a.IsAcknowledged)
            {
                a.Status = AlertStatus.Active;
                a.AcknowledgedBy = null;
            }

            repository.Update(a);

            if (escalated)
                log.Log(AlertMonitor.ModuleName, "Escalated", $"[{severity}] {title}: {message}", "⚠️", ActivityItem.SystemUser);
        }

        public void AutoResolve(Alert a)
        {
            a.Status = AlertStatus.Resolved;
            a.ResolvedOn = DateTime.Now;
            alerts.Remove(a);

            repository.UpdateStatus(a);
            log.Log(AlertMonitor.ModuleName, "Auto-resolved", $"{a.Title} cleared: condition no longer holds", "✅", ActivityItem.SystemUser);
        }

        public void Acknowledge(int alertId)
        {
            var a = alerts.FirstOrDefault(x => x.Id == alertId);
            if (a == null || !a.IsActive) return;

            var user = session.CurrentUser;
            bool manual = a.SourceKey == null;
            a.Status = manual ? AlertStatus.Resolved : AlertStatus.Acknowledged;
            a.AcknowledgedBy = user != null ? user.DisplayName : ActivityItem.SystemUser;
            if (manual)
            {
                a.ResolvedOn = DateTime.Now;
                alerts.Remove(a);
            }

            repository.UpdateStatus(a);
            log.Log(AlertMonitor.ModuleName, manual ? "Resolved" : "Acknowledged", $"{a.Title}: {a.Message}", "👁");
        }

        private static int SeverityRankOf(string severity) =>
            severity == "High" ? 0 : severity == "Medium" ? 1 : 2;
    }
}

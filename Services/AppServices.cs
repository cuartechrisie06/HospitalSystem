using System.Collections.Generic;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    // Composition root: the one place that knows which concrete classes are used and how they
    // fit together. Forms and views get the interfaces they need through their constructors.
    public sealed class AppServices : IReloadable
    {
        public IAuthService Auth { get; private set; }
        public IUserSession Session { get; private set; }
        public IUserDirectory Users { get; private set; }
        public IPermissionGuard Guard { get; private set; }
        public IActivityLogger Log { get; private set; }
        public IActivityLogReader LogReader { get; private set; }

        public IPatientService Patients { get; private set; }
        public IDepartmentService Departments { get; private set; }
        public IDoctorService Doctors { get; private set; }
        public IBedService Beds { get; private set; }
        public IAppointmentService Appointments { get; private set; }
        public IAdmissionService Admissions { get; private set; }
        public IChargeScheduleService ChargeSchedules { get; private set; }
        public IBillingService Billing { get; private set; }
        public IAlertService Alerts { get; private set; }
        public IAlertMonitor AlertMonitor { get; private set; }
        public IReportService Reports { get; private set; }
        public IBackupService Backup { get; private set; }

        private List<IReloadable> reloadables;

        // Upgrades the schema, loads everything from MySQL and wires the services together.
        public static AppServices Create(IDbConnectionFactory db)
        {
            var migrator = new SchemaMigrator(db);
            int retiredAlerts = migrator.Migrate();
            var app = Create(new MySqlRepositorySet(db), retiredAlerts);
            foreach (var note in migrator.Notes)
                app.Log.Log("Maintenance", "Upgrade", note, "🛠", ActivityItem.SystemUser);
            return app;
        }

        // Wires the services together over any set of repositories (MySQL in the app, fakes in the tests).
        public static AppServices Create(IRepositorySet repositories, int retiredAlerts = 0)
        {
            var log = new ActivityLogger(repositories.ActivityLog);
            var auth = new AuthService(repositories.Users, log);
            log.Session = auth;
            var guard = new PermissionGuard(auth, log);

            var patients = new PatientService(repositories.Patients, repositories.Admissions, auth, log, guard);
            var departments = new DepartmentService(repositories.Departments);
            var doctors = new DoctorService(repositories.Doctors, departments, log, guard);
            var beds = new BedService(repositories.Beds);
            var appointments = new AppointmentService(repositories.Appointments, patients, doctors, log);
            var admissions = new AdmissionService(repositories.Admissions, beds, beds, patients, doctors, log);
            var chargeSchedules = new ChargeScheduleService(repositories.ChargeSchedules, log, guard);
            var billing = new BillingService(repositories.Bills, chargeSchedules, beds, patients, doctors,
                auth, guard, log);
            var alerts = new AlertService(repositories.Alerts, auth, log);
            var monitor = new AlertMonitor(alerts, alerts, beds, admissions, departments, doctors, appointments,
                billing, patients);

            // Modules react to each other through events, so none of them has to call the other directly.
            admissions.Discharged += billing.SyncAdmissionBillOnDischarge;
            log.Logged += monitor.OnActivityLogged;

            if (retiredAlerts > 0)
                log.Log(Services.AlertMonitor.ModuleName, "Retired",
                    $"Closed {retiredAlerts} manually posted alert(s): alerts are now raised and cleared automatically", "🧹",
                    ActivityItem.SystemUser);

            // Conditions may have changed while the app was closed (e.g. appointments now overdue).
            monitor.Evaluate();

            return new AppServices
            {
                Auth = auth,
                Session = auth,
                Users = auth,
                Guard = guard,
                Log = log,
                LogReader = new ActivityLogReader(repositories.ActivityLog, guard),
                Patients = patients,
                Departments = departments,
                Doctors = doctors,
                Beds = beds,
                Appointments = appointments,
                Admissions = admissions,
                ChargeSchedules = chargeSchedules,
                Billing = billing,
                Alerts = alerts,
                AlertMonitor = monitor,
                Reports = new ReportService(admissions, appointments, beds, billing, guard, log),
                Backup = new BackupService(repositories.Backup, auth, guard, log),
                reloadables = new List<IReloadable>
                {
                    auth, patients, departments, doctors, beds, appointments, admissions, chargeSchedules, billing, alerts
                }
            };
        }

        // Re-reads everything from the database so changes made on other workstations show up.
        // DashboardForm calls this before opening a module.
        public void Reload()
        {
            foreach (var service in reloadables)
                service.Reload();
            AlertMonitor.Evaluate();
        }
    }
}

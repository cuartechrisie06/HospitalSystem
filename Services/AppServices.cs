using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    // Composition root: the one place that knows which concrete classes are used and how they
    // fit together. Forms and views get the interfaces they need through their constructors.
    public sealed class AppServices
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

        // Upgrades the schema, loads everything from MySQL and wires the services together.
        public static AppServices Create(IDbConnectionFactory db)
        {
            int retiredAlerts = new SchemaMigrator(db).Migrate();

            var logRepository = new MySqlActivityLogRepository(db);
            var log = new ActivityLogger(logRepository);
            var auth = new AuthService(new MySqlUserRepository(db), log);
            log.Session = auth;
            var guard = new PermissionGuard(auth, log);

            var patients = new PatientService(new MySqlPatientRepository(db), log, guard);
            var departments = new DepartmentService(new MySqlDepartmentRepository(db));
            var doctors = new DoctorService(new MySqlDoctorRepository(db), departments, log, guard);
            var beds = new BedService(new MySqlBedRepository(db));
            var appointments = new AppointmentService(new MySqlAppointmentRepository(db), patients, doctors, log);
            var admissions = new AdmissionService(new MySqlAdmissionRepository(db), beds, beds, patients, log);
            var chargeSchedules = new ChargeScheduleService(new MySqlChargeScheduleRepository(db), log, guard);
            var billing = new BillingService(new MySqlBillRepository(db), chargeSchedules, beds, patients, doctors,
                auth, guard, log);
            var alerts = new AlertService(new MySqlAlertRepository(db), auth, log);
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
                LogReader = new ActivityLogReader(logRepository, guard),
                Patients = patients,
                Departments = departments,
                Doctors = doctors,
                Beds = beds,
                Appointments = appointments,
                Admissions = admissions,
                ChargeSchedules = chargeSchedules,
                Billing = billing,
                Alerts = alerts,
                AlertMonitor = monitor
            };
        }
    }
}

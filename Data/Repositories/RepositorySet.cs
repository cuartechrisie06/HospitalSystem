namespace HospitalSystem.Data.Repositories
{
    // Every repository the services need. AppServices builds the app from one of these, so the same
    // wiring runs against MySQL in the app and against in-memory fakes in the tests.
    public interface IRepositorySet
    {
        IUserRepository Users { get; }
        IPatientRepository Patients { get; }
        IDepartmentRepository Departments { get; }
        IDoctorRepository Doctors { get; }
        IBedRepository Beds { get; }
        IAppointmentRepository Appointments { get; }
        IAdmissionRepository Admissions { get; }
        IChargeScheduleRepository ChargeSchedules { get; }
        IBillRepository Bills { get; }
        IAlertRepository Alerts { get; }
        IActivityLogRepository ActivityLog { get; }
    }

    public class MySqlRepositorySet : IRepositorySet
    {
        public MySqlRepositorySet(IDbConnectionFactory db)
        {
            Users = new MySqlUserRepository(db);
            Patients = new MySqlPatientRepository(db);
            Departments = new MySqlDepartmentRepository(db);
            Doctors = new MySqlDoctorRepository(db);
            Beds = new MySqlBedRepository(db);
            Appointments = new MySqlAppointmentRepository(db);
            Admissions = new MySqlAdmissionRepository(db);
            ChargeSchedules = new MySqlChargeScheduleRepository(db);
            Bills = new MySqlBillRepository(db);
            Alerts = new MySqlAlertRepository(db);
            ActivityLog = new MySqlActivityLogRepository(db);
        }

        public IUserRepository Users { get; }
        public IPatientRepository Patients { get; }
        public IDepartmentRepository Departments { get; }
        public IDoctorRepository Doctors { get; }
        public IBedRepository Beds { get; }
        public IAppointmentRepository Appointments { get; }
        public IAdmissionRepository Admissions { get; }
        public IChargeScheduleRepository ChargeSchedules { get; }
        public IBillRepository Bills { get; }
        public IAlertRepository Alerts { get; }
        public IActivityLogRepository ActivityLog { get; }
    }
}

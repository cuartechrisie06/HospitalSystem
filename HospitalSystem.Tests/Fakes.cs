using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;
using HospitalSystem.Services;

namespace HospitalSystem.Tests
{
    // In-memory stand-ins for the MySQL repositories. They behave like the database where the services
    // rely on it (ids, conditional bed updates, clash and open-admission queries), so the real services
    // and the real wiring in AppServices run unchanged in the tests.
    public class FakeRepositories : IRepositorySet
    {
        public readonly FakeUsers UserStore = new FakeUsers();
        public readonly FakeTable<Patient> PatientStore = new FakeTable<Patient>(p => p.Id, (p, id) => p.Id = id);
        public readonly FakeTable<Department> DepartmentStore = new FakeTable<Department>(d => d.Id, (d, id) => d.Id = id);
        public readonly FakeTable<Doctor> DoctorStore = new FakeTable<Doctor>(d => d.Id, (d, id) => d.Id = id);
        public readonly FakeBeds BedStore = new FakeBeds();
        public readonly FakeAppointments AppointmentStore = new FakeAppointments();
        public readonly FakeAdmissions AdmissionStore = new FakeAdmissions();
        public readonly FakeTable<ChargeSchedule> ChargeStore = new FakeTable<ChargeSchedule>(c => c.Id, (c, id) => c.Id = id);
        public readonly FakeBills BillStore = new FakeBills();
        public readonly FakeAlerts AlertStore = new FakeAlerts();
        public readonly FakeActivityLog LogStore = new FakeActivityLog();

        public IUserRepository Users => UserStore;
        public IPatientRepository Patients => new FakePatients(PatientStore);
        public IDepartmentRepository Departments => new FakeDepartments(DepartmentStore);
        public IDoctorRepository Doctors => new FakeDoctors(DoctorStore);
        public IBedRepository Beds => BedStore;
        public IAppointmentRepository Appointments => AppointmentStore;
        public IAdmissionRepository Admissions => AdmissionStore;
        public IChargeScheduleRepository ChargeSchedules => new FakeCharges(ChargeStore);
        public IBillRepository Bills => BillStore;
        public IAlertRepository Alerts => AlertStore;
        public IActivityLogRepository ActivityLog => LogStore;
        public IBackupRepository Backup => new FakeBackup();

        // Same shape as the seed in Data/schema.sql.
        public static FakeRepositories Seeded()
        {
            var r = new FakeRepositories();
            r.UserStore.Rows.Add(new User { Username = "admin", Password = "admin", DisplayName = "System Administrator", Role = "Administrator" });
            r.UserStore.Rows.Add(new User { Username = "nurse", Password = "nurse", DisplayName = "Ward Nurse", Role = "Nurse" });

            foreach (var name in new[] { "General Medicine", "Pediatrics", "Surgery", "Cardiology" })
                r.DepartmentStore.Insert(new Department { Name = name });

            r.DoctorStore.Insert(new Doctor { FullName = "Ana Reyes", DepartmentId = 1, IsOnDuty = true });
            r.DoctorStore.Insert(new Doctor { FullName = "Jose Cruz", DepartmentId = 1, IsOnDuty = true });
            r.DoctorStore.Insert(new Doctor { FullName = "Liza Tan", DepartmentId = 2, IsOnDuty = true });
            r.DoctorStore.Insert(new Doctor { FullName = "Mark Lim", DepartmentId = 3, IsOnDuty = true });
            r.DoctorStore.Insert(new Doctor { FullName = "Grace Go", DepartmentId = 4, IsOnDuty = true });

            foreach (var bed in BedLayout.Beds)   // bed ids 1-16, same as schema.sql
                r.BedStore.Insert(new Bed { RoomNo = bed.Room, BedNo = bed.Bed, Ward = bed.Ward });

            r.PatientStore.Insert(new Patient { FullName = "Juan Dela Cruz", Birthdate = DateTime.Today.AddYears(-45), Gender = "Male", Contact = "09171234567", RegisteredOn = DateTime.Today });
            r.PatientStore.Insert(new Patient { FullName = "Maria Santos", Birthdate = DateTime.Today.AddYears(-70), Gender = "Female", Contact = "09189876543", RegisteredOn = DateTime.Today });
            r.PatientStore.Insert(new Patient { FullName = "Pedro Ramirez", Birthdate = DateTime.Today.AddYears(-28), Gender = "Male", Contact = "09221234567", RegisteredOn = DateTime.Today });

            r.ChargeStore.Insert(new ChargeSchedule { Description = "Admission fee", Category = BillCategory.Other, UnitPrice = 500m });
            r.ChargeStore.Insert(new ChargeSchedule { Description = "Room - Ward", Category = BillCategory.Room, UnitPrice = BedLayout.WardRate, Ward = BedLayout.Ward, PerDay = true });
            r.ChargeStore.Insert(new ChargeSchedule { Description = "Room - Semi-Private", Category = BillCategory.Room, UnitPrice = BedLayout.SemiPrivateRate, Ward = BedLayout.SemiPrivate, PerDay = true });
            r.ChargeStore.Insert(new ChargeSchedule { Description = "Room - ICU", Category = BillCategory.Room, UnitPrice = 8000m, Ward = "ICU", PerDay = true });
            return r;
        }
    }

    public class FakeTable<T>
    {
        private readonly Func<T, int> getId;
        private readonly Action<T, int> setId;
        public readonly List<T> Rows = new List<T>();
        private int nextId = 1;

        public FakeTable(Func<T, int> getId, Action<T, int> setId)
        {
            this.getId = getId;
            this.setId = setId;
        }

        public void Insert(T item)
        {
            setId(item, nextId++);
            Rows.Add(item);
        }

        public T Get(int id) => Rows.FirstOrDefault(r => getId(r) == id);

        // A database hands back fresh objects on every load; so does the fake (shallow copies).
        public List<T> Snapshot() => Rows.Select(Clone).ToList();

        private static T Clone(T item) =>
            (T)typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(item, null);
    }

    public class FakeUsers : IUserRepository
    {
        public readonly List<User> Rows = new List<User>();

        public List<User> GetAll() => Rows.Select(Copy).ToList();

        public User Get(string username)
        {
            var u = Rows.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
            return u == null ? null : Copy(u);
        }

        public void UpdatePassword(User u) => Find(u).Password = u.Password;

        public void UpdateLoginState(User u)
        {
            var row = Find(u);
            row.FailedAttempts = u.FailedAttempts;
            row.LockedUntil = u.LockedUntil;
        }

        private User Find(User u) => Rows.First(x => x.Username == u.Username);

        private static User Copy(User u) => new User
        {
            Username = u.Username, Password = u.Password, DisplayName = u.DisplayName, Role = u.Role,
            FailedAttempts = u.FailedAttempts, LockedUntil = u.LockedUntil
        };
    }

    public class FakePatients : IPatientRepository
    {
        private readonly FakeTable<Patient> t;
        public FakePatients(FakeTable<Patient> t) { this.t = t; }
        public List<Patient> GetAll() => t.Snapshot();
        public void Insert(Patient p) => t.Insert(p);
        public void Update(Patient p) { }
        public void UpdateStatus(Patient p) { }
        public int NextId() => t.Rows.Count == 0 ? 1 : t.Rows.Max(p => p.Id) + 1;
    }

    public class FakeDepartments : IDepartmentRepository
    {
        private readonly FakeTable<Department> t;
        public FakeDepartments(FakeTable<Department> t) { this.t = t; }
        public List<Department> GetAll() => t.Snapshot();
    }

    public class FakeDoctors : IDoctorRepository
    {
        private readonly FakeTable<Doctor> t;
        public FakeDoctors(FakeTable<Doctor> t) { this.t = t; }
        public List<Doctor> GetAll() => t.Snapshot();
        public void Insert(Doctor d) => t.Insert(d);
        public void Update(Doctor d) { }
        public void UpdateStatus(Doctor d) { }
    }

    public class FakeBeds : IBedRepository
    {
        public readonly FakeTable<Bed> Table = new FakeTable<Bed>(b => b.Id, (b, id) => b.Id = id);
        public void Insert(Bed b) => Table.Insert(b);
        public List<Bed> GetAll() => Table.Snapshot();

        public bool TryOccupy(int bedId)
        {
            var bed = Table.Get(bedId);
            if (bed == null || bed.IsOccupied) return false;
            bed.IsOccupied = true;
            return true;
        }

        public void Release(int bedId)
        {
            var bed = Table.Get(bedId);
            if (bed != null) bed.IsOccupied = false;
        }
    }

    public class FakeAppointments : IAppointmentRepository
    {
        public readonly FakeTable<Appointment> Table = new FakeTable<Appointment>(a => a.Id, (a, id) => a.Id = id);
        public List<Appointment> GetAll() => Table.Snapshot();
        public void Insert(Appointment a) => Table.Insert(a);
        public void UpdateStatus(Appointment a) => Table.Get(a.Id).Status = a.Status;
        public void UpdateSchedule(Appointment a) => Table.Get(a.Id).ScheduledOn = a.ScheduledOn;

        public bool HasClash(int doctorId, DateTime when, int ignoreId, int windowMinutes) =>
            Table.Rows.Any(a => a.Id != ignoreId && a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled &&
                                Math.Abs((a.ScheduledOn - when).TotalMinutes) < windowMinutes);
    }

    public class FakeAdmissions : IAdmissionRepository
    {
        public readonly FakeTable<Admission> Table = new FakeTable<Admission>(a => a.Id, (a, id) => a.Id = id);
        public List<Admission> GetAll() => Table.Snapshot();
        public void Insert(Admission a) => Table.Insert(a);
        public void UpdateBedAssignment(Admission a) { var r = Table.Get(a.Id); r.BedId = a.BedId; r.AdmittedOn = a.AdmittedOn; r.Status = a.Status; }
        public void UpdateDischarge(Admission a) { var r = Table.Get(a.Id); r.DischargedOn = a.DischargedOn; r.Status = a.Status; }
        public void UpdateStatus(Admission a) => Table.Get(a.Id).Status = a.Status;

        public bool HasOpenAdmission(int patientId) =>
            Table.Rows.Any(a => a.PatientId == patientId && (a.Status == AdmissionStatus.Active || a.Status == AdmissionStatus.Pending));
    }

    public class FakeCharges : IChargeScheduleRepository
    {
        private readonly FakeTable<ChargeSchedule> t;
        public FakeCharges(FakeTable<ChargeSchedule> t) { this.t = t; }
        public List<ChargeSchedule> GetAll() => t.Snapshot();
        public void Insert(ChargeSchedule c) => t.Insert(c);
        public void Update(ChargeSchedule c) { }
    }

    public class FakeBills : IBillRepository
    {
        public readonly List<Bill> Rows = new List<Bill>();
        private int nextBill = 1, nextItem = 1, nextPayment = 1;
        public int SaveCount;

        public List<Bill> GetAll() => Rows.ToList();

        public void Insert(Bill b)
        {
            b.Id = nextBill++;
            foreach (var item in b.Items) { item.BillId = b.Id; item.Id = nextItem++; }
            Rows.Add(b);
            SaveCount++;
        }

        public void InsertItem(Bill b, BillItem item) { item.Id = nextItem++; SaveCount++; }
        public void DeleteItem(Bill b, int itemId) => SaveCount++;
        public void UpdateItems(Bill b, IEnumerable<BillItem> items) => SaveCount++;
        public void InsertPayment(Bill b, Payment p) { p.Id = nextPayment++; SaveCount++; }
        public void SaveTotals(Bill b) => SaveCount++;
    }

    public class FakeAlerts : IAlertRepository
    {
        public readonly List<Alert> Rows = new List<Alert>();
        private int nextId = 1;
        public List<Alert> GetOpen() => Rows.Where(a => a.Status != AlertStatus.Resolved).ToList();
        public void Insert(Alert a) { a.Id = nextId++; Rows.Add(a); }
        public void Update(Alert a) { }
        public void UpdateStatus(Alert a) { }
    }

    public class FakeActivityLog : IActivityLogRepository
    {
        public readonly List<ActivityItem> Rows = new List<ActivityItem>();
        public void Insert(ActivityItem item) { item.Id = Rows.Count + 1; Rows.Add(item); }

        public List<ActivityItem> Query(DateTime? from, DateTime? to, string module, string username, string search, int limit) =>
            Rows.Where(r => module == null || r.Module == module).Take(limit).ToList();

        public List<string> Modules() => Rows.Select(r => r.Module).Distinct().ToList();

        public bool Has(string module, string action) => Rows.Any(r => r.Module == module && r.Action == action);
    }

    public class FakeBackup : IBackupRepository
    {
        public void Export(TextWriter writer, string createdBy) => writer.Write(MySqlBackupRepository.Header);
        public void Import(string script) { }
    }

    public static class TestApp
    {
        public static AppServices Start(out FakeRepositories repos, string signInAs = "admin")
        {
            repos = FakeRepositories.Seeded();
            var app = AppServices.Create(repos);
            if (signInAs != null)
                app.Auth.SignIn(signInAs, signInAs);
            return app;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HospitalSystem.Models;

namespace HospitalSystem.Data
{
    public static class HospitalData
    {
        public static List<User> Users { get; private set; } = new List<User>();
        public static List<Patient> Patients { get; private set; } = new List<Patient>();
        public static List<Department> Departments { get; private set; } = new List<Department>();
        public static List<Doctor> Doctors { get; private set; } = new List<Doctor>();
        public static List<Appointment> Appointments { get; private set; } = new List<Appointment>();
        public static List<Admission> Admissions { get; private set; } = new List<Admission>();
        public static List<Bed> Beds { get; private set; } = new List<Bed>();
        public static List<Alert> Alerts { get; private set; } = new List<Alert>();

        public static User CurrentUser { get; set; }

        private static int _patientSeq = 0;
        private static int _appointmentSeq = 0;
        private static int _admissionSeq = 0;

        static HospitalData()
        {
            Seed();
        }

        private static void Seed()
        {
            // Users
            Users.Add(new User { Username = "admin", Password = "admin", DisplayName = "System Administrator", Role = "Administrator" });
            Users.Add(new User { Username = "nurse", Password = "nurse", DisplayName = "Ward Nurse", Role = "Nurse" });

            // Departments
            Departments.Add(new Department { Id = 1, Name = "General Medicine" });
            Departments.Add(new Department { Id = 2, Name = "Pediatrics" });
            Departments.Add(new Department { Id = 3, Name = "Surgery" });
            Departments.Add(new Department { Id = 4, Name = "Cardiology" });

            // Doctors (with On Duty)
            Doctors.Add(new Doctor { Id = 1, FullName = "Ana Reyes", DepartmentId = 1, Specialization = "Internal Medicine", IsOnDuty = true });
            Doctors.Add(new Doctor { Id = 2, FullName = "Mark Villanueva", DepartmentId = 1, Specialization = "Family Medicine", IsOnDuty = true });
            Doctors.Add(new Doctor { Id = 3, FullName = "Liza Tan", DepartmentId = 2, Specialization = "Pediatrics", IsOnDuty = false });
            Doctors.Add(new Doctor { Id = 4, FullName = "Jose Cruz", DepartmentId = 3, Specialization = "General Surgery", IsOnDuty = true });
            Doctors.Add(new Doctor { Id = 5, FullName = "Grace Lim", DepartmentId = 4, Specialization = "Cardiology", IsOnDuty = false });

            // Beds (12 beds across 6 rooms)
            string[] wards = { "General Ward", "Private", "ICU" };
            int bedId = 1;
            for (int room = 101; room <= 106; room++)
            {
                string ward = wards[(room - 101) % 3];
                for (int b = 1; b <= 2; b++)
                {
                    Beds.Add(new Bed
                    {
                        Id = bedId++,
                        RoomNo = room.ToString(),
                        BedNo = b.ToString(),
                        Ward = ward,
                        IsOccupied = false
                    });
                }
            }

            // Sample patients
            AddPatient(new Patient
            {
                FullName = "Juan Dela Cruz",
                Age = 45,
                Gender = "Male",
                Contact = "09171234567",
                Address = "Quezon City",
                BloodType = "O+",
                RegisteredOn = DateTime.Today.AddDays(-10)
            });

            AddPatient(new Patient
            {
                FullName = "Maria Santos",
                Age = 32,
                Gender = "Female",
                Contact = "09189876543",
                Address = "Makati City",
                BloodType = "A+",
                RegisteredOn = DateTime.Today.AddDays(-5)
            });

            AddPatient(new Patient
            {
                FullName = "Pedro Ramirez",
                Age = 28,
                Gender = "Male",
                Contact = "09221234567",
                Address = "Pasig City",
                BloodType = "B+",
                RegisteredOn = DateTime.Today.AddDays(-2)
            });

            // Sample Emergency Alerts
            Alerts.Add(new Alert
            {
                Id = 1,
                Title = "ICU Bed Critical",
                Message = "Only 1 ICU bed remaining",
                Severity = "High",
                CreatedOn = DateTime.Now.AddHours(-2)
            });
            Alerts.Add(new Alert
            {
                Id = 2,
                Title = "Staff Shortage",
                Message = "Pediatrics has no doctor on duty",
                Severity = "Medium",
                CreatedOn = DateTime.Now.AddHours(-5)
            });
            Alerts.Add(new Alert
            {
                Id = 3,
                Title = "Equipment Maintenance",
                Message = "X-Ray machine scheduled for maintenance tomorrow",
                Severity = "Low",
                CreatedOn = DateTime.Now.AddDays(-1)
            });
        }

        // -------------------- Authentication --------------------
        public static User Authenticate(string username, string password)
        {
            return Users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }

        // -------------------- Patients --------------------
        public static Patient AddPatient(Patient p)
        {
            p.Id = ++_patientSeq;
            if (p.RegisteredOn == default)
                p.RegisteredOn = DateTime.Now;
            Patients.Add(p);
            return p;
        }

        public static Patient GetPatient(int id) => Patients.FirstOrDefault(x => x.Id == id);

        public static string PatientName(int id)
        {
            var p = GetPatient(id);
            return p != null ? p.FullName : "(Unknown)";
        }

        // -------------------- Appointments --------------------
        public static Appointment AddAppointment(Appointment a)
        {
            a.Id = ++_appointmentSeq;
            if (string.IsNullOrEmpty(a.Status))
                a.Status = "Pending";
            Appointments.Add(a);
            return a;
        }

        public static List<Appointment> AppointmentsToday()
        {
            return Appointments
                .Where(a => a.ScheduledOn.Date == DateTime.Today && a.Status != "Cancelled")
                .ToList();
        }

        // -------------------- Admissions --------------------
        public static Admission AddAdmission(Admission a)
        {
            a.Id = ++_admissionSeq;
            a.AdmittedOn = DateTime.Now;
            a.Status = "Active";
            Admissions.Add(a);

            var bed = GetBed(a.BedId);
            if (bed != null)
                bed.IsOccupied = true;

            return a;
        }

        public static void Discharge(Admission a)
        {
            if (a == null) return;
            a.DischargedOn = DateTime.Now;
            a.Status = "Discharged";

            var bed = GetBed(a.BedId);
            if (bed != null)
                bed.IsOccupied = false;
        }

        public static List<Admission> ActiveAdmissions() => Admissions.Where(a => a.Status == "Active").ToList();

        // -------------------- Beds & Helpers --------------------
        public static Bed GetBed(int id) => Beds.FirstOrDefault(b => b.Id == id);

        public static List<Bed> AvailableBeds() => Beds.Where(b => !b.IsOccupied).ToList();

        public static string BedLabel(int id)
        {
            var b = GetBed(id);
            return b != null ? b.Label : "(Unknown)";
        }

        public static Doctor GetDoctor(int id) => Doctors.FirstOrDefault(d => d.Id == id);

        public static string DoctorName(int id)
        {
            var d = GetDoctor(id);
            return d != null ? "Dr. " + d.FullName : "(Unknown)";
        }

        public static Department GetDepartment(int id) => Departments.FirstOrDefault(d => d.Id == id);

        public static string DepartmentName(int id)
        {
            var d = GetDepartment(id);
            return d != null ? d.Name : "(Unknown)";
        }

        // -------------------- New Dashboard Helpers --------------------
        public static int OccupiedBedsCount() => Beds.Count(b => b.IsOccupied);
        public static int TotalBedsCount() => Beds.Count;
        public static double OccupancyRate() => TotalBedsCount() == 0 ? 0 : (double)OccupiedBedsCount() / TotalBedsCount() * 100;

        public static List<Doctor> DoctorsOnDuty() => Doctors.Where(d => d.IsOnDuty).ToList();

        public static List<Alert> ActiveAlerts() => Alerts.OrderByDescending(a => a.CreatedOn).ToList();
    }
}
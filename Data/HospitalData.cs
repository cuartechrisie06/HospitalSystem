using System;
using System.Collections.Generic;
using System.Linq;
using HospitalSystem.Models;

namespace HospitalSystem.Data
{
    /// <summary>
    /// In-memory data store.
    /// All lists live only while the application is running.
    /// Replace this class later with a real database layer if needed.
    /// </summary>
    public static class HospitalData
    {
        public static List<User> Users { get; private set; } = new List<User>();
        public static List<Patient> Patients { get; private set; } = new List<Patient>();
        public static List<Department> Departments { get; private set; } = new List<Department>();
        public static List<Doctor> Doctors { get; private set; } = new List<Doctor>();
        public static List<Appointment> Appointments { get; private set; } = new List<Appointment>();
        public static List<Admission> Admissions { get; private set; } = new List<Admission>();
        public static List<Bed> Beds { get; private set; } = new List<Bed>();

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

            // Doctors
            Doctors.Add(new Doctor { Id = 1, FullName = "Ana Reyes", DepartmentId = 1, Specialization = "Internal Medicine" });
            Doctors.Add(new Doctor { Id = 2, FullName = "Mark Villanueva", DepartmentId = 1, Specialization = "Family Medicine" });
            Doctors.Add(new Doctor { Id = 3, FullName = "Liza Tan", DepartmentId = 2, Specialization = "Pediatrics" });
            Doctors.Add(new Doctor { Id = 4, FullName = "Jose Cruz", DepartmentId = 3, Specialization = "General Surgery" });
            Doctors.Add(new Doctor { Id = 5, FullName = "Grace Lim", DepartmentId = 4, Specialization = "Cardiology" });

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
            p.RegisteredOn = DateTime.Now;
            Patients.Add(p);
            return p;
        }

        public static Patient GetPatient(int id)
        {
            return Patients.FirstOrDefault(x => x.Id == id);
        }

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

        public static List<Admission> ActiveAdmissions()
        {
            return Admissions.Where(a => a.Status == "Active").ToList();
        }

        // -------------------- Beds & Helpers --------------------

        public static Bed GetBed(int id)
        {
            return Beds.FirstOrDefault(b => b.Id == id);
        }

        public static List<Bed> AvailableBeds()
        {
            return Beds.Where(b => !b.IsOccupied).ToList();
        }

        public static string BedLabel(int id)
        {
            var b = GetBed(id);
            return b != null ? b.Label : "(Unknown)";
        }

        public static Doctor GetDoctor(int id)
        {
            return Doctors.FirstOrDefault(d => d.Id == id);
        }

        public static string DoctorName(int id)
        {
            var d = GetDoctor(id);
            return d != null ? "Dr. " + d.FullName : "(Unknown)";
        }

        public static Department GetDepartment(int id)
        {
            return Departments.FirstOrDefault(d => d.Id == id);
        }

        public static string DepartmentName(int id)
        {
            var d = GetDepartment(id);
            return d != null ? d.Name : "(Unknown)";
        }
    }
}

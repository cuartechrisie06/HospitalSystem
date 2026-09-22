using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
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

        static HospitalData()
        {
            LoadAll();
        }

        // -------------------- Loading from MySQL --------------------
        private static void LoadAll()
        {
            using (var conn = Db.OpenConnection())
            {
                Users = LoadUsers(conn);
                Departments = LoadDepartments(conn);
                Doctors = LoadDoctors(conn);
                Beds = LoadBeds(conn);
                Patients = LoadPatients(conn);
                Appointments = LoadAppointments(conn);
                Admissions = LoadAdmissions(conn);
                Alerts = LoadAlerts(conn);
            }
        }

        private static List<User> LoadUsers(MySqlConnection conn)
        {
            var list = new List<User>();
            using (var cmd = new MySqlCommand("SELECT username, password, display_name, role FROM users", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new User
                    {
                        Username = r.GetString("username"),
                        Password = r.GetString("password"),
                        DisplayName = r.IsDBNull(r.GetOrdinal("display_name")) ? null : r.GetString("display_name"),
                        Role = r.IsDBNull(r.GetOrdinal("role")) ? null : r.GetString("role")
                    });
                }
            }
            return list;
        }

        private static List<Department> LoadDepartments(MySqlConnection conn)
        {
            var list = new List<Department>();
            using (var cmd = new MySqlCommand("SELECT id, name FROM departments", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Department { Id = r.GetInt32("id"), Name = r.GetString("name") });
            }
            return list;
        }

        private static List<Doctor> LoadDoctors(MySqlConnection conn)
        {
            var list = new List<Doctor>();
            using (var cmd = new MySqlCommand("SELECT id, full_name, department_id, specialization, contact, is_on_duty, status FROM doctors", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Doctor
                    {
                        Id = r.GetInt32("id"),
                        FullName = r.GetString("full_name"),
                        DepartmentId = r.GetInt32("department_id"),
                        Specialization = r.IsDBNull(r.GetOrdinal("specialization")) ? null : r.GetString("specialization"),
                        Contact = r.IsDBNull(r.GetOrdinal("contact")) ? null : r.GetString("contact"),
                        IsOnDuty = r.GetBoolean("is_on_duty"),
                        Status = r.GetString("status")
                    });
                }
            }
            return list;
        }

        private static List<Bed> LoadBeds(MySqlConnection conn)
        {
            var list = new List<Bed>();
            using (var cmd = new MySqlCommand("SELECT id, room_no, bed_no, ward, is_occupied FROM beds", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Bed
                    {
                        Id = r.GetInt32("id"),
                        RoomNo = r.GetString("room_no"),
                        BedNo = r.GetString("bed_no"),
                        Ward = r.GetString("ward"),
                        IsOccupied = r.GetBoolean("is_occupied")
                    });
                }
            }
            return list;
        }

        private static List<Patient> LoadPatients(MySqlConnection conn)
        {
            var list = new List<Patient>();
            using (var cmd = new MySqlCommand("SELECT id, full_name, age, gender, contact, address, blood_type, status, registered_on FROM patients", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Patient
                    {
                        Id = r.GetInt32("id"),
                        FullName = r.GetString("full_name"),
                        Age = r.GetInt32("age"),
                        Gender = r.IsDBNull(r.GetOrdinal("gender")) ? null : r.GetString("gender"),
                        Contact = r.IsDBNull(r.GetOrdinal("contact")) ? null : r.GetString("contact"),
                        Address = r.IsDBNull(r.GetOrdinal("address")) ? null : r.GetString("address"),
                        BloodType = r.IsDBNull(r.GetOrdinal("blood_type")) ? null : r.GetString("blood_type"),
                        Status = r.GetString("status"),
                        RegisteredOn = r.GetDateTime("registered_on")
                    });
                }
            }
            return list;
        }

        private static List<Appointment> LoadAppointments(MySqlConnection conn)
        {
            var list = new List<Appointment>();
            using (var cmd = new MySqlCommand("SELECT id, patient_id, doctor_id, department_id, scheduled_on, reason, status FROM appointments", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Appointment
                    {
                        Id = r.GetInt32("id"),
                        PatientId = r.GetInt32("patient_id"),
                        DoctorId = r.GetInt32("doctor_id"),
                        DepartmentId = r.GetInt32("department_id"),
                        ScheduledOn = r.GetDateTime("scheduled_on"),
                        Reason = r.IsDBNull(r.GetOrdinal("reason")) ? null : r.GetString("reason"),
                        Status = r.GetString("status")
                    });
                }
            }
            return list;
        }

        private static List<Admission> LoadAdmissions(MySqlConnection conn)
        {
            var list = new List<Admission>();
            using (var cmd = new MySqlCommand("SELECT id, patient_id, doctor_id, bed_id, admitted_on, discharged_on, diagnosis, notes, status FROM admissions", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Admission
                    {
                        Id = r.GetInt32("id"),
                        PatientId = r.GetInt32("patient_id"),
                        DoctorId = r.GetInt32("doctor_id"),
                        BedId = r.GetInt32("bed_id"),
                        AdmittedOn = r.GetDateTime("admitted_on"),
                        DischargedOn = r.IsDBNull(r.GetOrdinal("discharged_on")) ? (DateTime?)null : r.GetDateTime("discharged_on"),
                        Diagnosis = r.IsDBNull(r.GetOrdinal("diagnosis")) ? null : r.GetString("diagnosis"),
                        Notes = r.IsDBNull(r.GetOrdinal("notes")) ? null : r.GetString("notes"),
                        Status = r.GetString("status")
                    });
                }
            }
            return list;
        }

        private static List<Alert> LoadAlerts(MySqlConnection conn)
        {
            var list = new List<Alert>();
            using (var cmd = new MySqlCommand("SELECT id, title, message, severity, created_on FROM alerts", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Alert
                    {
                        Id = r.GetInt32("id"),
                        Title = r.GetString("title"),
                        Message = r.GetString("message"),
                        Severity = r.GetString("severity"),
                        CreatedOn = r.GetDateTime("created_on")
                    });
                }
            }
            return list;
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
            if (p.RegisteredOn == default)
                p.RegisteredOn = DateTime.Now;
            p.Status = "Active";

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO patients (full_name, age, gender, contact, address, blood_type, status, registered_on) " +
                "VALUES (@fullName, @age, @gender, @contact, @address, @bloodType, @status, @registeredOn); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@fullName", p.FullName);
                cmd.Parameters.AddWithValue("@age", p.Age);
                cmd.Parameters.AddWithValue("@gender", (object)p.Gender ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@contact", (object)p.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@address", (object)p.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bloodType", (object)p.BloodType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", p.Status);
                cmd.Parameters.AddWithValue("@registeredOn", p.RegisteredOn);
                p.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Patients.Add(p);
            return p;
        }

        public static void UpdatePatient(Patient p)
        {
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE patients SET full_name=@fullName, age=@age, gender=@gender, contact=@contact, " +
                "address=@address, blood_type=@bloodType WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@fullName", p.FullName);
                cmd.Parameters.AddWithValue("@age", p.Age);
                cmd.Parameters.AddWithValue("@gender", (object)p.Gender ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@contact", (object)p.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@address", (object)p.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bloodType", (object)p.BloodType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        // Soft-delete only: patients are referenced by appointments/admissions (FK),
        // and a hospital record should stay auditable rather than disappear.
        public static void DeletePatient(Patient p)
        {
            p.Status = "Inactive";
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE patients SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", p.Status);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public static Patient GetPatient(int id) => Patients.FirstOrDefault(x => x.Id == id);

        public static string PatientName(int id)
        {
            var p = GetPatient(id);
            return p != null ? p.FullName : "(Unknown)";
        }

        public static List<Patient> ActivePatients() => Patients.Where(p => p.IsActive).ToList();

        public static int AppointmentCountForPatient(int patientId) =>
            Appointments.Count(a => a.PatientId == patientId);

        public static int AdmissionCountForPatient(int patientId) =>
            Admissions.Count(a => a.PatientId == patientId);

        public static bool HasActiveAdmission(int patientId) =>
            Admissions.Any(a => a.PatientId == patientId && a.Status == "Active");

        // -------------------- Appointments --------------------
        public static Appointment AddAppointment(Appointment a)
        {
            if (string.IsNullOrEmpty(a.Status))
                a.Status = "Pending";

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO appointments (patient_id, doctor_id, department_id, scheduled_on, reason, status) " +
                "VALUES (@patientId, @doctorId, @departmentId, @scheduledOn, @reason, @status); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@patientId", a.PatientId);
                cmd.Parameters.AddWithValue("@doctorId", a.DoctorId);
                cmd.Parameters.AddWithValue("@departmentId", a.DepartmentId);
                cmd.Parameters.AddWithValue("@scheduledOn", a.ScheduledOn);
                cmd.Parameters.AddWithValue("@reason", (object)a.Reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", a.Status);
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Appointments.Add(a);
            return a;
        }

        public static void UpdateAppointmentStatus(Appointment a, string status)
        {
            a.Status = status;
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE appointments SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
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
            a.AdmittedOn = DateTime.Now;
            a.Status = "Active";

            using (var conn = Db.OpenConnection())
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO admissions (patient_id, doctor_id, bed_id, admitted_on, discharged_on, diagnosis, notes, status) " +
                    "VALUES (@patientId, @doctorId, @bedId, @admittedOn, NULL, @diagnosis, @notes, @status); " +
                    "SELECT LAST_INSERT_ID();", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", a.PatientId);
                    cmd.Parameters.AddWithValue("@doctorId", a.DoctorId);
                    cmd.Parameters.AddWithValue("@bedId", a.BedId);
                    cmd.Parameters.AddWithValue("@admittedOn", a.AdmittedOn);
                    cmd.Parameters.AddWithValue("@diagnosis", (object)a.Diagnosis ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object)a.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", a.Status);
                    a.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new MySqlCommand("UPDATE beds SET is_occupied=1 WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", a.BedId);
                    cmd.ExecuteNonQuery();
                }
            }

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

            using (var conn = Db.OpenConnection())
            {
                using (var cmd = new MySqlCommand(
                    "UPDATE admissions SET discharged_on=@dischargedOn, status=@status WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@dischargedOn", a.DischargedOn);
                    cmd.Parameters.AddWithValue("@status", a.Status);
                    cmd.Parameters.AddWithValue("@id", a.Id);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new MySqlCommand("UPDATE beds SET is_occupied=0 WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", a.BedId);
                    cmd.ExecuteNonQuery();
                }
            }

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

        public static List<Doctor> ActiveDoctors() => Doctors.Where(d => d.IsActive).ToList();

        public static Doctor AddDoctor(Doctor d)
        {
            d.Status = "Active";

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO doctors (full_name, department_id, specialization, contact, is_on_duty, status) " +
                "VALUES (@fullName, @departmentId, @specialization, @contact, @isOnDuty, @status); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@fullName", d.FullName);
                cmd.Parameters.AddWithValue("@departmentId", d.DepartmentId);
                cmd.Parameters.AddWithValue("@specialization", (object)d.Specialization ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@contact", (object)d.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isOnDuty", d.IsOnDuty);
                cmd.Parameters.AddWithValue("@status", d.Status);
                d.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Doctors.Add(d);
            return d;
        }

        public static void UpdateDoctor(Doctor d)
        {
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE doctors SET full_name=@fullName, department_id=@departmentId, specialization=@specialization, " +
                "contact=@contact, is_on_duty=@isOnDuty WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@fullName", d.FullName);
                cmd.Parameters.AddWithValue("@departmentId", d.DepartmentId);
                cmd.Parameters.AddWithValue("@specialization", (object)d.Specialization ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@contact", (object)d.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isOnDuty", d.IsOnDuty);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }
        }

        // Soft-delete only: doctors are referenced by appointments/admissions (FK),
        // and losing a doctor's record should never erase that history.
        public static void DeleteDoctor(Doctor d)
        {
            d.Status = "Inactive";
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE doctors SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", d.Status);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public static int AppointmentCountForDoctor(int doctorId) =>
            Appointments.Count(a => a.DoctorId == doctorId);

        public static int AdmissionCountForDoctor(int doctorId) =>
            Admissions.Count(a => a.DoctorId == doctorId);

        public static int UpcomingAppointmentCountForDoctor(int doctorId) =>
            Appointments.Count(a => a.DoctorId == doctorId && a.Status != "Cancelled" && a.ScheduledOn >= DateTime.Now);

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

        public static List<Doctor> DoctorsOnDuty() => Doctors.Where(d => d.IsOnDuty && d.IsActive).ToList();

        public static List<Alert> ActiveAlerts() => Alerts.OrderByDescending(a => a.CreatedOn).ToList();
    }
}

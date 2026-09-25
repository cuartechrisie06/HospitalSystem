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
        public static List<Bill> Bills { get; private set; } = new List<Bill>();
        public static List<Alert> Alerts { get; private set; } = new List<Alert>();
        public static List<ActivityItem> Activities { get; private set; } = new List<ActivityItem>();
        public static HashSet<int> DismissedAutoAlertIds { get; } = new HashSet<int>();

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
                EnsureBillingTables(conn);
                Bills = LoadBills(conn);
                Alerts = LoadAlerts(conn);
                Activities = LoadActivities(conn);
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

        // Keep in sync with Data/schema.sql. Creating them here means a database
        // imported before billing existed picks the tables up without a re-import.
        private static void EnsureBillingTables(MySqlConnection conn)
        {
            string[] ddl =
            {
                "CREATE TABLE IF NOT EXISTS bills (" +
                "  id INT PRIMARY KEY AUTO_INCREMENT," +
                "  patient_id INT NOT NULL," +
                "  admission_id INT NULL," +
                "  appointment_id INT NULL," +
                "  bill_date DATETIME NOT NULL," +
                "  total_amount DECIMAL(12,2) NOT NULL DEFAULT 0," +
                "  amount_paid DECIMAL(12,2) NOT NULL DEFAULT 0," +
                "  balance DECIMAL(12,2) NOT NULL DEFAULT 0," +
                "  status VARCHAR(20) NOT NULL DEFAULT 'Unpaid'," +
                "  notes VARCHAR(500)," +
                "  created_by VARCHAR(50)," +
                "  created_at DATETIME NOT NULL," +
                "  FOREIGN KEY (patient_id) REFERENCES patients(id)," +
                "  FOREIGN KEY (admission_id) REFERENCES admissions(id)," +
                "  FOREIGN KEY (appointment_id) REFERENCES appointments(id))",

                "CREATE TABLE IF NOT EXISTS bill_items (" +
                "  id INT PRIMARY KEY AUTO_INCREMENT," +
                "  bill_id INT NOT NULL," +
                "  description VARCHAR(255) NOT NULL," +
                "  category VARCHAR(20) NOT NULL," +
                "  quantity INT NOT NULL DEFAULT 1," +
                "  unit_price DECIMAL(12,2) NOT NULL," +
                "  amount DECIMAL(12,2) NOT NULL," +
                "  FOREIGN KEY (bill_id) REFERENCES bills(id) ON DELETE CASCADE)",

                "CREATE TABLE IF NOT EXISTS payments (" +
                "  id INT PRIMARY KEY AUTO_INCREMENT," +
                "  bill_id INT NOT NULL," +
                "  amount DECIMAL(12,2) NOT NULL," +
                "  payment_method VARCHAR(20) NOT NULL," +
                "  payment_date DATETIME NOT NULL," +
                "  reference_no VARCHAR(100)," +
                "  received_by VARCHAR(100)," +
                "  FOREIGN KEY (bill_id) REFERENCES bills(id))"
            };

            foreach (var sql in ddl)
            {
                using (var cmd = new MySqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static List<Bill> LoadBills(MySqlConnection conn)
        {
            var list = new List<Bill>();
            using (var cmd = new MySqlCommand(
                "SELECT id, patient_id, admission_id, appointment_id, bill_date, total_amount, amount_paid, " +
                "balance, status, notes, created_by, created_at FROM bills", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    BillStatus status;
                    Enum.TryParse(r.GetString("status"), out status);

                    list.Add(new Bill
                    {
                        Id = r.GetInt32("id"),
                        PatientId = r.GetInt32("patient_id"),
                        AdmissionId = r.IsDBNull(r.GetOrdinal("admission_id")) ? (int?)null : r.GetInt32("admission_id"),
                        AppointmentId = r.IsDBNull(r.GetOrdinal("appointment_id")) ? (int?)null : r.GetInt32("appointment_id"),
                        BillDate = r.GetDateTime("bill_date"),
                        TotalAmount = r.GetDecimal("total_amount"),
                        AmountPaid = r.GetDecimal("amount_paid"),
                        Balance = r.GetDecimal("balance"),
                        Status = status,
                        Notes = r.IsDBNull(r.GetOrdinal("notes")) ? null : r.GetString("notes"),
                        CreatedBy = r.IsDBNull(r.GetOrdinal("created_by")) ? null : r.GetString("created_by"),
                        CreatedAt = r.GetDateTime("created_at")
                    });
                }
            }

            var byId = list.ToDictionary(b => b.Id);

            using (var cmd = new MySqlCommand(
                "SELECT id, bill_id, description, category, quantity, unit_price, amount FROM bill_items", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    Bill bill;
                    if (!byId.TryGetValue(r.GetInt32("bill_id"), out bill)) continue;

                    BillCategory category;
                    if (!Enum.TryParse(r.GetString("category"), out category))
                        category = BillCategory.Other;

                    bill.Items.Add(new BillItem
                    {
                        Id = r.GetInt32("id"),
                        BillId = bill.Id,
                        Description = r.GetString("description"),
                        Category = category,
                        Quantity = r.GetInt32("quantity"),
                        UnitPrice = r.GetDecimal("unit_price"),
                        Amount = r.GetDecimal("amount")
                    });
                }
            }

            using (var cmd = new MySqlCommand(
                "SELECT id, bill_id, amount, payment_method, payment_date, reference_no, received_by FROM payments", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    Bill bill;
                    if (!byId.TryGetValue(r.GetInt32("bill_id"), out bill)) continue;

                    PaymentMethod method;
                    Enum.TryParse(r.GetString("payment_method"), out method);

                    bill.Payments.Add(new Payment
                    {
                        Id = r.GetInt32("id"),
                        BillId = bill.Id,
                        Amount = r.GetDecimal("amount"),
                        Method = method,
                        PaymentDate = r.GetDateTime("payment_date"),
                        ReferenceNo = r.IsDBNull(r.GetOrdinal("reference_no")) ? null : r.GetString("reference_no"),
                        ReceivedBy = r.IsDBNull(r.GetOrdinal("received_by")) ? null : r.GetString("received_by")
                    });
                }
            }

            return list;
        }

        private static List<Alert> LoadAlerts(MySqlConnection conn)
        {
            var list = new List<Alert>();
            try
            {
                using (var cmd = new MySqlCommand("SELECT id, title, message, severity, status, created_on FROM alerts WHERE status = 'Active'", conn))
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
                            Status = r.IsDBNull(r.GetOrdinal("status")) ? "Active" : r.GetString("status"),
                            CreatedOn = r.GetDateTime("created_on"),
                            IsAuto = false
                        });
                    }
                }
            }
            catch
            {
                // Fallback for schema versions without status column
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
                            Status = "Active",
                            CreatedOn = r.GetDateTime("created_on"),
                            IsAuto = false
                        });
                    }
                }
            }
            return list;
        }

        private static List<ActivityItem> LoadActivities(MySqlConnection conn)
        {
            var list = new List<ActivityItem>();
            try
            {
                using (var cmd = new MySqlCommand("SELECT id, module, action, description, icon, created_at FROM activity_log ORDER BY created_at DESC LIMIT 50", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ActivityItem
                        {
                            Id = r.GetInt32("id"),
                            Module = r.GetString("module"),
                            Action = r.GetString("action"),
                            Description = r.GetString("description"),
                            Icon = r.IsDBNull(r.GetOrdinal("icon")) ? "" : r.GetString("icon"),
                            Timestamp = r.GetDateTime("created_at")
                        });
                    }
                }
            }
            catch
            {
                // activity_log table might not exist yet
            }

            if (list.Count == 0)
            {
                SeedInitialActivities(conn, list);
            }

            return list;
        }

        private static void SeedInitialActivities(MySqlConnection conn, List<ActivityItem> list)
        {
            var initial = new List<ActivityItem>();

            foreach (var p in Patients)
            {
                initial.Add(new ActivityItem
                {
                    Module = "Patients",
                    Action = "Registered",
                    Description = $"New patient registered: {p.FullName} ({p.PatientNo})",
                    Icon = "👤",
                    Timestamp = p.RegisteredOn
                });
            }

            foreach (var d in Doctors.Where(doc => doc.IsActive))
            {
                initial.Add(new ActivityItem
                {
                    Module = "Doctors",
                    Action = "Duty",
                    Description = d.IsOnDuty
                        ? $"Dr. {d.FullName} clocked On Duty ({DepartmentName(d.DepartmentId)})"
                        : $"Dr. {d.FullName} added to staff ({DepartmentName(d.DepartmentId)})",
                    Icon = "🩺",
                    Timestamp = DateTime.Now.AddHours(-2)
                });
            }

            foreach (var a in Appointments)
            {
                initial.Add(new ActivityItem
                {
                    Module = "Appointments",
                    Action = "Scheduled",
                    Description = $"Appointment scheduled: {PatientName(a.PatientId)} with {DoctorName(a.DoctorId)}",
                    Icon = "📅",
                    Timestamp = a.ScheduledOn.AddDays(-1)
                });
            }

            foreach (var adm in Admissions)
            {
                initial.Add(new ActivityItem
                {
                    Module = "Admissions",
                    Action = "Admitted",
                    Description = $"Admitted: {PatientName(adm.PatientId)} to {BedLabel(adm.BedId)}",
                    Icon = "🏥",
                    Timestamp = adm.AdmittedOn
                });
                if (adm.DischargedOn.HasValue)
                {
                    initial.Add(new ActivityItem
                    {
                        Module = "Admissions",
                        Action = "Discharged",
                        Description = $"Discharged: {PatientName(adm.PatientId)} from {BedLabel(adm.BedId)}",
                        Icon = "🚪",
                        Timestamp = adm.DischargedOn.Value
                    });
                }
            }

            var sorted = initial.OrderByDescending(x => x.Timestamp).Take(25).ToList();
            foreach (var item in sorted)
            {
                try
                {
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO activity_log (module, action, description, icon, created_at) " +
                        "VALUES (@module, @action, @description, @icon, @createdAt); SELECT LAST_INSERT_ID();", conn))
                    {
                        cmd.Parameters.AddWithValue("@module", item.Module);
                        cmd.Parameters.AddWithValue("@action", item.Action);
                        cmd.Parameters.AddWithValue("@description", item.Description);
                        cmd.Parameters.AddWithValue("@icon", item.Icon ?? "");
                        cmd.Parameters.AddWithValue("@createdAt", item.Timestamp);
                        item.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch { }
                list.Add(item);
            }
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
            LogActivity("Patients", "Registered", $"New patient registered: {p.FullName} ({p.PatientNo})", "👤");
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

            LogActivity("Patients", "Updated", $"Updated patient record: {p.FullName} ({p.PatientNo})", "👤");
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

            LogActivity("Patients", "Deactivated", $"Deactivated patient: {p.FullName} ({p.PatientNo})", "👤");
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
            LogActivity("Appointments", "Scheduled", $"Appointment scheduled: {PatientName(a.PatientId)} with {DoctorName(a.DoctorId)}", "📅");
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

            string icon = status == "Confirmed" ? "✅" : status == "Cancelled" ? "❌" : "📅";
            LogActivity("Appointments", status, $"{status} appointment for {PatientName(a.PatientId)}", icon);
        }

        public static void CancelAppointment(Appointment a)
        {
            a.Cancel();
            UpdateAppointmentStatus(a, a.Status);
        }

        public static void CompleteAppointment(Appointment a)
        {
            a.MarkAsCompleted();
            UpdateAppointmentStatus(a, a.Status);
        }

        public static void RescheduleAppointment(Appointment a, DateTime newDate)
        {
            DateTime oldDate = a.ScheduledOn;
            a.Reschedule(newDate);

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE appointments SET scheduled_on=@scheduledOn WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@scheduledOn", a.ScheduledOn);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Appointments", "Rescheduled",
                $"Rescheduled appointment for {PatientName(a.PatientId)}: {oldDate:yyyy-MM-dd HH:mm} → {a.ScheduledOn:yyyy-MM-dd HH:mm}", "🔁");
        }

        // Same 30-minute window the Schedule button uses; ignoreId lets a reschedule skip itself.
        public static bool HasAppointmentClash(int doctorId, DateTime when, int ignoreId = 0)
        {
            return Appointments.Any(a =>
                a.Id != ignoreId &&
                a.DoctorId == doctorId &&
                a.Status != "Cancelled" &&
                Math.Abs((a.ScheduledOn - when).TotalMinutes) < 30);
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

            LogActivity("Admissions", "Admitted", $"Admitted: {PatientName(a.PatientId)} to {BedLabel(a.BedId)}", "🏥");
            return a;
        }

        public static void Discharge(Admission a)
        {
            if (a == null) return;
            a.Discharge(DateTime.Now);

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

            LogActivity("Admissions", "Discharged", $"Discharged patient: {PatientName(a.PatientId)} from {BedLabel(a.BedId)}", "🚪");
        }

        public static void CancelAdmission(Admission a)
        {
            a.Cancel();

            using (var conn = Db.OpenConnection())
            {
                using (var cmd = new MySqlCommand("UPDATE admissions SET status=@status WHERE id=@id", conn))
                {
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

            LogActivity("Admissions", "Cancelled", $"Cancelled admission {a.AdmissionNo} for {PatientName(a.PatientId)}", "❌");
        }

        public static List<Admission> ActiveAdmissions() => Admissions.Where(a => a.Status == "Active").ToList();

        // -------------------- Billing --------------------
        public const decimal ConsultationFee = 500m;

        // Daily room rate by ward, used when generating a bill from an admission.
        public static decimal RoomRate(string ward)
        {
            switch (ward)
            {
                case "ICU": return 8000m;
                case "Private": return 3000m;
                default: return 1500m;
            }
        }

        public static Bill GetBill(int id) => Bills.FirstOrDefault(b => b.Id == id);

        public static Bill OpenBillForAdmission(int admissionId) =>
            Bills.FirstOrDefault(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled);

        public static Bill OpenBillForAppointment(int appointmentId) =>
            Bills.FirstOrDefault(b => b.AppointmentId == appointmentId && b.Status != BillStatus.Cancelled);

        public static decimal OutstandingBalance() =>
            Bills.Where(b => b.Status != BillStatus.Cancelled).Sum(b => b.Balance);

        // Starting line items for a bill: room charges for the stay, or the consultation fee.
        public static List<BillItem> DefaultChargesFor(Admission admission, Appointment appointment)
        {
            var items = new List<BillItem>();

            if (admission != null)
            {
                var bed = GetBed(admission.BedId);
                items.Add(new BillItem
                {
                    Description = "Room charge - " + BedLabel(admission.BedId),
                    Category = BillCategory.Room,
                    Quantity = admission.CalculateDaysStayed(),
                    UnitPrice = RoomRate(bed != null ? bed.Ward : null)
                });
            }

            if (appointment != null)
            {
                items.Add(new BillItem
                {
                    Description = "Consultation - " + DoctorName(appointment.DoctorId),
                    Category = BillCategory.Consultation,
                    Quantity = 1,
                    UnitPrice = ConsultationFee
                });
            }

            return items;
        }

        public static Bill CreateBill(Bill b, IEnumerable<BillItem> items)
        {
            b.BillDate = b.BillDate == default ? DateTime.Now : b.BillDate;
            b.CreatedAt = DateTime.Now;
            b.CreatedBy = CurrentUser != null ? CurrentUser.Username : null;
            b.Status = BillStatus.Unpaid;
            foreach (var item in items)
                b.AddItem(item);

            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO bills (patient_id, admission_id, appointment_id, bill_date, total_amount, amount_paid, " +
                    "balance, status, notes, created_by, created_at) " +
                    "VALUES (@patientId, @admissionId, @appointmentId, @billDate, @total, @paid, @balance, @status, " +
                    "@notes, @createdBy, @createdAt); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@patientId", b.PatientId);
                    cmd.Parameters.AddWithValue("@admissionId", (object)b.AdmissionId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@appointmentId", (object)b.AppointmentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@billDate", b.BillDate);
                    cmd.Parameters.AddWithValue("@total", b.TotalAmount);
                    cmd.Parameters.AddWithValue("@paid", b.AmountPaid);
                    cmd.Parameters.AddWithValue("@balance", b.Balance);
                    cmd.Parameters.AddWithValue("@status", b.Status.ToString());
                    cmd.Parameters.AddWithValue("@notes", (object)b.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdBy", (object)b.CreatedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdAt", b.CreatedAt);
                    b.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var item in b.Items)
                {
                    item.BillId = b.Id;
                    InsertBillItem(item, conn, tx);
                }

                tx.Commit();
            }

            Bills.Add(b);
            LogActivity("Billing", "Created", $"Bill {b.BillNo} created for {PatientName(b.PatientId)} ({b.TotalAmount:N2})", "🧾");
            return b;
        }

        public static void AddBillItem(Bill b, BillItem item)
        {
            b.AddItem(item);

            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                InsertBillItem(item, conn, tx);
                SaveBillTotals(b, conn, tx);
                tx.Commit();
            }

            LogActivity("Billing", "Item Added", $"Added \"{item.Description}\" to {b.BillNo} ({item.Amount:N2})", "🧾");
        }

        public static void RemoveBillItem(Bill b, int itemId)
        {
            var item = b.Items.FirstOrDefault(i => i.Id == itemId);
            b.RemoveItem(itemId);

            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = new MySqlCommand("DELETE FROM bill_items WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    cmd.ExecuteNonQuery();
                }
                SaveBillTotals(b, conn, tx);
                tx.Commit();
            }

            LogActivity("Billing", "Item Removed", $"Removed \"{item.Description}\" from {b.BillNo}", "🧾");
        }

        public static void RecordPayment(Bill b, Payment p)
        {
            if (p.PaymentDate == default)
                p.PaymentDate = DateTime.Now;
            p.ReceivedBy = CurrentUser != null ? CurrentUser.DisplayName : null;
            b.ApplyPayment(p);

            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO payments (bill_id, amount, payment_method, payment_date, reference_no, received_by) " +
                    "VALUES (@billId, @amount, @method, @date, @ref, @receivedBy); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@billId", p.BillId);
                    cmd.Parameters.AddWithValue("@amount", p.Amount);
                    cmd.Parameters.AddWithValue("@method", p.Method.ToString());
                    cmd.Parameters.AddWithValue("@date", p.PaymentDate);
                    cmd.Parameters.AddWithValue("@ref", (object)p.ReferenceNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@receivedBy", (object)p.ReceivedBy ?? DBNull.Value);
                    p.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
                SaveBillTotals(b, conn, tx);
                tx.Commit();
            }

            LogActivity("Billing", "Payment", $"Payment of {p.Amount:N2} ({p.Method}) received for {b.BillNo}", "💰");
        }

        public static void CancelBill(Bill b)
        {
            b.MarkAsCancelled();

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE bills SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", b.Status.ToString());
                cmd.Parameters.AddWithValue("@id", b.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Billing", "Cancelled", $"Cancelled bill {b.BillNo} for {PatientName(b.PatientId)}", "❌");
        }

        private static void InsertBillItem(BillItem item, MySqlConnection conn, MySqlTransaction tx)
        {
            using (var cmd = new MySqlCommand(
                "INSERT INTO bill_items (bill_id, description, category, quantity, unit_price, amount) " +
                "VALUES (@billId, @description, @category, @quantity, @unitPrice, @amount); SELECT LAST_INSERT_ID();", conn, tx))
            {
                cmd.Parameters.AddWithValue("@billId", item.BillId);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@category", item.Category.ToString());
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("@amount", item.Amount);
                item.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void SaveBillTotals(Bill b, MySqlConnection conn, MySqlTransaction tx)
        {
            using (var cmd = new MySqlCommand(
                "UPDATE bills SET total_amount=@total, amount_paid=@paid, balance=@balance, status=@status WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@total", b.TotalAmount);
                cmd.Parameters.AddWithValue("@paid", b.AmountPaid);
                cmd.Parameters.AddWithValue("@balance", b.Balance);
                cmd.Parameters.AddWithValue("@status", b.Status.ToString());
                cmd.Parameters.AddWithValue("@id", b.Id);
                cmd.ExecuteNonQuery();
            }
        }

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
            LogActivity("Doctors", "Added", $"New doctor added: Dr. {d.FullName} ({DepartmentName(d.DepartmentId)})", "🩺");
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

            LogActivity("Doctors", "Updated", $"Dr. {d.FullName} updated ({(d.IsOnDuty ? "On Duty" : "Off Duty")})", "🩺");
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

            LogActivity("Doctors", "Deactivated", $"Deactivated doctor: Dr. {d.FullName}", "🩺");
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

        // -------------------- Activities & Alerts Helpers --------------------
        public static void LogActivity(string module, string action, string description, string icon)
        {
            var item = new ActivityItem
            {
                Module = module,
                Action = action,
                Description = description,
                Icon = icon,
                Timestamp = DateTime.Now
            };

            try
            {
                using (var conn = Db.OpenConnection())
                using (var cmd = new MySqlCommand(
                    "INSERT INTO activity_log (module, action, description, icon, created_at) " +
                    "VALUES (@module, @action, @description, @icon, @createdAt); SELECT LAST_INSERT_ID();", conn))
                {
                    cmd.Parameters.AddWithValue("@module", item.Module);
                    cmd.Parameters.AddWithValue("@action", item.Action);
                    cmd.Parameters.AddWithValue("@description", item.Description);
                    cmd.Parameters.AddWithValue("@icon", (object)item.Icon ?? "");
                    cmd.Parameters.AddWithValue("@createdAt", item.Timestamp);
                    item.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch { }

            Activities.Insert(0, item);
            if (Activities.Count > 100)
                Activities.RemoveAt(Activities.Count - 1);
        }

        public static Alert AddAlert(Alert a)
        {
            if (a.CreatedOn == default)
                a.CreatedOn = DateTime.Now;
            a.Status = "Active";

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO alerts (title, message, severity, status, created_on) " +
                "VALUES (@title, @message, @severity, @status, @createdOn); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@title", a.Title);
                cmd.Parameters.AddWithValue("@message", a.Message);
                cmd.Parameters.AddWithValue("@severity", a.Severity);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@createdOn", a.CreatedOn);
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Alerts.Add(a);
            LogActivity("Alerts", "Created", $"Emergency alert posted: {a.Title}", "⚠️");
            return a;
        }

        public static void ResolveAlert(int alertId)
        {
            if (alertId < 0)
            {
                DismissedAutoAlertIds.Add(alertId);
                LogActivity("Alerts", "Resolved", "Auto alert acknowledged/dismissed", "✅");
                return;
            }

            var a = Alerts.FirstOrDefault(x => x.Id == alertId);
            if (a != null)
            {
                a.Status = "Resolved";
                Alerts.Remove(a);
                try
                {
                    using (var conn = Db.OpenConnection())
                    using (var cmd = new MySqlCommand("UPDATE alerts SET status='Resolved' WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", alertId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }

                LogActivity("Alerts", "Resolved", $"Resolved alert: {a.Title}", "✅");
            }
        }

        public static List<Alert> ActiveAlerts()
        {
            var result = new List<Alert>();

            // 1. Dynamic auto-detected condition: ICU Bed Capacity
            var icuBeds = Beds.Where(b => b.Ward == "ICU").ToList();
            if (icuBeds.Count > 0)
            {
                int availableIcu = icuBeds.Count(b => !b.IsOccupied);
                int autoIcuId = -100;
                if (availableIcu <= 1 && !DismissedAutoAlertIds.Contains(autoIcuId))
                {
                    result.Add(new Alert
                    {
                        Id = autoIcuId,
                        Title = availableIcu == 0 ? "ICU Bed Full" : "ICU Bed Critical",
                        Message = availableIcu == 0 ? "All ICU beds are currently occupied" : $"Only {availableIcu} of {icuBeds.Count} ICU beds remaining",
                        Severity = "High",
                        CreatedOn = DateTime.Now,
                        IsAuto = true
                    });
                }
            }

            // 2. Dynamic auto-detected condition: Staff Shortage per department
            foreach (var dept in Departments)
            {
                int deptId = dept.Id;
                int autoDeptId = -200 - deptId;
                var deptDoctors = Doctors.Where(d => d.DepartmentId == deptId && d.IsActive).ToList();
                if (deptDoctors.Count > 0 && !deptDoctors.Any(d => d.IsOnDuty) && !DismissedAutoAlertIds.Contains(autoDeptId))
                {
                    result.Add(new Alert
                    {
                        Id = autoDeptId,
                        Title = "Staff Shortage",
                        Message = $"{dept.Name} has no doctor currently on duty",
                        Severity = "Medium",
                        CreatedOn = DateTime.Now,
                        IsAuto = true
                    });
                }
            }

            // 3. Dynamic auto-detected condition: High Overall Bed Occupancy (>80%)
            int totalBeds = Beds.Count;
            int occupiedBeds = Beds.Count(b => b.IsOccupied);
            int autoOccupancyId = -300;
            if (totalBeds > 0 && ((double)occupiedBeds / totalBeds) >= 0.8 && !DismissedAutoAlertIds.Contains(autoOccupancyId))
            {
                result.Add(new Alert
                {
                    Id = autoOccupancyId,
                    Title = "High Bed Occupancy",
                    Message = $"{occupiedBeds} of {totalBeds} beds occupied ({Math.Round((double)occupiedBeds / totalBeds * 100)}%)",
                    Severity = "Medium",
                    CreatedOn = DateTime.Now,
                    IsAuto = true
                });
            }

            // 4. Active manual alerts
            result.AddRange(Alerts.Where(a => a.Status == "Active"));

            // Sort by Severity (High first, then Medium, then Low), then by CreatedOn
            return result
                .OrderBy(a => a.Severity == "High" ? 0 : a.Severity == "Medium" ? 1 : 2)
                .ThenByDescending(a => a.CreatedOn)
                .ToList();
        }
    }
}

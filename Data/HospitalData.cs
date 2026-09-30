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
        public static List<ChargeSchedule> ChargeSchedules { get; private set; } = new List<ChargeSchedule>();
        public static List<Alert> Alerts { get; private set; } = new List<Alert>();   // Active + Acknowledged

        public static User CurrentUser { get; set; }

        public static bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;

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
                EnsureDoctorColumns(conn);
                Doctors = LoadDoctors(conn);
                Beds = LoadBeds(conn);
                Patients = LoadPatients(conn);
                Appointments = LoadAppointments(conn);
                EnsureWorkflowColumns(conn);
                Admissions = LoadAdmissions(conn);
                EnsureBillingTables(conn);
                Bills = LoadBills(conn);
                ChargeSchedules = LoadChargeSchedules(conn);
                Alerts = LoadAlerts(conn);
            }

            // Conditions may have changed while the app was closed (e.g. appointments now overdue).
            AlertMonitor.Evaluate();
        }

        // Keep in sync with Data/schema.sql: upgrades an older database in place for the
        // waiting list (admissions without a bed), alert triggers and the activity log's user.
        private static void EnsureWorkflowColumns(MySqlConnection conn)
        {
            AddColumnIfMissing(conn, "admissions", "requested_on", "requested_on DATETIME NULL");
            using (var cmd = new MySqlCommand(
                "SELECT IS_NULLABLE FROM information_schema.columns " +
                "WHERE table_schema = DATABASE() AND table_name = 'admissions' AND column_name = 'bed_id'", conn))
            {
                if ((cmd.ExecuteScalar() as string) == "NO")
                {
                    using (var alter = new MySqlCommand("ALTER TABLE admissions MODIFY bed_id INT NULL", conn))
                        alter.ExecuteNonQuery();
                }
            }

            AddColumnIfMissing(conn, "alerts", "source_key", "source_key VARCHAR(100) NULL");
            AddColumnIfMissing(conn, "alerts", "module", "module VARCHAR(50) NULL");
            AddColumnIfMissing(conn, "alerts", "acknowledged_by", "acknowledged_by VARCHAR(100) NULL");
            AddColumnIfMissing(conn, "alerts", "resolved_on", "resolved_on DATETIME NULL");

            // Alerts are no longer posted by hand. Close any still open from before (they have no
            // trigger, so nothing would ever clear them); the rows stay in the table as history.
            int retired;
            using (var cmd = new MySqlCommand(
                "UPDATE alerts SET status='Resolved', resolved_on=NOW(), acknowledged_by=@by " +
                "WHERE source_key IS NULL AND status IN ('Active', 'Acknowledged')", conn))
            {
                cmd.Parameters.AddWithValue("@by", SystemUser);
                retired = cmd.ExecuteNonQuery();
            }

            AddColumnIfMissing(conn, "activity_log", "username", "username VARCHAR(50) NULL");

            if (retired > 0)
                LogActivity(AlertMonitor.ModuleName, "Retired",
                    $"Closed {retired} manually posted alert(s): alerts are now raised and cleared automatically", "🧹", SystemUser);
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

        // Keep in sync with Data/schema.sql. Adding the columns here means a database
        // imported before the doctor record fields existed picks them up without a
        // re-import. Works on both MySQL and MariaDB (no "ADD COLUMN IF NOT EXISTS").
        private static void EnsureDoctorColumns(MySqlConnection conn)
        {
            var columns = new[]
            {
                new { Name = "license_number", Ddl = "ALTER TABLE doctors ADD COLUMN license_number VARCHAR(50)" },
                new { Name = "credentials", Ddl = "ALTER TABLE doctors ADD COLUMN credentials VARCHAR(255)" },
                new { Name = "deactivation_reason", Ddl = "ALTER TABLE doctors ADD COLUMN deactivation_reason VARCHAR(255)" }
            };

            foreach (var col in columns)
            {
                bool exists;
                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.columns " +
                    "WHERE table_schema = DATABASE() AND table_name = 'doctors' AND column_name = @col", conn))
                {
                    cmd.Parameters.AddWithValue("@col", col.Name);
                    exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }

                if (!exists)
                {
                    using (var cmd = new MySqlCommand(col.Ddl, conn))
                        cmd.ExecuteNonQuery();
                }
            }
        }

        private static List<Doctor> LoadDoctors(MySqlConnection conn)
        {
            var list = new List<Doctor>();
            using (var cmd = new MySqlCommand(
                "SELECT id, full_name, department_id, specialization, contact, license_number, credentials, " +
                "is_on_duty, status, deactivation_reason FROM doctors", conn))
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
                        LicenseNumber = r.IsDBNull(r.GetOrdinal("license_number")) ? null : r.GetString("license_number"),
                        Credentials = r.IsDBNull(r.GetOrdinal("credentials")) ? null : r.GetString("credentials"),
                        IsOnDuty = r.GetBoolean("is_on_duty"),
                        Status = r.GetString("status"),
                        DeactivationReason = r.IsDBNull(r.GetOrdinal("deactivation_reason")) ? null : r.GetString("deactivation_reason")
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
            using (var cmd = new MySqlCommand(
                "SELECT id, full_name, age, gender, contact, address, blood_type, status, registered_on FROM patients", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int age = r.GetInt32("age");

                    list.Add(new Patient
                    {
                        Id = r.GetInt32("id"),
                        FullName = r.GetString("full_name"),
                        // Gi-convert ang Age → approximate Birthdate
                        Birthdate = DateTime.Today.AddYears(-age),
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
            using (var cmd = new MySqlCommand("SELECT id, patient_id, doctor_id, bed_id, admitted_on, requested_on, discharged_on, diagnosis, notes, status FROM admissions", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var admittedOn = r.GetDateTime("admitted_on");
                    list.Add(new Admission
                    {
                        Id = r.GetInt32("id"),
                        PatientId = r.GetInt32("patient_id"),
                        DoctorId = r.GetInt32("doctor_id"),
                        BedId = r.IsDBNull(r.GetOrdinal("bed_id")) ? 0 : r.GetInt32("bed_id"),
                        AdmittedOn = admittedOn,
                        // Admissions from before the waiting list were requested and admitted at once.
                        RequestedOn = r.IsDBNull(r.GetOrdinal("requested_on")) ? admittedOn : r.GetDateTime("requested_on"),
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
                "  per_day TINYINT(1) NOT NULL DEFAULT 0," +
                "  FOREIGN KEY (bill_id) REFERENCES bills(id) ON DELETE CASCADE)",

                "CREATE TABLE IF NOT EXISTS payments (" +
                "  id INT PRIMARY KEY AUTO_INCREMENT," +
                "  bill_id INT NOT NULL," +
                "  amount DECIMAL(12,2) NOT NULL," +
                "  payment_method VARCHAR(20) NOT NULL," +
                "  payment_date DATETIME NOT NULL," +
                "  reference_no VARCHAR(100)," +
                "  received_by VARCHAR(100)," +
                "  FOREIGN KEY (bill_id) REFERENCES bills(id))",

                "CREATE TABLE IF NOT EXISTS charge_schedules (" +
                "  id INT PRIMARY KEY AUTO_INCREMENT," +
                "  description VARCHAR(255) NOT NULL," +
                "  category VARCHAR(20) NOT NULL," +
                "  unit_price DECIMAL(12,2) NOT NULL," +
                "  ward VARCHAR(50) NULL," +
                "  per_day TINYINT(1) NOT NULL DEFAULT 0," +
                "  is_active TINYINT(1) NOT NULL DEFAULT 1)"
            };

            foreach (var sql in ddl)
            {
                using (var cmd = new MySqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }

            // bill_items existed before per_day was added; upgrade older databases in place.
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns " +
                "WHERE table_schema = DATABASE() AND table_name = 'bill_items' AND column_name = 'per_day'", conn))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    using (var alter = new MySqlCommand(
                        "ALTER TABLE bill_items ADD COLUMN per_day TINYINT(1) NOT NULL DEFAULT 0", conn))
                        alter.ExecuteNonQuery();
                }
            }

            // Breakdown/adjustment columns added after bills existed. Upgrading an older database
            // in place (same approach as EnsureDoctorColumns); schema.sql creates them directly.
            var billColumns = new[]
            {
                new { Name = "subtotal", Ddl = "subtotal DECIMAL(12,2) NOT NULL DEFAULT 0" },
                new { Name = "discount_value", Ddl = "discount_value DECIMAL(12,2) NOT NULL DEFAULT 0" },
                new { Name = "discount_is_percent", Ddl = "discount_is_percent TINYINT(1) NOT NULL DEFAULT 1" },
                new { Name = "discount_reason", Ddl = "discount_reason VARCHAR(255)" },
                new { Name = "discount_amount", Ddl = "discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0" },
                new { Name = "eligibility", Ddl = "eligibility VARCHAR(20) NOT NULL DEFAULT 'None'" },
                new { Name = "eligibility_id_no", Ddl = "eligibility_id_no VARCHAR(50)" },
                new { Name = "statutory_discount", Ddl = "statutory_discount DECIMAL(12,2) NOT NULL DEFAULT 0" },
                // Existing bills keep 0% so their totals don't change; new bills start at DefaultVatRate.
                new { Name = "vat_rate", Ddl = "vat_rate DECIMAL(5,2) NOT NULL DEFAULT 0" },
                new { Name = "vat_amount", Ddl = "vat_amount DECIMAL(12,2) NOT NULL DEFAULT 0" },
                new { Name = "hmo_provider", Ddl = "hmo_provider VARCHAR(100)" },
                new { Name = "hmo_loa_no", Ddl = "hmo_loa_no VARCHAR(50)" },
                new { Name = "hmo_coverage", Ddl = "hmo_coverage DECIMAL(12,2) NOT NULL DEFAULT 0" },
                new { Name = "hmo_amount", Ddl = "hmo_amount DECIMAL(12,2) NOT NULL DEFAULT 0" }
            };

            foreach (var col in billColumns)
                AddColumnIfMissing(conn, "bills", col.Name, col.Ddl);

            // Method-specific payment details (card, HMO, cash tendered).
            AddColumnIfMissing(conn, "payments", "amount_tendered", "amount_tendered DECIMAL(12,2) NULL");
            AddColumnIfMissing(conn, "payments", "card_type", "card_type VARCHAR(30)");
            AddColumnIfMissing(conn, "payments", "card_last4", "card_last4 CHAR(4)");
            AddColumnIfMissing(conn, "payments", "approval_code", "approval_code VARCHAR(50)");
            AddColumnIfMissing(conn, "payments", "hmo_provider", "hmo_provider VARCHAR(100)");
            AddColumnIfMissing(conn, "payments", "hmo_loa_no", "hmo_loa_no VARCHAR(50)");
            // Payments recorded as "Insurance" before HMO became its own method.
            using (var cmd = new MySqlCommand("UPDATE payments SET payment_method='HMO' WHERE payment_method='Insurance'", conn))
                cmd.ExecuteNonQuery();

            // Default admission charges, same as the seed in schema.sql.
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM charge_schedules", conn))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    using (var seed = new MySqlCommand(
                        "INSERT INTO charge_schedules (description, category, unit_price, ward, per_day, is_active) VALUES " +
                        "('Admission fee', 'Other', 500.00, NULL, 0, 1)," +
                        "('Room charge - General Ward', 'Room', 1500.00, 'General Ward', 1, 1)," +
                        "('Room charge - Private', 'Room', 3000.00, 'Private', 1, 1)," +
                        "('Room charge - ICU', 'Room', 8000.00, 'ICU', 1, 1)," +
                        "('Nursing care', 'Other', 350.00, NULL, 1, 1)," +
                        "('ICU monitoring', 'Procedure', 2000.00, 'ICU', 1, 1)," +
                        "('Basic laboratory panel (CBC, urinalysis)', 'Laboratory', 750.00, NULL, 0, 1)", conn))
                        seed.ExecuteNonQuery();
                }
            }
        }

        private static string ReadString(MySqlDataReader r, string column) =>
            r.IsDBNull(r.GetOrdinal(column)) ? null : r.GetString(column);

        // Works on both MySQL and MariaDB (no "ADD COLUMN IF NOT EXISTS").
        private static void AddColumnIfMissing(MySqlConnection conn, string table, string column, string ddl)
        {
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns " +
                "WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @col", conn))
            {
                cmd.Parameters.AddWithValue("@table", table);
                cmd.Parameters.AddWithValue("@col", column);
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return;
            }

            using (var cmd = new MySqlCommand("ALTER TABLE " + table + " ADD COLUMN " + ddl, conn))
                cmd.ExecuteNonQuery();
        }

        private static List<ChargeSchedule> LoadChargeSchedules(MySqlConnection conn)
        {
            var list = new List<ChargeSchedule>();
            using (var cmd = new MySqlCommand(
                "SELECT id, description, category, unit_price, ward, per_day, is_active FROM charge_schedules", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    BillCategory category;
                    if (!Enum.TryParse(r.GetString("category"), out category))
                        category = BillCategory.Other;

                    list.Add(new ChargeSchedule
                    {
                        Id = r.GetInt32("id"),
                        Description = r.GetString("description"),
                        Category = category,
                        UnitPrice = r.GetDecimal("unit_price"),
                        Ward = r.IsDBNull(r.GetOrdinal("ward")) ? null : r.GetString("ward"),
                        PerDay = r.GetBoolean("per_day"),
                        IsActive = r.GetBoolean("is_active")
                    });
                }
            }
            return list;
        }

        private static List<Bill> LoadBills(MySqlConnection conn)
        {
            var list = new List<Bill>();
            using (var cmd = new MySqlCommand(
                "SELECT id, patient_id, admission_id, appointment_id, bill_date, total_amount, amount_paid, " +
                "balance, status, notes, created_by, created_at, discount_value, discount_is_percent, discount_reason, " +
                "eligibility, eligibility_id_no, vat_rate, hmo_provider, hmo_loa_no, hmo_coverage FROM bills", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    BillStatus status;
                    Enum.TryParse(r.GetString("status"), out status);

                    DiscountEligibility eligibility;
                    if (!Enum.TryParse(r.GetString("eligibility"), out eligibility))
                        eligibility = DiscountEligibility.None;

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
                        CreatedAt = r.GetDateTime("created_at"),
                        Adjustments = new BillAdjustments
                        {
                            DiscountValue = r.GetDecimal("discount_value"),
                            DiscountIsPercent = r.GetBoolean("discount_is_percent"),
                            DiscountReason = r.IsDBNull(r.GetOrdinal("discount_reason")) ? null : r.GetString("discount_reason"),
                            Eligibility = eligibility,
                            EligibilityIdNo = r.IsDBNull(r.GetOrdinal("eligibility_id_no")) ? null : r.GetString("eligibility_id_no"),
                            VatRate = r.GetDecimal("vat_rate"),
                            HmoProvider = r.IsDBNull(r.GetOrdinal("hmo_provider")) ? null : r.GetString("hmo_provider"),
                            HmoLoaNo = r.IsDBNull(r.GetOrdinal("hmo_loa_no")) ? null : r.GetString("hmo_loa_no"),
                            HmoCoverage = r.GetDecimal("hmo_coverage")
                        }
                    });
                }
            }

            var byId = list.ToDictionary(b => b.Id);

            using (var cmd = new MySqlCommand(
                "SELECT id, bill_id, description, category, quantity, unit_price, amount, per_day FROM bill_items", conn))
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
                        Amount = r.GetDecimal("amount"),
                        PerDay = r.GetBoolean("per_day")
                    });
                }
            }

            using (var cmd = new MySqlCommand(
                "SELECT id, bill_id, amount, payment_method, payment_date, reference_no, received_by, " +
                "amount_tendered, card_type, card_last4, approval_code, hmo_provider, hmo_loa_no FROM payments", conn))
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
                        ReferenceNo = ReadString(r, "reference_no"),
                        ReceivedBy = ReadString(r, "received_by"),
                        AmountTendered = r.IsDBNull(r.GetOrdinal("amount_tendered")) ? (decimal?)null : r.GetDecimal("amount_tendered"),
                        CardType = ReadString(r, "card_type"),
                        CardLast4 = ReadString(r, "card_last4"),
                        ApprovalCode = ReadString(r, "approval_code"),
                        HmoProvider = ReadString(r, "hmo_provider"),
                        HmoLoaNo = ReadString(r, "hmo_loa_no")
                    });
                }
            }

            // Rebuild the breakdown (subtotal, discounts, VAT, HMO) from the items and adjustments.
            foreach (var bill in list)
            {
                bill.CalculateTotal();
                bill.CalculateBalance();
            }

            return list;
        }

        // Open alerts only (Active or Acknowledged); resolved ones stay in the table as history.
        private static List<Alert> LoadAlerts(MySqlConnection conn)
        {
            var list = new List<Alert>();
            using (var cmd = new MySqlCommand(
                "SELECT id, title, message, severity, status, created_on, source_key, module, acknowledged_by " +
                "FROM alerts WHERE status IN ('Active', 'Acknowledged')", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string sourceKey = ReadString(r, "source_key");
                    list.Add(new Alert
                    {
                        Id = r.GetInt32("id"),
                        Title = ReadString(r, "title") ?? "",
                        Message = ReadString(r, "message") ?? "",
                        Severity = ReadString(r, "severity") ?? "Low",
                        Status = r.GetString("status"),
                        CreatedOn = r.GetDateTime("created_on"),
                        SourceKey = sourceKey,
                        Module = ReadString(r, "module"),
                        AcknowledgedBy = ReadString(r, "acknowledged_by"),
                        IsAuto = sourceKey != null
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

        // Signs in and records the attempt, successful or not, in the activity log.
        public static User SignIn(string username, string password)
        {
            var user = Authenticate(username, password);
            if (user == null)
            {
                LogActivity("Security", "Sign-in Failed", $"Failed sign-in attempt for username \"{username}\"", "🔒", username);
                return null;
            }

            CurrentUser = user;
            LogActivity("Security", "Signed In", $"{user.DisplayName} ({user.Role}) signed in", "🔑");
            return user;
        }

        public static void SignOut()
        {
            if (CurrentUser != null)
                LogActivity("Security", "Signed Out", $"{CurrentUser.DisplayName} signed out", "🚪");
            CurrentUser = null;
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
        public static int GetNextPatientId()
        {
            using (var conn = Db.OpenConnection())
            {
                using (var cmd = new MySqlCommand("SELECT IFNULL(MAX(id), 0) + 1 FROM patients", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void ActivatePatient(Patient p)
        {
            Permissions.Demand(Permission.DeactivatePatients, "reactivate patient records");
            p.Status = "Active";
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE patients SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", p.Status);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Patients", "Activated", $"Activated patient: {p.FullName} ({p.PatientNo})", "👤");
        }
        public static void DeletePatient(Patient p)
        {
            Permissions.Demand(Permission.DeactivatePatients, "deactivate patient records");
            if (p == null) return;

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

        // Admitted or on the waiting list: either way the patient can't be admitted again or deactivated.
        public static bool HasActiveAdmission(int patientId) =>
            Admissions.Any(a => a.PatientId == patientId && (a.IsActive || a.IsPending));

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
        // With a bed (BedId > 0) the patient is admitted now. Without one (BedId == 0) the
        // admission goes on the waiting list as Pending until AssignBed is called.
        public static Admission AddAdmission(Admission a)
        {
            bool waitlisted = a.BedId <= 0;
            if (!waitlisted && !AvailableBeds().Any(b => b.Id == a.BedId))
                throw new InvalidOperationException("That bed is no longer available.");

            a.RequestedOn = DateTime.Now;
            a.AdmittedOn = a.RequestedOn;   // replaced when a bed is assigned to a waitlisted patient
            a.Status = waitlisted ? "Pending" : "Active";

            using (var conn = Db.OpenConnection())
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO admissions (patient_id, doctor_id, bed_id, admitted_on, requested_on, discharged_on, diagnosis, notes, status) " +
                    "VALUES (@patientId, @doctorId, @bedId, @admittedOn, @requestedOn, NULL, @diagnosis, @notes, @status); " +
                    "SELECT LAST_INSERT_ID();", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", a.PatientId);
                    cmd.Parameters.AddWithValue("@doctorId", a.DoctorId);
                    cmd.Parameters.AddWithValue("@bedId", waitlisted ? (object)DBNull.Value : a.BedId);
                    cmd.Parameters.AddWithValue("@admittedOn", a.AdmittedOn);
                    cmd.Parameters.AddWithValue("@requestedOn", a.RequestedOn);
                    cmd.Parameters.AddWithValue("@diagnosis", (object)a.Diagnosis ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object)a.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", a.Status);
                    a.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (!waitlisted)
                    SetBedOccupied(a.BedId, true, conn);
            }

            Admissions.Add(a);

            if (waitlisted)
                LogActivity("Admissions", "Waitlisted", $"{PatientName(a.PatientId)} added to the admission waiting list ({a.AdmissionNo})", "⏳");
            else
                LogActivity("Admissions", "Admitted", $"Admitted: {PatientName(a.PatientId)} to {BedLabel(a.BedId)}", "🏥");
            return a;
        }

        // Moves a waitlisted patient into a free bed; the stay (and room charges) start now.
        public static void AssignBed(Admission a, int bedId)
        {
            if (!AvailableBeds().Any(b => b.Id == bedId))
                throw new InvalidOperationException("That bed is no longer available.");

            TimeSpan waited = DateTime.Now - a.RequestedOn;
            a.AssignBed(bedId, DateTime.Now);

            using (var conn = Db.OpenConnection())
            {
                using (var cmd = new MySqlCommand(
                    "UPDATE admissions SET bed_id=@bedId, admitted_on=@admittedOn, status=@status WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@bedId", a.BedId);
                    cmd.Parameters.AddWithValue("@admittedOn", a.AdmittedOn);
                    cmd.Parameters.AddWithValue("@status", a.Status);
                    cmd.Parameters.AddWithValue("@id", a.Id);
                    cmd.ExecuteNonQuery();
                }
                SetBedOccupied(a.BedId, true, conn);
            }

            LogActivity("Admissions", "Bed Assigned",
                $"Admitted from waiting list: {PatientName(a.PatientId)} to {BedLabel(a.BedId)} after waiting {FormatDuration(waited)}", "🏥");
        }

        public static List<Admission> PendingAdmissions() =>
            Admissions.Where(a => a.IsPending).OrderBy(a => a.RequestedOn).ToList();

        private static void SetBedOccupied(int bedId, bool occupied, MySqlConnection conn)
        {
            using (var cmd = new MySqlCommand("UPDATE beds SET is_occupied=@occupied WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@occupied", occupied);
                cmd.Parameters.AddWithValue("@id", bedId);
                cmd.ExecuteNonQuery();
            }

            var bed = GetBed(bedId);
            if (bed != null)
                bed.IsOccupied = occupied;
        }

        public static string FormatDuration(TimeSpan span)
        {
            if (span.TotalMinutes < 60) return Math.Max(0, (int)span.TotalMinutes) + " min";
            if (span.TotalHours < 24) return (int)span.TotalHours + " h " + span.Minutes + " min";
            return (int)span.TotalDays + " d " + span.Hours + " h";
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

            // Admission status change feeds billing: finalize room days on the linked bill.
            SyncAdmissionBillOnDischarge(a);
        }

        // Admissions can't be cancelled; an admitted patient leaves only by discharge. A patient
        // still on the waiting list (no bed, no bill yet) can be taken off it, e.g. if they go elsewhere.
        public static void RemoveFromWaitingList(Admission a)
        {
            a.RemoveFromWaitingList();

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE admissions SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Admissions", "Removed from Waiting List",
                $"Removed {PatientName(a.PatientId)} from the admission waiting list ({a.AdmissionNo})", "❌");
        }

        public static List<Admission> ActiveAdmissions() => Admissions.Where(a => a.Status == "Active").ToList();

        // -------------------- Billing --------------------
        public const decimal ConsultationFee = 500m;

        // Standard VAT rate (%) applied to new bills; each bill can override it under Adjustments.
        public const decimal DefaultVatRate = 12m;

        public static Bill GetBill(int id) => Bills.FirstOrDefault(b => b.Id == id);

        public static Bill OpenBillForAdmission(int admissionId) =>
            Bills.FirstOrDefault(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled);

        public static Bill OpenBillForAppointment(int appointmentId) =>
            Bills.FirstOrDefault(b => b.AppointmentId == appointmentId && b.Status != BillStatus.Cancelled);

        public static IEnumerable<Bill> OpenBills() => Bills.Where(b => b.Status != BillStatus.Cancelled);

        // Still owed by patients, across all bills.
        public static decimal OutstandingBalance() => OpenBills().Sum(b => b.Balance);

        // Approved HMO coverage not yet received from the HMOs.
        public static decimal OutstandingHmo() => OpenBills().Sum(b => b.HmoBalance);

        public static decimal PatientOutstanding(int patientId) =>
            OpenBills().Where(b => b.PatientId == patientId).Sum(b => b.Balance);

        public static int PatientBillsWithBalance(int patientId) =>
            OpenBills().Count(b => b.PatientId == patientId && b.Balance > 0);

        // Line items from the charge schedule that apply to the admission's ward,
        // with per-day charges covering the days stayed so far.
        public static List<BillItem> AdmissionChargesFor(Admission admission)
        {
            var bed = GetBed(admission.BedId);
            string ward = bed != null ? bed.Ward : null;
            int days = admission.CalculateDaysStayed();

            return ChargeSchedules
                .Where(c => c.AppliesTo(ward))
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Description)
                .Select(c => c.ToBillItem(days))
                .ToList();
        }

        // Starting line items for a bill: scheduled admission charges, or the consultation fee.
        public static List<BillItem> DefaultChargesFor(Admission admission, Appointment appointment)
        {
            var items = new List<BillItem>();

            if (admission != null)
                items.AddRange(AdmissionChargesFor(admission));

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
            // New bills start at the standard VAT rate; it can be changed per bill under Adjustments.
            if (b.Adjustments.VatRate == 0)
                b.Adjustments.VatRate = DefaultVatRate;
            foreach (var item in items)
                b.AddItem(item);
            b.CalculateTotal();
            b.CalculateBalance();

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

                SaveBillTotals(b, conn, tx);   // breakdown and adjustment columns
                tx.Commit();
            }

            Bills.Add(b);
            LogActivity("Billing", "Created", $"Bill {b.BillNo} created for {PatientName(b.PatientId)} ({b.TotalAmount:N2})", "🧾");
            return b;
        }

        // Admission -> billing trigger: guarantees every admission has an open bill with
        // the charges from the pre-set schedule for its ward. Returns the existing open bill if one is already linked, so
        // admitting never double-bills. Called by AdmissionsView right after AddAdmission.
        public static Bill EnsureAdmissionBill(Admission admission)
        {
            // Waitlisted: no bed means no ward and no room rate yet; the bill opens on AssignBed.
            if (admission == null || admission.IsPending) return null;

            var existing = OpenBillForAdmission(admission.Id);
            if (existing != null) return existing;

            var items = DefaultChargesFor(admission, null);
            return CreateBill(new Bill
            {
                PatientId = admission.PatientId,
                AdmissionId = admission.Id,
                Notes = "Auto-generated on admission"
            }, items);
        }

        // On discharge, bring the per-day charges (room, nursing...) in line with the real
        // length of stay (the bill was opened at admit with 1 day). A deposit already paid
        // doesn't block this: the total only grows, so it never drops below what was paid.
        // Never lets a billing hiccup break the discharge itself.
        public static void SyncAdmissionBillOnDischarge(Admission admission)
        {
            if (admission == null) return;

            var bill = OpenBillForAdmission(admission.Id);
            if (bill == null) return;

            int days = admission.CalculateDaysStayed();
            var changed = bill.SyncPerDayItems(days);
            if (changed.Count == 0) return;

            try
            {
                using (var conn = Db.OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    foreach (var item in changed)
                    {
                        using (var cmd = new MySqlCommand(
                            "UPDATE bill_items SET quantity=@q, amount=@a WHERE id=@id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@q", item.Quantity);
                            cmd.Parameters.AddWithValue("@a", item.Amount);
                            cmd.Parameters.AddWithValue("@id", item.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    SaveBillTotals(bill, conn, tx);
                    tx.Commit();
                }
                LogActivity("Billing", "Updated",
                    $"Per-day charges on {bill.BillNo} updated to {days} day(s) at discharge ({bill.TotalAmount:N2})", "🧾");
            }
            catch (MySqlException)
            {
                // Discharge already succeeded; the bill can still be adjusted in Billing.
            }
        }

        public static void AddBillItem(Bill b, BillItem item)
        {
            Permissions.Demand(Permission.AddBillCharges, "add charges to bills");
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
            Permissions.Demand(Permission.RemoveBillCharges, "remove charges from bills");
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
            Permissions.Demand(Permission.RecordPayments, "record payments");
            if (p.PaymentDate == default)
                p.PaymentDate = DateTime.Now;
            p.ReceivedBy = CurrentUser != null ? CurrentUser.DisplayName : null;
            b.ApplyPayment(p);

            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO payments (bill_id, amount, payment_method, payment_date, reference_no, received_by, " +
                    "amount_tendered, card_type, card_last4, approval_code, hmo_provider, hmo_loa_no) " +
                    "VALUES (@billId, @amount, @method, @date, @ref, @receivedBy, " +
                    "@tendered, @cardType, @cardLast4, @approval, @hmoProvider, @hmoLoa); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@billId", p.BillId);
                    cmd.Parameters.AddWithValue("@amount", p.Amount);
                    cmd.Parameters.AddWithValue("@method", p.Method.ToString());
                    cmd.Parameters.AddWithValue("@date", p.PaymentDate);
                    cmd.Parameters.AddWithValue("@ref", NullIfBlank(p.ReferenceNo));
                    cmd.Parameters.AddWithValue("@receivedBy", NullIfBlank(p.ReceivedBy));
                    cmd.Parameters.AddWithValue("@tendered", (object)p.AmountTendered ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cardType", NullIfBlank(p.CardType));
                    cmd.Parameters.AddWithValue("@cardLast4", NullIfBlank(p.CardLast4));
                    cmd.Parameters.AddWithValue("@approval", NullIfBlank(p.ApprovalCode));
                    cmd.Parameters.AddWithValue("@hmoProvider", NullIfBlank(p.HmoProvider));
                    cmd.Parameters.AddWithValue("@hmoLoa", NullIfBlank(p.HmoLoaNo));
                    p.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
                SaveBillTotals(b, conn, tx);
                tx.Commit();
            }

            LogActivity("Billing", "Payment",
                $"{p.MethodLabel} payment of {p.Amount:N2} received for {b.BillNo}" +
                (string.IsNullOrEmpty(p.Details) ? "" : $" ({p.Details})") +
                $". Patient balance {b.Balance:N2}" + (b.HmoBalance > 0 ? $", HMO outstanding {b.HmoBalance:N2}" : ""), "💰");
        }

        private static void InsertBillItem(BillItem item, MySqlConnection conn, MySqlTransaction tx)
        {
            using (var cmd = new MySqlCommand(
                "INSERT INTO bill_items (bill_id, description, category, quantity, unit_price, amount, per_day) " +
                "VALUES (@billId, @description, @category, @quantity, @unitPrice, @amount, @perDay); SELECT LAST_INSERT_ID();", conn, tx))
            {
                cmd.Parameters.AddWithValue("@billId", item.BillId);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@category", item.Category.ToString());
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("@amount", item.Amount);
                cmd.Parameters.AddWithValue("@perDay", item.PerDay);
                item.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Replaces a bill's discount, senior citizen/PWD discount, VAT rate and HMO coverage.
        public static void UpdateBillAdjustments(Bill b, BillAdjustments adj)
        {
            Permissions.Demand(Permission.AdjustBills, "apply discounts, VAT or HMO coverage");
            b.ApplyAdjustments(adj);

            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                SaveBillTotals(b, conn, tx);
                tx.Commit();
            }

            LogActivity("Billing", "Adjusted",
                $"Adjusted {b.BillNo}: discounts {(b.DiscountAmount + b.StatutoryDiscountAmount):N2}, VAT {b.VatAmount:N2}, " +
                $"HMO {b.HmoAmount:N2}, amount due {b.TotalAmount:N2}", "🧾");
        }

        // Saves the totals, the full breakdown and the adjustment inputs.
        private static void SaveBillTotals(Bill b, MySqlConnection conn, MySqlTransaction tx)
        {
            var adj = b.Adjustments;
            using (var cmd = new MySqlCommand(
                "UPDATE bills SET total_amount=@total, amount_paid=@paid, balance=@balance, status=@status, " +
                "subtotal=@subtotal, discount_value=@discountValue, discount_is_percent=@discountIsPercent, " +
                "discount_reason=@discountReason, discount_amount=@discountAmount, eligibility=@eligibility, " +
                "eligibility_id_no=@eligibilityIdNo, statutory_discount=@statutoryDiscount, vat_rate=@vatRate, " +
                "vat_amount=@vatAmount, hmo_provider=@hmoProvider, hmo_loa_no=@hmoLoaNo, hmo_coverage=@hmoCoverage, " +
                "hmo_amount=@hmoAmount WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@total", b.TotalAmount);
                cmd.Parameters.AddWithValue("@paid", b.AmountPaid);
                cmd.Parameters.AddWithValue("@balance", b.Balance);
                cmd.Parameters.AddWithValue("@status", b.Status.ToString());
                cmd.Parameters.AddWithValue("@subtotal", b.Subtotal);
                cmd.Parameters.AddWithValue("@discountValue", adj.DiscountValue);
                cmd.Parameters.AddWithValue("@discountIsPercent", adj.DiscountIsPercent);
                cmd.Parameters.AddWithValue("@discountReason", NullIfBlank(adj.DiscountReason));
                cmd.Parameters.AddWithValue("@discountAmount", b.DiscountAmount);
                cmd.Parameters.AddWithValue("@eligibility", adj.Eligibility.ToString());
                cmd.Parameters.AddWithValue("@eligibilityIdNo", NullIfBlank(adj.EligibilityIdNo));
                cmd.Parameters.AddWithValue("@statutoryDiscount", b.StatutoryDiscountAmount);
                cmd.Parameters.AddWithValue("@vatRate", adj.VatRate);
                cmd.Parameters.AddWithValue("@vatAmount", b.VatAmount);
                cmd.Parameters.AddWithValue("@hmoProvider", NullIfBlank(adj.HmoProvider));
                cmd.Parameters.AddWithValue("@hmoLoaNo", NullIfBlank(adj.HmoLoaNo));
                cmd.Parameters.AddWithValue("@hmoCoverage", adj.HmoCoverage);
                cmd.Parameters.AddWithValue("@hmoAmount", b.HmoAmount);
                cmd.Parameters.AddWithValue("@id", b.Id);
                cmd.ExecuteNonQuery();
            }
        }

        private static object NullIfBlank(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        // -------------------- Charge Schedule --------------------
        public static List<string> Wards() =>
            Beds.Select(b => b.Ward).Where(w => !string.IsNullOrEmpty(w)).Distinct().OrderBy(w => w).ToList();

        public static ChargeSchedule AddChargeSchedule(ChargeSchedule c)
        {
            Permissions.Demand(Permission.ManageChargeSchedule, "change the admission charge schedule");
            c.IsActive = true;

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO charge_schedules (description, category, unit_price, ward, per_day, is_active) " +
                "VALUES (@description, @category, @unitPrice, @ward, @perDay, @isActive); SELECT LAST_INSERT_ID();", conn))
            {
                AddChargeScheduleParameters(cmd, c);
                c.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            ChargeSchedules.Add(c);
            LogActivity("Billing", "Schedule Added", $"Added scheduled charge \"{c.Description}\" ({c.UnitPrice:N2}, {c.Basis})", "📋");
            return c;
        }

        // Only affects bills generated from now on; existing bills keep the price they were issued with.
        public static void UpdateChargeSchedule(ChargeSchedule c)
        {
            Permissions.Demand(Permission.ManageChargeSchedule, "change the admission charge schedule");
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE charge_schedules SET description=@description, category=@category, unit_price=@unitPrice, " +
                "ward=@ward, per_day=@perDay, is_active=@isActive WHERE id=@id", conn))
            {
                AddChargeScheduleParameters(cmd, c);
                cmd.Parameters.AddWithValue("@id", c.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Billing", "Schedule Updated",
                $"Scheduled charge \"{c.Description}\" updated ({c.UnitPrice:N2}, {c.Basis}{(c.IsActive ? "" : ", inactive")})", "📋");
        }

        private static void AddChargeScheduleParameters(MySqlCommand cmd, ChargeSchedule c)
        {
            cmd.Parameters.AddWithValue("@description", c.Description);
            cmd.Parameters.AddWithValue("@category", c.Category.ToString());
            cmd.Parameters.AddWithValue("@unitPrice", c.UnitPrice);
            cmd.Parameters.AddWithValue("@ward", string.IsNullOrEmpty(c.Ward) ? (object)DBNull.Value : c.Ward);
            cmd.Parameters.AddWithValue("@perDay", c.PerDay);
            cmd.Parameters.AddWithValue("@isActive", c.IsActive);
        }

        // -------------------- Beds & Helpers --------------------
        public static Bed GetBed(int id) => Beds.FirstOrDefault(b => b.Id == id);

        public static List<Bed> AvailableBeds() => Beds.Where(b => !b.IsOccupied).ToList();

        public static string BedLabel(int id)
        {
            if (id <= 0) return "(waiting for bed)";
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

        public static List<Doctor> AllDoctors() => Doctors.ToList();

        public static Doctor AddDoctor(Doctor d)
        {
            Permissions.Demand(Permission.ManageDoctors, "add doctors");
            d.Status = "Active";

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO doctors (full_name, department_id, specialization, contact, license_number, credentials, is_on_duty, status) " +
                "VALUES (@fullName, @departmentId, @specialization, @contact, @licenseNumber, @credentials, @isOnDuty, @status); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@fullName", d.FullName);
                cmd.Parameters.AddWithValue("@departmentId", d.DepartmentId);
                cmd.Parameters.AddWithValue("@specialization", (object)d.Specialization ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@contact", (object)d.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@licenseNumber", (object)d.LicenseNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@credentials", (object)d.Credentials ?? DBNull.Value);
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
            Permissions.Demand(Permission.ManageDoctors, "edit doctor records");
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE doctors SET full_name=@fullName, department_id=@departmentId, specialization=@specialization, " +
                "contact=@contact, license_number=@licenseNumber, credentials=@credentials, is_on_duty=@isOnDuty WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@fullName", d.FullName);
                cmd.Parameters.AddWithValue("@departmentId", d.DepartmentId);
                cmd.Parameters.AddWithValue("@specialization", (object)d.Specialization ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@contact", (object)d.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@licenseNumber", (object)d.LicenseNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@credentials", (object)d.Credentials ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isOnDuty", d.IsOnDuty);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Doctors", "Updated", $"Dr. {d.FullName} updated ({(d.IsOnDuty ? "On Duty" : "Off Duty")})", "🩺");
        }

        // Soft-delete only: doctors are referenced by appointments/admissions (FK),
        // and losing a doctor's record should never erase that history. Deactivating
        // takes the doctor off duty and records why; the reason is kept while inactive.
        public static void DeactivateDoctor(Doctor d, string reason)
        {
            Permissions.Demand(Permission.ManageDoctors, "deactivate doctors");
            d.Status = "Inactive";
            d.DeactivationReason = reason;
            d.IsOnDuty = false;

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE doctors SET status=@status, deactivation_reason=@reason, is_on_duty=0 WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", d.Status);
                cmd.Parameters.AddWithValue("@reason", (object)d.DeactivationReason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Doctors", "Deactivated", $"Deactivated doctor: Dr. {d.FullName} - {reason}", "🩺");
        }

        public static void ActivateDoctor(Doctor d)
        {
            Permissions.Demand(Permission.ManageDoctors, "reactivate doctors");
            d.Status = "Active";
            d.DeactivationReason = null;

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE doctors SET status=@status, deactivation_reason=NULL WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", d.Status);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity("Doctors", "Reactivated", $"Reactivated doctor: Dr. {d.FullName}", "🩺");
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

        // -------------------- Activity Log --------------------
        public const string SystemUser = "System";

        // Every action in every module comes through here. The entry records who did it
        // (the signed-in user, or "System" for automatic actions). Logging a module action
        // also re-checks the alert conditions, so alerts follow what happens in the modules.
        public static void LogActivity(string module, string action, string description, string icon, string username = null)
        {
            var item = new ActivityItem
            {
                Module = module,
                Action = action,
                Description = description,
                Icon = icon,
                Timestamp = DateTime.Now,
                Username = username ?? (CurrentUser != null ? CurrentUser.Username : SystemUser)
            };

            try
            {
                using (var conn = Db.OpenConnection())
                using (var cmd = new MySqlCommand(
                    "INSERT INTO activity_log (module, action, description, icon, created_at, username) " +
                    "VALUES (@module, @action, @description, @icon, @createdAt, @username); SELECT LAST_INSERT_ID();", conn))
                {
                    cmd.Parameters.AddWithValue("@module", item.Module);
                    cmd.Parameters.AddWithValue("@action", item.Action);
                    cmd.Parameters.AddWithValue("@description", Truncate(item.Description, 255));
                    cmd.Parameters.AddWithValue("@icon", (object)item.Icon ?? "");
                    cmd.Parameters.AddWithValue("@createdAt", item.Timestamp);
                    cmd.Parameters.AddWithValue("@username", Truncate(item.Username, 50));
                    item.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (MySqlException)
            {
                // The action itself already succeeded; a logging failure must not undo or block it.
            }

            if (module != AlertMonitor.ModuleName && module != "Security")
                AlertMonitor.Evaluate();
        }

        private static string Truncate(string s, int max) =>
            s == null || s.Length <= max ? s : s.Substring(0, max);

        // Newest first. Every filter is optional; limit keeps a huge log from freezing the grid.
        public static List<ActivityItem> QueryActivityLog(DateTime? from, DateTime? to, string module, string username, string search, int limit)
        {
            Permissions.Demand(Permission.ViewActivityLog, "view the activity log");
            var sql = new System.Text.StringBuilder(
                "SELECT id, module, action, description, icon, created_at, username FROM activity_log WHERE 1=1");
            var cmd = new MySqlCommand();
            if (from.HasValue) { sql.Append(" AND created_at >= @from"); cmd.Parameters.AddWithValue("@from", from.Value); }
            if (to.HasValue) { sql.Append(" AND created_at < @to"); cmd.Parameters.AddWithValue("@to", to.Value); }
            if (!string.IsNullOrEmpty(module)) { sql.Append(" AND module = @module"); cmd.Parameters.AddWithValue("@module", module); }
            if (!string.IsNullOrEmpty(username)) { sql.Append(" AND username = @username"); cmd.Parameters.AddWithValue("@username", username); }
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql.Append(" AND (description LIKE @search OR action LIKE @search)");
                cmd.Parameters.AddWithValue("@search", "%" + search.Trim() + "%");
            }
            sql.Append(" ORDER BY created_at DESC, id DESC LIMIT @limit");
            cmd.Parameters.AddWithValue("@limit", limit);

            var list = new List<ActivityItem>();
            using (var conn = Db.OpenConnection())
            using (cmd)
            {
                cmd.Connection = conn;
                cmd.CommandText = sql.ToString();
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
                            Icon = ReadString(r, "icon") ?? "",
                            Timestamp = r.GetDateTime("created_at"),
                            Username = ReadString(r, "username") ?? "(not recorded)"
                        });
                    }
                }
            }
            return list;
        }

        public static List<string> ActivityLogModules()
        {
            var list = new List<string>();
            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("SELECT DISTINCT module FROM activity_log ORDER BY module", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(r.GetString(0));
            }
            return list;
        }

        // -------------------- Alerts --------------------
        // Alerts are only ever raised and resolved by AlertMonitor from conditions in the
        // other modules; staff can acknowledge one, which hides it until the condition
        // clears (and raises it again if the condition comes back later).

        public static List<Alert> ActiveAlerts() =>
            Alerts.Where(a => a.IsActive).OrderBy(a => a.SeverityRank).ThenByDescending(a => a.CreatedOn).ToList();

        public static List<Alert> AcknowledgedAlerts() =>
            Alerts.Where(a => a.IsAcknowledged).OrderBy(a => a.SeverityRank).ThenByDescending(a => a.CreatedOn).ToList();

        internal static Alert RaiseAlert(string sourceKey, string module, string title, string message, string severity)
        {
            var a = new Alert
            {
                SourceKey = sourceKey,
                Module = module,
                Title = title,
                Message = message,
                Severity = severity,
                Status = "Active",
                IsAuto = true,
                CreatedOn = DateTime.Now
            };

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO alerts (title, message, severity, status, created_on, source_key, module) " +
                "VALUES (@title, @message, @severity, @status, @createdOn, @sourceKey, @module); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@title", a.Title);
                cmd.Parameters.AddWithValue("@message", Truncate(a.Message, 500));
                cmd.Parameters.AddWithValue("@severity", a.Severity);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@createdOn", a.CreatedOn);
                cmd.Parameters.AddWithValue("@sourceKey", a.SourceKey);
                cmd.Parameters.AddWithValue("@module", a.Module);
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Alerts.Add(a);
            LogActivity(AlertMonitor.ModuleName, "Raised", $"[{severity}] {title}: {message} (from {module})", "⚠️", SystemUser);
            return a;
        }

        // Same condition still holding, but its details changed (e.g. 1 ICU bed left -> 0).
        internal static void UpdateAlert(Alert a, string title, string message, string severity)
        {
            bool escalated = SeverityRankOf(severity) < a.SeverityRank;
            a.Title = title;
            a.Message = message;
            a.Severity = severity;
            // A worse situation than the one that was acknowledged needs attention again.
            if (escalated && a.IsAcknowledged)
            {
                a.Status = "Active";
                a.AcknowledgedBy = null;
            }

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE alerts SET title=@title, message=@message, severity=@severity, status=@status, acknowledged_by=@ackBy WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@message", Truncate(message, 500));
                cmd.Parameters.AddWithValue("@severity", severity);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@ackBy", (object)a.AcknowledgedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }

            if (escalated)
                LogActivity(AlertMonitor.ModuleName, "Escalated", $"[{severity}] {title}: {message}", "⚠️", SystemUser);
        }

        // The condition cleared: close the alert (history stays in the alerts table).
        internal static void AutoResolveAlert(Alert a)
        {
            a.Status = "Resolved";
            a.ResolvedOn = DateTime.Now;
            Alerts.Remove(a);

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE alerts SET status='Resolved', resolved_on=@resolvedOn WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@resolvedOn", a.ResolvedOn);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity(AlertMonitor.ModuleName, "Auto-resolved", $"{a.Title} cleared: condition no longer holds", "✅", SystemUser);
        }

        // Staff saw it and are handling it. Old manual alerts (no trigger) are closed outright.
        public static void AcknowledgeAlert(int alertId)
        {
            var a = Alerts.FirstOrDefault(x => x.Id == alertId);
            if (a == null || !a.IsActive) return;

            bool manual = a.SourceKey == null;
            a.Status = manual ? "Resolved" : "Acknowledged";
            a.AcknowledgedBy = CurrentUser != null ? CurrentUser.DisplayName : SystemUser;
            if (manual)
            {
                a.ResolvedOn = DateTime.Now;
                Alerts.Remove(a);
            }

            using (var conn = Db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE alerts SET status=@status, acknowledged_by=@ackBy, resolved_on=@resolvedOn WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@ackBy", a.AcknowledgedBy);
                cmd.Parameters.AddWithValue("@resolvedOn", (object)a.ResolvedOn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }

            LogActivity(AlertMonitor.ModuleName, manual ? "Resolved" : "Acknowledged", $"{a.Title}: {a.Message}", "👁");
        }

        private static int SeverityRankOf(string severity) =>
            severity == "High" ? 0 : severity == "Medium" ? 1 : 2;
    }
}

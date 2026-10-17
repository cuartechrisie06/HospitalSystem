using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data
{
    // Upgrades an older database in place so it matches Data/schema.sql (keep the two in sync).
    // Runs once at startup, before anything is loaded. Works on both MySQL and MariaDB
    // (no "ADD COLUMN IF NOT EXISTS").
    public class SchemaMigrator
    {
        private readonly IDbConnectionFactory db;

        public SchemaMigrator(IDbConnectionFactory db)
        {
            this.db = db;
        }

        // Messages about what the upgrade did (or couldn't do yet), for the activity log.
        public List<string> Notes { get; } = new List<string>();

        // Returns how many old manually posted alerts were closed, so the caller can log it.
        public int Migrate()
        {
            using (var conn = db.OpenConnection())
            {
                EnsureDoctorColumns(conn);
                int retired = EnsureWorkflowColumns(conn);
                EnsureBillingTables(conn);
                EnsureSecurityColumns(conn);
                EnsurePatientColumns(conn);
                EnsureIndexes(conn);
                EnsureBedLayout(conn);
                return retired;
            }
        }

        // Adding the columns here means a database imported before the doctor record
        // fields existed picks them up without a re-import.
        private static void EnsureDoctorColumns(MySqlConnection conn)
        {
            AddColumnIfMissing(conn, "doctors", "license_number", "license_number VARCHAR(50)");
            AddColumnIfMissing(conn, "doctors", "credentials", "credentials VARCHAR(255)");
            AddColumnIfMissing(conn, "doctors", "deactivation_reason", "deactivation_reason VARCHAR(255)");
        }

        // The waiting list (admissions without a bed), alert triggers and the activity log's user.
        private static int EnsureWorkflowColumns(MySqlConnection conn)
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
                cmd.Parameters.AddWithValue("@by", ActivityItem.SystemUser);
                retired = cmd.ExecuteNonQuery();
            }

            AddColumnIfMissing(conn, "activity_log", "username", "username VARCHAR(50) NULL");
            return retired;
        }

        // Creating them here means a database imported before billing existed picks the tables up without a re-import.
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

            // bill_items existed before per_day was added.
            AddColumnIfMissing(conn, "bill_items", "per_day", "per_day TINYINT(1) NOT NULL DEFAULT 0");

            // Breakdown/adjustment columns added after bills existed; schema.sql creates them directly.
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
                // Existing bills keep 0% so their totals don't change; new bills start at BillingRates.DefaultVatRate.
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
                        "('Room charge - Ward', 'Room', 1500.00, 'Ward', 1, 1)," +
                        "('Room charge - Semi-Private', 'Room', 2250.00, 'Semi-Private', 1, 1)," +
                        "('Room charge - Private', 'Room', 3000.00, 'Private', 1, 1)," +
                        "('Room charge - ICU', 'Room', 8000.00, 'ICU', 1, 1)," +
                        "('Nursing care', 'Other', 350.00, NULL, 1, 1)," +
                        "('ICU monitoring', 'Procedure', 2000.00, 'ICU', 1, 1)," +
                        "('Basic laboratory panel (CBC, urinalysis)', 'Laboratory', 750.00, NULL, 0, 1)", conn))
                        seed.ExecuteNonQuery();
                }
            }
        }

        // Password hashes are longer than the old plain-text column allowed; failed sign-ins lock
        // an account for a while (tracked in the database so every workstation sees the lock).
        // AuthService replaces any remaining plain-text passwords with hashes on startup.
        private static void EnsureSecurityColumns(MySqlConnection conn)
        {
            using (var cmd = new MySqlCommand(
                "SELECT CHARACTER_MAXIMUM_LENGTH FROM information_schema.columns " +
                "WHERE table_schema = DATABASE() AND table_name = 'users' AND column_name = 'password'", conn))
            {
                if (Convert.ToInt64(cmd.ExecuteScalar()) < 255)
                {
                    using (var alter = new MySqlCommand("ALTER TABLE users MODIFY password VARCHAR(255) NOT NULL", conn))
                        alter.ExecuteNonQuery();
                }
            }

            AddColumnIfMissing(conn, "users", "failed_attempts", "failed_attempts INT NOT NULL DEFAULT 0");
            AddColumnIfMissing(conn, "users", "locked_until", "locked_until DATETIME NULL");
        }

        // A real birthdate (age alone goes stale every birthday) and who deactivated or
        // reactivated a patient record, and when.
        private static void EnsurePatientColumns(MySqlConnection conn)
        {
            bool hadBirthdate = ColumnExists(conn, "patients", "birthdate");
            AddColumnIfMissing(conn, "patients", "birthdate", "birthdate DATE NULL");
            if (!hadBirthdate)
            {
                // Best estimate for records saved before birthdates were stored: the age they
                // were registered with, counted back from the registration date.
                using (var cmd = new MySqlCommand(
                    "UPDATE patients SET birthdate = DATE_SUB(DATE(registered_on), INTERVAL age YEAR) WHERE birthdate IS NULL", conn))
                    cmd.ExecuteNonQuery();
            }

            AddColumnIfMissing(conn, "patients", "deactivated_at", "deactivated_at DATETIME NULL");
            AddColumnIfMissing(conn, "patients", "deactivated_by", "deactivated_by VARCHAR(100) NULL");
            AddColumnIfMissing(conn, "patients", "deactivation_reason", "deactivation_reason VARCHAR(255) NULL");
            AddColumnIfMissing(conn, "patients", "deactivation_notes", "deactivation_notes VARCHAR(500) NULL");
            AddColumnIfMissing(conn, "patients", "reactivated_at", "reactivated_at DATETIME NULL");
            AddColumnIfMissing(conn, "patients", "reactivated_by", "reactivated_by VARCHAR(100) NULL");
        }

        // Moves a database still on the old layout (6 rooms of 2 beds: General Ward / Private / ICU) to
        // BedLayout, and renames the General Ward room charge and adds the semi-private one to match.
        // Beds are rearranged in place so past admissions keep their bed. A patient in a bed would end up
        // labelled with a different room, so this waits until no bed is occupied.
        private void EnsureBedLayout(MySqlConnection conn)
        {
            if (Count(conn, "SELECT COUNT(*) FROM beds WHERE ward='" + BedLayout.SemiPrivate + "'") > 0)
                return;   // already on the new layout

            if (Count(conn, "SELECT COUNT(*) FROM beds WHERE is_occupied=1") > 0 ||
                Count(conn, "SELECT COUNT(*) FROM admissions WHERE status='Active'") > 0)
            {
                Notes.Add("Room layout update (private / semi-private / ward / ICU) is waiting: it is applied " +
                          "automatically the next time the app starts with no patient in a bed.");
                return;
            }

            var existing = new List<int>();
            using (var cmd = new MySqlCommand("SELECT id FROM beds ORDER BY id", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) existing.Add(r.GetInt32(0));

            using (var tx = conn.BeginTransaction())
            {
                for (int i = 0; i < BedLayout.Beds.Length; i++)
                {
                    var bed = BedLayout.Beds[i];
                    string sql = i < existing.Count
                        ? "UPDATE beds SET room_no=@room, bed_no=@bed, ward=@ward, is_occupied=0 WHERE id=@id"
                        : "INSERT INTO beds (room_no, bed_no, ward, is_occupied) VALUES (@room, @bed, @ward, 0)";
                    using (var cmd = new MySqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@room", bed.Room);
                        cmd.Parameters.AddWithValue("@bed", bed.Bed);
                        cmd.Parameters.AddWithValue("@ward", bed.Ward);
                        if (i < existing.Count) cmd.Parameters.AddWithValue("@id", existing[i]);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Extra beds beyond the layout: remove the ones no admission ever used.
                foreach (int id in existing.Skip(BedLayout.Beds.Length))
                {
                    using (var cmd = new MySqlCommand(
                        "DELETE FROM beds WHERE id=@id AND NOT EXISTS (SELECT 1 FROM admissions WHERE bed_id=@id)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                Exec(conn, tx, "UPDATE charge_schedules SET ward='" + BedLayout.Ward + "' WHERE ward='General Ward'");
                Exec(conn, tx, "UPDATE charge_schedules SET description='Room charge - Ward' WHERE description='Room charge - General Ward'");
                if (Count(conn, "SELECT COUNT(*) FROM charge_schedules WHERE ward='" + BedLayout.SemiPrivate + "'", tx) == 0)
                {
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO charge_schedules (description, category, unit_price, ward, per_day, is_active) " +
                        "VALUES ('Room charge - Semi-Private', 'Room', @rate, @ward, 1, 1)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@rate", BedLayout.SemiPrivateRate);
                        cmd.Parameters.AddWithValue("@ward", BedLayout.SemiPrivate);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }

            Notes.Add($"Rooms updated: 2 private (1 bed), 2 semi-private (2 beds), 2 wards (3 beds) and 2 ICU rooms (2 beds), " +
                      $"{BedLayout.Beds.Length} beds in all. 'General Ward' is now 'Ward'; a semi-private room charge was added.");
        }

        private static long Count(MySqlConnection conn, string sql, MySqlTransaction tx = null)
        {
            using (var cmd = new MySqlCommand(sql, conn, tx))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private static void Exec(MySqlConnection conn, MySqlTransaction tx, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn, tx))
                cmd.ExecuteNonQuery();
        }

        // Lookups the app runs often. Foreign key columns are already indexed by InnoDB.
        private static void EnsureIndexes(MySqlConnection conn)
        {
            AddIndexIfMissing(conn, "appointments", "ix_appointments_doctor_time", "doctor_id, scheduled_on");
            AddIndexIfMissing(conn, "appointments", "ix_appointments_status", "status");
            AddIndexIfMissing(conn, "admissions", "ix_admissions_status", "status");
            AddIndexIfMissing(conn, "bills", "ix_bills_status", "status");
            AddIndexIfMissing(conn, "alerts", "ix_alerts_status", "status");
            AddIndexIfMissing(conn, "activity_log", "ix_activity_created", "created_at");
            AddIndexIfMissing(conn, "activity_log", "ix_activity_module", "module");
            AddIndexIfMissing(conn, "activity_log", "ix_activity_username", "username");
        }

        private static bool ColumnExists(MySqlConnection conn, string table, string column)
        {
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns " +
                "WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @col", conn))
            {
                cmd.Parameters.AddWithValue("@table", table);
                cmd.Parameters.AddWithValue("@col", column);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void AddIndexIfMissing(MySqlConnection conn, string table, string index, string columns)
        {
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.statistics " +
                "WHERE table_schema = DATABASE() AND table_name = @table AND index_name = @index", conn))
            {
                cmd.Parameters.AddWithValue("@table", table);
                cmd.Parameters.AddWithValue("@index", index);
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return;
            }

            using (var cmd = new MySqlCommand("CREATE INDEX " + index + " ON " + table + " (" + columns + ")", conn))
                cmd.ExecuteNonQuery();
        }

        private static void AddColumnIfMissing(MySqlConnection conn, string table, string column, string ddl)
        {
            if (ColumnExists(conn, table, column)) return;

            using (var cmd = new MySqlCommand("ALTER TABLE " + table + " ADD COLUMN " + ddl, conn))
                cmd.ExecuteNonQuery();
        }
    }
}

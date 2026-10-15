using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;

namespace HospitalSystem.Data.Repositories
{
    public interface IBackupRepository
    {
        // Writes every table's rows to a SQL script that Restore can load back.
        void Export(TextWriter writer, string createdBy);

        // Replaces all data with the script's, in one transaction: either everything loads or nothing changes.
        void Import(string script);
    }

    public class MySqlBackupRepository : IBackupRepository
    {
        public const string Header = "-- HospitalSystem backup";

        // Every table the app uses. Foreign key checks are off while loading, so order only matters for readability.
        private static readonly string[] Tables =
        {
            "users", "departments", "doctors", "beds", "patients", "appointments", "admissions",
            "charge_schedules", "bills", "bill_items", "payments", "alerts", "activity_log"
        };

        private const int RowsPerInsert = 100;

        private readonly IDbConnectionFactory db;
        public MySqlBackupRepository(IDbConnectionFactory db) { this.db = db; }

        public void Export(TextWriter w, string createdBy)
        {
            w.WriteLine(Header);
            w.WriteLine("-- Created " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " by " + createdBy);
            w.WriteLine("-- Restore it from the app (Backup & Restore). It replaces ALL current data.");
            w.WriteLine("SET FOREIGN_KEY_CHECKS=0;");
            w.WriteLine("START TRANSACTION;");

            using (var conn = db.OpenConnection())
            {
                foreach (var table in Tables)
                    w.WriteLine("DELETE FROM `" + table + "`;");

                foreach (var table in Tables)
                    ExportTable(conn, table, w);
            }

            w.WriteLine("COMMIT;");
            w.WriteLine("SET FOREIGN_KEY_CHECKS=1;");
        }

        public void Import(string script)
        {
            if (script == null || !script.StartsWith(Header, StringComparison.Ordinal))
                throw new InvalidOperationException("That file is not a Hospital System backup.");

            using (var conn = db.OpenConnection())
            {
                try
                {
                    new MySqlScript(conn, script).Execute();
                }
                catch
                {
                    // Undo whatever part of the script ran before the error; the data stays as it was.
                    using (var rollback = new MySqlCommand("ROLLBACK; SET FOREIGN_KEY_CHECKS=1;", conn))
                        rollback.ExecuteNonQuery();
                    throw;
                }
            }
        }

        private static void ExportTable(MySqlConnection conn, string table, TextWriter w)
        {
            using (var cmd = new MySqlCommand("SELECT * FROM `" + table + "`", conn))
            using (var r = cmd.ExecuteReader())
            {
                var columns = new List<string>();
                for (int i = 0; i < r.FieldCount; i++)
                    columns.Add("`" + r.GetName(i) + "`");
                string insert = "INSERT INTO `" + table + "` (" + string.Join(", ", columns) + ") VALUES";

                var batch = new List<string>();
                while (r.Read())
                {
                    var values = new string[r.FieldCount];
                    for (int i = 0; i < r.FieldCount; i++)
                        values[i] = r.IsDBNull(i) ? "NULL" : Literal(r.GetValue(i));
                    batch.Add("(" + string.Join(", ", values) + ")");

                    if (batch.Count == RowsPerInsert)
                    {
                        w.WriteLine(insert + Environment.NewLine + string.Join("," + Environment.NewLine, batch) + ";");
                        batch.Clear();
                    }
                }
                if (batch.Count > 0)
                    w.WriteLine(insert + Environment.NewLine + string.Join("," + Environment.NewLine, batch) + ";");
            }
        }

        private static string Literal(object value)
        {
            switch (value)
            {
                case bool b: return b ? "1" : "0";
                case DateTime d: return "'" + d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "'";
                case decimal m: return m.ToString(CultureInfo.InvariantCulture);
                case double f: return f.ToString("R", CultureInfo.InvariantCulture);
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case sbyte _: case byte _: case short _: case ushort _: case int _: case uint _: case long _: case ulong _:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                default: return Quote(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static string Quote(string s)
        {
            var sb = new StringBuilder("'");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\'': sb.Append("''"); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\0': sb.Append("\\0"); break;
                    case ';': sb.Append("\\;"); break;   // keeps the script splitter from cutting a statement inside a string
                    default: sb.Append(c); break;
                }
            }
            return sb.Append("'").ToString();
        }
    }
}

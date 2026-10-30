using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data.Repositories
{
    public interface IPatientRepository
    {
        List<Patient> GetAll();
        void Insert(Patient p);
        void Update(Patient p);
        // Status plus who deactivated / reactivated the record and when.
        void UpdateStatus(Patient p);
        int NextId();
    }

    public class MySqlPatientRepository : IPatientRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlPatientRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Patient> GetAll()
        {
            var list = new List<Patient>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT id, full_name, age, birthdate, gender, contact, address, blood_type, status, registered_on, " +
                "deactivated_at, deactivated_by, deactivation_reason, deactivation_notes, reactivated_at, reactivated_by " +
                "FROM patients", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Patient
                    {
                        Id = r.GetInt32("id"),
                        FullName = r.GetString("full_name"),
                        // The SchemaMigrator fills birthdate for old rows; fall back to the age just in case.
                        Birthdate = r.IsDBNull(r.GetOrdinal("birthdate"))
                            ? DateTime.Today.AddYears(-r.GetInt32("age"))
                            : r.GetDateTime("birthdate"),
                        Gender = Sql.ReadString(r, "gender"),
                        Contact = Sql.ReadString(r, "contact"),
                        Address = Sql.ReadString(r, "address"),
                        BloodType = Sql.ReadString(r, "blood_type"),
                        Status = r.GetString("status"),
                        RegisteredOn = r.GetDateTime("registered_on"),
                        DeactivatedAt = ReadDate(r, "deactivated_at"),
                        DeactivatedBy = Sql.ReadString(r, "deactivated_by"),
                        DeactivationReason = Sql.ReadString(r, "deactivation_reason"),
                        DeactivationNotes = Sql.ReadString(r, "deactivation_notes"),
                        ReactivatedAt = ReadDate(r, "reactivated_at"),
                        ReactivatedBy = Sql.ReadString(r, "reactivated_by")
                    });
                }
            }
            return list;
        }

        public void Insert(Patient p)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO patients (full_name, age, birthdate, gender, contact, address, blood_type, status, registered_on) " +
                "VALUES (@fullName, @age, @birthdate, @gender, @contact, @address, @bloodType, @status, @registeredOn); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                AddRecordParameters(cmd, p);
                cmd.Parameters.AddWithValue("@status", p.Status);
                cmd.Parameters.AddWithValue("@registeredOn", p.RegisteredOn);
                p.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(Patient p)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE patients SET full_name=@fullName, age=@age, birthdate=@birthdate, gender=@gender, contact=@contact, " +
                "address=@address, blood_type=@bloodType WHERE id=@id", conn))
            {
                AddRecordParameters(cmd, p);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(Patient p)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE patients SET status=@status, deactivated_at=@deactivatedAt, deactivated_by=@deactivatedBy, " +
                "deactivation_reason=@reason, deactivation_notes=@notes, reactivated_at=@reactivatedAt, " +
                "reactivated_by=@reactivatedBy WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", p.Status);
                cmd.Parameters.AddWithValue("@deactivatedAt", Sql.OrNull(p.DeactivatedAt));
                cmd.Parameters.AddWithValue("@deactivatedBy", Sql.NullIfBlank(p.DeactivatedBy));
                cmd.Parameters.AddWithValue("@reason", Sql.NullIfBlank(p.DeactivationReason));
                cmd.Parameters.AddWithValue("@notes", Sql.NullIfBlank(p.DeactivationNotes));
                cmd.Parameters.AddWithValue("@reactivatedAt", Sql.OrNull(p.ReactivatedAt));
                cmd.Parameters.AddWithValue("@reactivatedBy", Sql.NullIfBlank(p.ReactivatedBy));
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public int NextId()
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("SELECT IFNULL(MAX(id), 0) + 1 FROM patients", conn))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static DateTime? ReadDate(MySqlDataReader r, string column) =>
            r.IsDBNull(r.GetOrdinal(column)) ? (DateTime?)null : r.GetDateTime(column);

        private static void AddRecordParameters(MySqlCommand cmd, Patient p)
        {
            cmd.Parameters.AddWithValue("@fullName", p.FullName);
            // age is kept for older reports and tools; birthdate is the source of truth.
            cmd.Parameters.AddWithValue("@age", p.Age ?? 0);
            cmd.Parameters.AddWithValue("@birthdate", Sql.OrNull(p.Birthdate?.Date));
            cmd.Parameters.AddWithValue("@gender", Sql.OrNull(p.Gender));
            cmd.Parameters.AddWithValue("@contact", Sql.OrNull(p.Contact));
            cmd.Parameters.AddWithValue("@address", Sql.OrNull(p.Address));
            cmd.Parameters.AddWithValue("@bloodType", Sql.OrNull(p.BloodType));
        }
    }
}

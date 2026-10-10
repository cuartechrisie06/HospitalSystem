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
                        Gender = Sql.ReadString(r, "gender"),
                        Contact = Sql.ReadString(r, "contact"),
                        Address = Sql.ReadString(r, "address"),
                        BloodType = Sql.ReadString(r, "blood_type"),
                        Status = r.GetString("status"),
                        RegisteredOn = r.GetDateTime("registered_on")
                    });
                }
            }
            return list;
        }

        public void Insert(Patient p)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO patients (full_name, age, gender, contact, address, blood_type, status, registered_on) " +
                "VALUES (@fullName, @age, @gender, @contact, @address, @bloodType, @status, @registeredOn); " +
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
                "UPDATE patients SET full_name=@fullName, age=@age, gender=@gender, contact=@contact, " +
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
            using (var cmd = new MySqlCommand("UPDATE patients SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", p.Status);
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

        private static void AddRecordParameters(MySqlCommand cmd, Patient p)
        {
            cmd.Parameters.AddWithValue("@fullName", p.FullName);
            cmd.Parameters.AddWithValue("@age", p.Age);
            cmd.Parameters.AddWithValue("@gender", Sql.OrNull(p.Gender));
            cmd.Parameters.AddWithValue("@contact", Sql.OrNull(p.Contact));
            cmd.Parameters.AddWithValue("@address", Sql.OrNull(p.Address));
            cmd.Parameters.AddWithValue("@bloodType", Sql.OrNull(p.BloodType));
        }
    }
}

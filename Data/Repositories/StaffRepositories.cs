using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data.Repositories
{
    public interface IUserRepository
    {
        List<User> GetAll();
    }

    public interface IDepartmentRepository
    {
        List<Department> GetAll();
    }

    public interface IDoctorRepository
    {
        List<Doctor> GetAll();
        void Insert(Doctor d);
        void Update(Doctor d);

        // Writes status, deactivation reason and duty flag together.
        void UpdateStatus(Doctor d);
    }

    public class MySqlUserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlUserRepository(IDbConnectionFactory db) { this.db = db; }

        public List<User> GetAll()
        {
            var list = new List<User>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("SELECT username, password, display_name, role FROM users", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new User
                    {
                        Username = r.GetString("username"),
                        Password = r.GetString("password"),
                        DisplayName = Sql.ReadString(r, "display_name"),
                        Role = Sql.ReadString(r, "role")
                    });
                }
            }
            return list;
        }
    }

    public class MySqlDepartmentRepository : IDepartmentRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlDepartmentRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Department> GetAll()
        {
            var list = new List<Department>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("SELECT id, name FROM departments", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Department { Id = r.GetInt32("id"), Name = r.GetString("name") });
            }
            return list;
        }
    }

    public class MySqlDoctorRepository : IDoctorRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlDoctorRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Doctor> GetAll()
        {
            var list = new List<Doctor>();
            using (var conn = db.OpenConnection())
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
                        Specialization = Sql.ReadString(r, "specialization"),
                        Contact = Sql.ReadString(r, "contact"),
                        LicenseNumber = Sql.ReadString(r, "license_number"),
                        Credentials = Sql.ReadString(r, "credentials"),
                        IsOnDuty = r.GetBoolean("is_on_duty"),
                        Status = r.GetString("status"),
                        DeactivationReason = Sql.ReadString(r, "deactivation_reason")
                    });
                }
            }
            return list;
        }

        public void Insert(Doctor d)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO doctors (full_name, department_id, specialization, contact, license_number, credentials, is_on_duty, status) " +
                "VALUES (@fullName, @departmentId, @specialization, @contact, @licenseNumber, @credentials, @isOnDuty, @status); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                AddRecordParameters(cmd, d);
                cmd.Parameters.AddWithValue("@status", d.Status);
                d.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(Doctor d)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE doctors SET full_name=@fullName, department_id=@departmentId, specialization=@specialization, " +
                "contact=@contact, license_number=@licenseNumber, credentials=@credentials, is_on_duty=@isOnDuty WHERE id=@id", conn))
            {
                AddRecordParameters(cmd, d);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(Doctor d)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE doctors SET status=@status, deactivation_reason=@reason, is_on_duty=@isOnDuty WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", d.Status);
                cmd.Parameters.AddWithValue("@reason", Sql.OrNull(d.DeactivationReason));
                cmd.Parameters.AddWithValue("@isOnDuty", d.IsOnDuty);
                cmd.Parameters.AddWithValue("@id", d.Id);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddRecordParameters(MySqlCommand cmd, Doctor d)
        {
            cmd.Parameters.AddWithValue("@fullName", d.FullName);
            cmd.Parameters.AddWithValue("@departmentId", d.DepartmentId);
            cmd.Parameters.AddWithValue("@specialization", Sql.OrNull(d.Specialization));
            cmd.Parameters.AddWithValue("@contact", Sql.OrNull(d.Contact));
            cmd.Parameters.AddWithValue("@licenseNumber", Sql.OrNull(d.LicenseNumber));
            cmd.Parameters.AddWithValue("@credentials", Sql.OrNull(d.Credentials));
            cmd.Parameters.AddWithValue("@isOnDuty", d.IsOnDuty);
        }
    }
}

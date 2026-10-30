using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data.Repositories
{
    public interface IAppointmentRepository
    {
        List<Appointment> GetAll();
        void Insert(Appointment a);
        void UpdateStatus(Appointment a);
        void UpdateSchedule(Appointment a);

        // Another open appointment for the doctor within the window, checked in the database
        // so bookings made on other workstations count too.
        bool HasClash(int doctorId, DateTime when, int ignoreId, int windowMinutes);
    }

    public class MySqlAppointmentRepository : IAppointmentRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlAppointmentRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Appointment> GetAll()
        {
            var list = new List<Appointment>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT id, patient_id, doctor_id, department_id, scheduled_on, reason, status FROM appointments", conn))
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
                        Reason = Sql.ReadString(r, "reason"),
                        Status = r.GetString("status")
                    });
                }
            }
            return list;
        }

        public void Insert(Appointment a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO appointments (patient_id, doctor_id, department_id, scheduled_on, reason, status) " +
                "VALUES (@patientId, @doctorId, @departmentId, @scheduledOn, @reason, @status); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@patientId", a.PatientId);
                cmd.Parameters.AddWithValue("@doctorId", a.DoctorId);
                cmd.Parameters.AddWithValue("@departmentId", a.DepartmentId);
                cmd.Parameters.AddWithValue("@scheduledOn", a.ScheduledOn);
                cmd.Parameters.AddWithValue("@reason", Sql.OrNull(a.Reason));
                cmd.Parameters.AddWithValue("@status", a.Status);
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateStatus(Appointment a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE appointments SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public bool HasClash(int doctorId, DateTime when, int ignoreId, int windowMinutes)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM appointments WHERE doctor_id=@doctorId AND id<>@ignoreId AND status<>@cancelled " +
                "AND scheduled_on > @from AND scheduled_on < @to", conn))
            {
                cmd.Parameters.AddWithValue("@doctorId", doctorId);
                cmd.Parameters.AddWithValue("@ignoreId", ignoreId);
                cmd.Parameters.AddWithValue("@cancelled", AppointmentStatus.Cancelled);
                cmd.Parameters.AddWithValue("@from", when.AddMinutes(-windowMinutes));
                cmd.Parameters.AddWithValue("@to", when.AddMinutes(windowMinutes));
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void UpdateSchedule(Appointment a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE appointments SET scheduled_on=@scheduledOn WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@scheduledOn", a.ScheduledOn);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

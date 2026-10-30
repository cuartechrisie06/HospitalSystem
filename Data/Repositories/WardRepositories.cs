using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data.Repositories
{
    public interface IBedRepository
    {
        List<Bed> GetAll();

        // Marks the bed occupied only if it is still free in the database. False when another
        // workstation took it first.
        bool TryOccupy(int bedId);

        void Release(int bedId);
    }

    public interface IAdmissionRepository
    {
        List<Admission> GetAll();
        void Insert(Admission a);

        // Bed, admitted time and status: a waitlisted patient getting a bed.
        void UpdateBedAssignment(Admission a);

        void UpdateDischarge(Admission a);
        void UpdateStatus(Admission a);

        // Admitted or waitlisted right now, checked in the database (another workstation may have added one).
        bool HasOpenAdmission(int patientId);
    }

    public class MySqlBedRepository : IBedRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlBedRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Bed> GetAll()
        {
            var list = new List<Bed>();
            using (var conn = db.OpenConnection())
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

        public bool TryOccupy(int bedId)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE beds SET is_occupied=1 WHERE id=@id AND is_occupied=0", conn))
            {
                cmd.Parameters.AddWithValue("@id", bedId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public void Release(int bedId)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE beds SET is_occupied=0 WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", bedId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class MySqlAdmissionRepository : IAdmissionRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlAdmissionRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Admission> GetAll()
        {
            var list = new List<Admission>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT id, patient_id, doctor_id, bed_id, admitted_on, requested_on, discharged_on, diagnosis, notes, status FROM admissions", conn))
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
                        Diagnosis = Sql.ReadString(r, "diagnosis"),
                        Notes = Sql.ReadString(r, "notes"),
                        Status = r.GetString("status")
                    });
                }
            }
            return list;
        }

        public void Insert(Admission a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO admissions (patient_id, doctor_id, bed_id, admitted_on, requested_on, discharged_on, diagnosis, notes, status) " +
                "VALUES (@patientId, @doctorId, @bedId, @admittedOn, @requestedOn, NULL, @diagnosis, @notes, @status); " +
                "SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@patientId", a.PatientId);
                cmd.Parameters.AddWithValue("@doctorId", a.DoctorId);
                cmd.Parameters.AddWithValue("@bedId", a.BedId > 0 ? (object)a.BedId : DBNull.Value);
                cmd.Parameters.AddWithValue("@admittedOn", a.AdmittedOn);
                cmd.Parameters.AddWithValue("@requestedOn", a.RequestedOn);
                cmd.Parameters.AddWithValue("@diagnosis", Sql.OrNull(a.Diagnosis));
                cmd.Parameters.AddWithValue("@notes", Sql.OrNull(a.Notes));
                cmd.Parameters.AddWithValue("@status", a.Status);
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateBedAssignment(Admission a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE admissions SET bed_id=@bedId, admitted_on=@admittedOn, status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@bedId", a.BedId);
                cmd.Parameters.AddWithValue("@admittedOn", a.AdmittedOn);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateDischarge(Admission a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE admissions SET discharged_on=@dischargedOn, status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@dischargedOn", a.DischargedOn);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(Admission a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("UPDATE admissions SET status=@status WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public bool HasOpenAdmission(int patientId)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM admissions WHERE patient_id=@patientId AND status IN (@active, @pending)", conn))
            {
                cmd.Parameters.AddWithValue("@patientId", patientId);
                cmd.Parameters.AddWithValue("@active", AdmissionStatus.Active);
                cmd.Parameters.AddWithValue("@pending", AdmissionStatus.Pending);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }
}

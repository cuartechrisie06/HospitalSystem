using System;
using System.Collections.Generic;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    public interface IPatientService
    {
        List<Patient> All();
        List<Patient> Active();
        Patient Get(int id);
        string NameOf(int id);
        int NextId();

        Patient Add(Patient p);
        void Update(Patient p);
        void Activate(Patient p);
        void Deactivate(Patient p);
    }

    public interface IDepartmentService
    {
        List<Department> All();
        Department Get(int id);
        string NameOf(int id);
    }

    public interface IDoctorService
    {
        List<Doctor> All();
        List<Doctor> Active();
        List<Doctor> OnDuty();
        Doctor Get(int id);
        string NameOf(int id);

        Doctor Add(Doctor d);
        void Update(Doctor d);

        // Soft-delete only: doctors are referenced by appointments/admissions (FK),
        // and losing a doctor's record should never erase that history. Deactivating
        // takes the doctor off duty and records why; the reason is kept while inactive.
        void Deactivate(Doctor d, string reason);
        void Activate(Doctor d);
    }

    public class PatientService : IPatientService
    {
        private readonly IPatientRepository repository;
        private readonly IActivityLogger log;
        private readonly IPermissionGuard guard;
        private readonly List<Patient> patients;

        public PatientService(IPatientRepository repository, IActivityLogger log, IPermissionGuard guard)
        {
            this.repository = repository;
            this.log = log;
            this.guard = guard;
            patients = repository.GetAll();
        }

        public List<Patient> All() => patients.ToList();

        public List<Patient> Active() => patients.Where(p => p.IsActive).ToList();

        public Patient Get(int id) => patients.FirstOrDefault(x => x.Id == id);

        public string NameOf(int id)
        {
            var p = Get(id);
            return p != null ? p.FullName : "(Unknown)";
        }

        public int NextId() => repository.NextId();

        public Patient Add(Patient p)
        {
            if (p.RegisteredOn == default)
                p.RegisteredOn = DateTime.Now;
            p.Status = "Active";

            repository.Insert(p);
            patients.Add(p);
            log.Log("Patients", "Registered", $"New patient registered: {p.FullName} ({p.PatientNo})", "👤");
            return p;
        }

        public void Update(Patient p)
        {
            repository.Update(p);
            log.Log("Patients", "Updated", $"Updated patient record: {p.FullName} ({p.PatientNo})", "👤");
        }

        public void Activate(Patient p)
        {
            guard.Demand(Permission.DeactivatePatients, "reactivate patient records");
            p.Status = "Active";
            repository.UpdateStatus(p);
            log.Log("Patients", "Activated", $"Activated patient: {p.FullName} ({p.PatientNo})", "👤");
        }

        public void Deactivate(Patient p)
        {
            guard.Demand(Permission.DeactivatePatients, "deactivate patient records");
            if (p == null) return;

            p.Status = "Inactive";
            repository.UpdateStatus(p);
            log.Log("Patients", "Deactivated", $"Deactivated patient: {p.FullName} ({p.PatientNo})", "👤");
        }
    }

    public class DepartmentService : IDepartmentService
    {
        private readonly List<Department> departments;

        public DepartmentService(IDepartmentRepository repository)
        {
            departments = repository.GetAll();
        }

        public List<Department> All() => departments.ToList();

        public Department Get(int id) => departments.FirstOrDefault(d => d.Id == id);

        public string NameOf(int id)
        {
            var d = Get(id);
            return d != null ? d.Name : "(Unknown)";
        }
    }

    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository repository;
        private readonly IDepartmentService departments;
        private readonly IActivityLogger log;
        private readonly IPermissionGuard guard;
        private readonly List<Doctor> doctors;

        public DoctorService(IDoctorRepository repository, IDepartmentService departments,
            IActivityLogger log, IPermissionGuard guard)
        {
            this.repository = repository;
            this.departments = departments;
            this.log = log;
            this.guard = guard;
            doctors = repository.GetAll();
        }

        public List<Doctor> All() => doctors.ToList();

        public List<Doctor> Active() => doctors.Where(d => d.IsActive).ToList();

        public List<Doctor> OnDuty() => doctors.Where(d => d.IsOnDuty && d.IsActive).ToList();

        public Doctor Get(int id) => doctors.FirstOrDefault(d => d.Id == id);

        public string NameOf(int id)
        {
            var d = Get(id);
            return d != null ? "Dr. " + d.FullName : "(Unknown)";
        }

        public Doctor Add(Doctor d)
        {
            guard.Demand(Permission.ManageDoctors, "add doctors");
            d.Status = "Active";

            repository.Insert(d);
            doctors.Add(d);
            log.Log("Doctors", "Added", $"New doctor added: Dr. {d.FullName} ({departments.NameOf(d.DepartmentId)})", "🩺");
            return d;
        }

        public void Update(Doctor d)
        {
            guard.Demand(Permission.ManageDoctors, "edit doctor records");
            repository.Update(d);
            log.Log("Doctors", "Updated", $"Dr. {d.FullName} updated ({(d.IsOnDuty ? "On Duty" : "Off Duty")})", "🩺");
        }

        public void Deactivate(Doctor d, string reason)
        {
            guard.Demand(Permission.ManageDoctors, "deactivate doctors");
            d.Status = "Inactive";
            d.DeactivationReason = reason;
            d.IsOnDuty = false;

            repository.UpdateStatus(d);
            log.Log("Doctors", "Deactivated", $"Deactivated doctor: Dr. {d.FullName} - {reason}", "🩺");
        }

        public void Activate(Doctor d)
        {
            guard.Demand(Permission.ManageDoctors, "reactivate doctors");
            d.Status = "Active";
            d.DeactivationReason = null;

            repository.UpdateStatus(d);
            log.Log("Doctors", "Reactivated", $"Reactivated doctor: Dr. {d.FullName}", "🩺");
        }
    }
}

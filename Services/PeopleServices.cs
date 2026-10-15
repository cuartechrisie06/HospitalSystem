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

        // Refused while the patient is admitted or on the waiting list.
        void Deactivate(Patient p, string reason = null, string notes = null);
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

    public class PatientService : IPatientService, IReloadable
    {
        private readonly IPatientRepository repository;
        private readonly IAdmissionRepository admissions;
        private readonly IUserSession session;
        private readonly IActivityLogger log;
        private readonly IPermissionGuard guard;
        private readonly List<Patient> patients;

        public PatientService(IPatientRepository repository, IAdmissionRepository admissions, IUserSession session,
            IActivityLogger log, IPermissionGuard guard)
        {
            this.repository = repository;
            this.admissions = admissions;
            this.session = session;
            this.log = log;
            this.guard = guard;
            patients = repository.GetAll();
        }

        public void Reload()
        {
            patients.Clear();
            patients.AddRange(repository.GetAll());
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
            Validate(p);
            if (p.RegisteredOn == default)
                p.RegisteredOn = DateTime.Now;
            p.Status = PatientStatus.Active;

            repository.Insert(p);
            patients.Add(p);
            log.Log("Patients", "Registered", $"New patient registered: {p.FullName} ({p.PatientNo})", "👤");
            return p;
        }

        public void Update(Patient p)
        {
            Validate(p);
            repository.Update(p);
            log.Log("Patients", "Updated", $"Updated patient record: {p.FullName} ({p.PatientNo})", "👤");
        }

        public void Activate(Patient p)
        {
            guard.Demand(Permission.DeactivatePatients, "reactivate patient records");
            p.Status = PatientStatus.Active;
            p.ReactivatedAt = DateTime.Now;
            p.ReactivatedBy = CurrentUserName();
            repository.UpdateStatus(p);
            log.Log("Patients", "Activated", $"Activated patient: {p.FullName} ({p.PatientNo})", "👤");
        }

        public void Deactivate(Patient p, string reason = null, string notes = null)
        {
            guard.Demand(Permission.DeactivatePatients, "deactivate patient records");
            if (p == null) return;
            Validation.Require(!admissions.HasOpenAdmission(p.Id),
                "This patient currently has an active admission and must be discharged before they can be deactivated.");

            p.Status = PatientStatus.Inactive;
            p.DeactivatedAt = DateTime.Now;
            p.DeactivatedBy = CurrentUserName();
            p.DeactivationReason = reason;
            p.DeactivationNotes = notes;
            repository.UpdateStatus(p);
            log.Log("Patients", "Deactivated",
                $"Deactivated patient: {p.FullName} ({p.PatientNo})" + (string.IsNullOrWhiteSpace(reason) ? "" : " - " + reason), "👤");
        }

        private string CurrentUserName() =>
            session.CurrentUser != null ? session.CurrentUser.DisplayName : ActivityItem.SystemUser;

        private static void Validate(Patient p)
        {
            Validation.RequireText(p.FullName, "Full Name is required.");
            Validation.Require(p.Birthdate.HasValue, "Birthdate is required.");
            Validation.Require(p.Birthdate.Value.Date <= DateTime.Today, "Birthdate cannot be in the future.");
            Validation.Require(p.Birthdate.Value.Date >= DateTime.Today.AddYears(-130), "Please check the birthdate.");
            Validation.RequireText(p.Gender, "Please select Gender (Male or Female).");
            Validation.RequireText(p.Contact, "Contact number is required.");
            Validation.Require(Validation.PhilippineMobile.IsMatch(p.Contact.Trim()), Validation.PhoneMessage);
        }
    }

    public class DepartmentService : IDepartmentService, IReloadable
    {
        private readonly IDepartmentRepository repository;
        private readonly List<Department> departments;

        public DepartmentService(IDepartmentRepository repository)
        {
            this.repository = repository;
            departments = repository.GetAll();
        }

        public void Reload()
        {
            departments.Clear();
            departments.AddRange(repository.GetAll());
        }

        public List<Department> All() => departments.ToList();

        public Department Get(int id) => departments.FirstOrDefault(d => d.Id == id);

        public string NameOf(int id)
        {
            var d = Get(id);
            return d != null ? d.Name : "(Unknown)";
        }
    }

    public class DoctorService : IDoctorService, IReloadable
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

        public void Reload()
        {
            doctors.Clear();
            doctors.AddRange(repository.GetAll());
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
            Validate(d);
            d.Status = DoctorStatus.Active;

            repository.Insert(d);
            doctors.Add(d);
            log.Log("Doctors", "Added", $"New doctor added: Dr. {d.FullName} ({departments.NameOf(d.DepartmentId)})", "🩺");
            return d;
        }

        public void Update(Doctor d)
        {
            guard.Demand(Permission.ManageDoctors, "edit doctor records");
            Validate(d);
            repository.Update(d);
            log.Log("Doctors", "Updated", $"Dr. {d.FullName} updated ({(d.IsOnDuty ? "On Duty" : "Off Duty")})", "🩺");
        }

        public void Deactivate(Doctor d, string reason)
        {
            guard.Demand(Permission.ManageDoctors, "deactivate doctors");
            Validation.RequireText(reason, "Please choose a reason for deactivation.");
            d.Status = DoctorStatus.Inactive;
            d.DeactivationReason = reason;
            d.IsOnDuty = false;

            repository.UpdateStatus(d);
            log.Log("Doctors", "Deactivated", $"Deactivated doctor: Dr. {d.FullName} - {reason}", "🩺");
        }

        public void Activate(Doctor d)
        {
            guard.Demand(Permission.ManageDoctors, "reactivate doctors");
            d.Status = DoctorStatus.Active;
            d.DeactivationReason = null;

            repository.UpdateStatus(d);
            log.Log("Doctors", "Reactivated", $"Reactivated doctor: Dr. {d.FullName}", "🩺");
        }

        private void Validate(Doctor d)
        {
            Validation.RequireText(d.FullName, "Full Name is required.");
            Validation.Require(departments.Get(d.DepartmentId) != null, "Please select a department.");
            Validation.Require(string.IsNullOrWhiteSpace(d.Contact) || Validation.PhilippineMobile.IsMatch(d.Contact.Trim()),
                Validation.PhoneMessage);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    public interface IBedService
    {
        List<Bed> All();
        List<Bed> Available();
        Bed Get(int id);
        string Label(int id);
        List<string> Wards();

        int OccupiedCount();
        int TotalCount();
        double OccupancyRate();
    }

    // Changing occupancy: only the admission workflow does this, so it's kept off IBedService.
    public interface IBedOccupancy
    {
        void SetOccupied(int bedId, bool occupied);
    }

    public interface IAppointmentService
    {
        List<Appointment> All();
        List<Appointment> Today();

        Appointment Add(Appointment a);
        void UpdateStatus(Appointment a, string status);
        void Cancel(Appointment a);
        void Complete(Appointment a);
        void Reschedule(Appointment a, DateTime newDate);

        // Same 30-minute window the Schedule button uses; ignoreId lets a reschedule skip itself.
        bool HasClash(int doctorId, DateTime when, int ignoreId = 0);

        int CountForPatient(int patientId);
        int CountForDoctor(int doctorId);
        int UpcomingCountForDoctor(int doctorId);
    }

    public interface IAdmissionService
    {
        List<Admission> All();
        List<Admission> Active();
        List<Admission> Pending();

        // With a bed (BedId > 0) the patient is admitted now. Without one (BedId == 0) the
        // admission goes on the waiting list as Pending until AssignBed is called.
        Admission Add(Admission a);

        // Moves a waitlisted patient into a free bed; the stay (and room charges) start now.
        void AssignBed(Admission a, int bedId);

        void Discharge(Admission a);

        // Admissions can't be cancelled; an admitted patient leaves only by discharge. A patient
        // still on the waiting list (no bed, no bill yet) can be taken off it, e.g. if they go elsewhere.
        void RemoveFromWaitingList(Admission a);

        int CountForPatient(int patientId);

        // Admitted or on the waiting list: either way the patient can't be admitted again or deactivated.
        bool HasActive(int patientId);

        // Raised after a discharge is saved. Billing listens to finalize room days on the bill.
        event Action<Admission> Discharged;
    }

    public class BedService : IBedService, IBedOccupancy
    {
        private readonly IBedRepository repository;
        private readonly List<Bed> beds;

        public BedService(IBedRepository repository)
        {
            this.repository = repository;
            beds = repository.GetAll();
        }

        public List<Bed> All() => beds.ToList();

        public List<Bed> Available() => beds.Where(b => !b.IsOccupied).ToList();

        public Bed Get(int id) => beds.FirstOrDefault(b => b.Id == id);

        public string Label(int id)
        {
            if (id <= 0) return "(waiting for bed)";
            var b = Get(id);
            return b != null ? b.Label : "(Unknown)";
        }

        public List<string> Wards() =>
            beds.Select(b => b.Ward).Where(w => !string.IsNullOrEmpty(w)).Distinct().OrderBy(w => w).ToList();

        public int OccupiedCount() => beds.Count(b => b.IsOccupied);

        public int TotalCount() => beds.Count;

        public double OccupancyRate() => TotalCount() == 0 ? 0 : (double)OccupiedCount() / TotalCount() * 100;

        public void SetOccupied(int bedId, bool occupied)
        {
            repository.SetOccupied(bedId, occupied);

            var bed = Get(bedId);
            if (bed != null)
                bed.IsOccupied = occupied;
        }
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository repository;
        private readonly IPatientService patients;
        private readonly IDoctorService doctors;
        private readonly IActivityLogger log;
        private readonly List<Appointment> appointments;

        public AppointmentService(IAppointmentRepository repository, IPatientService patients,
            IDoctorService doctors, IActivityLogger log)
        {
            this.repository = repository;
            this.patients = patients;
            this.doctors = doctors;
            this.log = log;
            appointments = repository.GetAll();
        }

        public List<Appointment> All() => appointments.ToList();

        public List<Appointment> Today() =>
            appointments.Where(a => a.ScheduledOn.Date == DateTime.Today && a.Status != "Cancelled").ToList();

        public Appointment Add(Appointment a)
        {
            if (string.IsNullOrEmpty(a.Status))
                a.Status = "Pending";

            repository.Insert(a);
            appointments.Add(a);
            log.Log("Appointments", "Scheduled",
                $"Appointment scheduled: {patients.NameOf(a.PatientId)} with {doctors.NameOf(a.DoctorId)}", "📅");
            return a;
        }

        public void UpdateStatus(Appointment a, string status)
        {
            a.Status = status;
            repository.UpdateStatus(a);

            string icon = status == "Confirmed" ? "✅" : status == "Cancelled" ? "❌" : "📅";
            log.Log("Appointments", status, $"{status} appointment for {patients.NameOf(a.PatientId)}", icon);
        }

        public void Cancel(Appointment a)
        {
            a.Cancel();
            UpdateStatus(a, a.Status);
        }

        public void Complete(Appointment a)
        {
            a.MarkAsCompleted();
            UpdateStatus(a, a.Status);
        }

        public void Reschedule(Appointment a, DateTime newDate)
        {
            DateTime oldDate = a.ScheduledOn;
            a.Reschedule(newDate);

            repository.UpdateSchedule(a);
            log.Log("Appointments", "Rescheduled",
                $"Rescheduled appointment for {patients.NameOf(a.PatientId)}: {oldDate:yyyy-MM-dd HH:mm} → {a.ScheduledOn:yyyy-MM-dd HH:mm}", "🔁");
        }

        public bool HasClash(int doctorId, DateTime when, int ignoreId = 0)
        {
            return appointments.Any(a =>
                a.Id != ignoreId &&
                a.DoctorId == doctorId &&
                a.Status != "Cancelled" &&
                Math.Abs((a.ScheduledOn - when).TotalMinutes) < 30);
        }

        public int CountForPatient(int patientId) => appointments.Count(a => a.PatientId == patientId);

        public int CountForDoctor(int doctorId) => appointments.Count(a => a.DoctorId == doctorId);

        public int UpcomingCountForDoctor(int doctorId) =>
            appointments.Count(a => a.DoctorId == doctorId && a.Status != "Cancelled" && a.ScheduledOn >= DateTime.Now);
    }

    public class AdmissionService : IAdmissionService
    {
        private readonly IAdmissionRepository repository;
        private readonly IBedService beds;
        private readonly IBedOccupancy occupancy;
        private readonly IPatientService patients;
        private readonly IActivityLogger log;
        private readonly List<Admission> admissions;

        public AdmissionService(IAdmissionRepository repository, IBedService beds, IBedOccupancy occupancy,
            IPatientService patients, IActivityLogger log)
        {
            this.repository = repository;
            this.beds = beds;
            this.occupancy = occupancy;
            this.patients = patients;
            this.log = log;
            admissions = repository.GetAll();
        }

        public event Action<Admission> Discharged;

        public List<Admission> All() => admissions.ToList();

        public List<Admission> Active() => admissions.Where(a => a.Status == "Active").ToList();

        public List<Admission> Pending() =>
            admissions.Where(a => a.IsPending).OrderBy(a => a.RequestedOn).ToList();

        public Admission Add(Admission a)
        {
            bool waitlisted = a.BedId <= 0;
            if (!waitlisted && !beds.Available().Any(b => b.Id == a.BedId))
                throw new InvalidOperationException("That bed is no longer available.");

            a.RequestedOn = DateTime.Now;
            a.AdmittedOn = a.RequestedOn;   // replaced when a bed is assigned to a waitlisted patient
            a.Status = waitlisted ? "Pending" : "Active";

            repository.Insert(a);
            if (!waitlisted)
                occupancy.SetOccupied(a.BedId, true);

            admissions.Add(a);

            if (waitlisted)
                log.Log("Admissions", "Waitlisted", $"{patients.NameOf(a.PatientId)} added to the admission waiting list ({a.AdmissionNo})", "⏳");
            else
                log.Log("Admissions", "Admitted", $"Admitted: {patients.NameOf(a.PatientId)} to {beds.Label(a.BedId)}", "🏥");
            return a;
        }

        public void AssignBed(Admission a, int bedId)
        {
            if (!beds.Available().Any(b => b.Id == bedId))
                throw new InvalidOperationException("That bed is no longer available.");

            TimeSpan waited = DateTime.Now - a.RequestedOn;
            a.AssignBed(bedId, DateTime.Now);

            repository.UpdateBedAssignment(a);
            occupancy.SetOccupied(a.BedId, true);

            log.Log("Admissions", "Bed Assigned",
                $"Admitted from waiting list: {patients.NameOf(a.PatientId)} to {beds.Label(a.BedId)} after waiting {DurationText.Format(waited)}", "🏥");
        }

        public void Discharge(Admission a)
        {
            if (a == null) return;
            a.Discharge(DateTime.Now);

            repository.UpdateDischarge(a);
            occupancy.SetOccupied(a.BedId, false);

            log.Log("Admissions", "Discharged", $"Discharged patient: {patients.NameOf(a.PatientId)} from {beds.Label(a.BedId)}", "🚪");

            Discharged?.Invoke(a);
        }

        public void RemoveFromWaitingList(Admission a)
        {
            a.RemoveFromWaitingList();
            repository.UpdateStatus(a);

            log.Log("Admissions", "Removed from Waiting List",
                $"Removed {patients.NameOf(a.PatientId)} from the admission waiting list ({a.AdmissionNo})", "❌");
        }

        public int CountForPatient(int patientId) => admissions.Count(a => a.PatientId == patientId);

        public bool HasActive(int patientId) =>
            admissions.Any(a => a.PatientId == patientId && (a.IsActive || a.IsPending));
    }
}

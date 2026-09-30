using System;

namespace HospitalSystem.Models
{
    public class Admission
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int BedId { get; set; }                // 0 while the patient is on the waiting list
        public DateTime AdmittedOn { get; set; }      // when a bed was assigned; the stay counts from here
        public DateTime RequestedOn { get; set; }     // when the admission was requested
        public DateTime? DischargedOn { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }   // Pending / Active / Discharged / Cancelled

        public string AdmissionNo
        {
            get { return "ADM-" + Id.ToString("D4"); }
        }

        public int DaysStayed => CalculateDaysStayed();

        public bool IsActive => Status == "Active";

        // On the waiting list: admission requested but no bed is free yet.
        public bool IsPending => Status == "Pending";

        public TimeSpan WaitingTime => (IsPending ? DateTime.Now : AdmittedOn) - RequestedOn;

        // These only change the object; HospitalData persists the change and frees the bed.
        public void AssignBed(int bedId, DateTime date)
        {
            if (!IsPending)
                throw new InvalidOperationException("Only a pending admission can be assigned a bed.");
            if (bedId <= 0)
                throw new InvalidOperationException("Please select an available bed.");

            BedId = bedId;
            AdmittedOn = date;
            Status = "Active";
        }

        public void Discharge(DateTime date)
        {
            if (!IsActive)
                throw new InvalidOperationException("Only an active admission can be discharged.");
            if (date < AdmittedOn)
                throw new InvalidOperationException("Discharge date cannot be earlier than the admission date.");

            DischargedOn = date;
            Status = "Discharged";
        }

        // For admissions entered by mistake, or a patient who leaves the waiting list.
        // Unlike a discharge, no stay is recorded.
        public void Cancel()
        {
            if (!IsActive && !IsPending)
                throw new InvalidOperationException("Only an active or pending admission can be cancelled.");

            Status = "Cancelled";
        }

        public int CalculateDaysStayed()
        {
            if (IsPending) return 0;
            DateTime end = DischargedOn.HasValue ? DischargedOn.Value : DateTime.Now;
            int days = (int)(end.Date - AdmittedOn.Date).TotalDays;
            return days < 1 ? 1 : days;
        }

        public override string ToString()
        {
            return AdmissionNo + " (" + (IsPending ? "waiting since " + RequestedOn.ToString("yyyy-MM-dd") : AdmittedOn.ToString("yyyy-MM-dd")) +
                ", " + Status + ")";
        }
    }
}

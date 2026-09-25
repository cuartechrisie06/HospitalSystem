using System;

namespace HospitalSystem.Models
{
    public class Admission
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int BedId { get; set; }
        public DateTime AdmittedOn { get; set; }
        public DateTime? DischargedOn { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }   // Active / Discharged / Cancelled

        public string AdmissionNo
        {
            get { return "ADM-" + Id.ToString("D4"); }
        }

        public int DaysStayed => CalculateDaysStayed();

        public bool IsActive => Status == "Active";

        // These only change the object; HospitalData persists the change and frees the bed.
        public void Discharge(DateTime date)
        {
            if (!IsActive)
                throw new InvalidOperationException("Only an active admission can be discharged.");
            if (date < AdmittedOn)
                throw new InvalidOperationException("Discharge date cannot be earlier than the admission date.");

            DischargedOn = date;
            Status = "Discharged";
        }

        // For admissions entered by mistake. Unlike a discharge, no stay is recorded.
        public void Cancel()
        {
            if (!IsActive)
                throw new InvalidOperationException("Only an active admission can be cancelled.");

            Status = "Cancelled";
        }

        public int CalculateDaysStayed()
        {
            DateTime end = DischargedOn.HasValue ? DischargedOn.Value : DateTime.Now;
            int days = (int)(end.Date - AdmittedOn.Date).TotalDays;
            return days < 1 ? 1 : days;
        }

        public override string ToString()
        {
            return AdmissionNo + " (" + AdmittedOn.ToString("yyyy-MM-dd") + ", " + Status + ")";
        }
    }
}

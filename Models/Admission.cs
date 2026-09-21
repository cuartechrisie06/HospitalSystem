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
        public string Status { get; set; }   // Active / Discharged

        public string AdmissionNo
        {
            get { return "ADM-" + Id.ToString("D4"); }
        }

        public int DaysStayed
        {
            get
            {
                DateTime end = DischargedOn.HasValue ? DischargedOn.Value : DateTime.Now;
                int days = (int)(end.Date - AdmittedOn.Date).TotalDays;
                return days < 1 ? 1 : days;
            }
        }
    }
}

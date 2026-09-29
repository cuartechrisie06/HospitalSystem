using System;

namespace HospitalSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime? Birthdate { get; set; }
        public string Gender { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string BloodType { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime RegisteredOn { get; set; }

        public DateTime? DeactivatedAt { get; set; }
        public string DeactivationReason { get; set; }
        public string DeactivationNotes { get; set; }
        public string DeactivatedBy { get; set; }

        public DateTime? ReactivatedAt { get; set; }
        public string ReactivatedBy { get; set; }

        public string PatientNo
        {
            get { return "P-" + Id.ToString("D4"); }
        }

        public bool IsActive => Status != "Inactive";

        public int? Age
        {
            get
            {
                if (!Birthdate.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - Birthdate.Value.Year;
                if (Birthdate.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public override string ToString()
        {
            return PatientNo + " - " + FullName;
        }
    }
}
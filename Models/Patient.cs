using System;

namespace HospitalSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string BloodType { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime RegisteredOn { get; set; }

        public string PatientNo
        {
            get { return "P-" + Id.ToString("D4"); }
        }

        public bool IsActive => Status != "Inactive";

        public override string ToString()
        {
            return PatientNo + " - " + FullName;
        }
    }
}

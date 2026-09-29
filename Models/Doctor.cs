namespace HospitalSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int DepartmentId { get; set; }
        public string Specialization { get; set; }
        public string Contact { get; set; }
        public string LicenseNumber { get; set; }
        public string Credentials { get; set; }
        public bool IsOnDuty { get; set; } = false;
        public string Status { get; set; } = "Active";

        // Set while the doctor is Inactive; holds the reason chosen at deactivation.
        public string DeactivationReason { get; set; }

        public string DoctorNo
        {
            get { return "D-" + Id.ToString("D4"); }
        }

        public bool IsActive => Status != "Inactive";

        public override string ToString()
        {
            return "Dr. " + FullName;
        }
    }
}
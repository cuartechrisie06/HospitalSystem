namespace HospitalSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int DepartmentId { get; set; }
        public string Specialization { get; set; }
        public bool IsOnDuty { get; set; } = false;   // ← bag-o

        public override string ToString()
        {
            return "Dr. " + FullName;
        }
    }
}
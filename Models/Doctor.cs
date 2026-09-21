namespace HospitalSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int DepartmentId { get; set; }
        public string Specialization { get; set; }

        public override string ToString()
        {
            return "Dr. " + FullName;
        }
    }
}

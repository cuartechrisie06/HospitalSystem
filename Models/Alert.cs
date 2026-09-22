using System;

namespace HospitalSystem.Models
{
    public class Alert
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }   // High, Medium, Low
        public DateTime CreatedOn { get; set; }
    }
}
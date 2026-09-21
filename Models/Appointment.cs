using System;

namespace HospitalSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int DepartmentId { get; set; }
        public DateTime ScheduledOn { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }   // Pending / Confirmed / Cancelled

        public string AppointmentNo
        {
            get { return "A-" + Id.ToString("D4"); }
        }
    }
}

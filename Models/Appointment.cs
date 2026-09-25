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
        public string Status { get; set; }   // Pending / Confirmed / Completed / Cancelled

        public string AppointmentNo
        {
            get { return "A-" + Id.ToString("D4"); }
        }

        public bool IsOpen => Status == "Pending" || Status == "Confirmed";

        // These only change the object; HospitalData persists the change.
        public void Reschedule(DateTime newDate)
        {
            if (!IsOpen)
                throw new InvalidOperationException("Only Pending or Confirmed appointments can be rescheduled.");
            if (newDate < DateTime.Now)
                throw new InvalidOperationException("The new date must be in the future.");

            ScheduledOn = newDate;
        }

        public void Cancel()
        {
            if (!IsOpen)
                throw new InvalidOperationException("Only Pending or Confirmed appointments can be cancelled.");

            Status = "Cancelled";
        }

        public void MarkAsCompleted()
        {
            if (Status != "Confirmed")
                throw new InvalidOperationException("Only Confirmed appointments can be marked as completed.");

            Status = "Completed";
        }

        public override string ToString()
        {
            return AppointmentNo + " (" + ScheduledOn.ToString("yyyy-MM-dd HH:mm") + ", " + Status + ")";
        }
    }
}

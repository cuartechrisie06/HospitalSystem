namespace HospitalSystem.Models
{
    // The status values stored in the database, in one place so a typo can't silently break a filter.

    public static class PatientStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
    }

    public static class DoctorStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
    }

    public static class AppointmentStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }

    public static class AdmissionStatus
    {
        public const string Pending = "Pending";         // on the waiting list, no bed yet
        public const string Active = "Active";           // admitted, occupying a bed
        public const string Discharged = "Discharged";
        public const string Cancelled = "Cancelled";     // taken off the waiting list
    }

    public static class AlertStatus
    {
        public const string Active = "Active";
        public const string Acknowledged = "Acknowledged";
        public const string Resolved = "Resolved";
    }
}

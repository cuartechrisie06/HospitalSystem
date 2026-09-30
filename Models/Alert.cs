using System;

namespace HospitalSystem.Models
{
    // Raised automatically by AlertMonitor when a condition in another module holds
    // (beds full, no doctor on duty, patients waiting for a bed...) and resolved
    // automatically when the condition clears.
    public class Alert
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }   // High, Medium, Low
        public string Status { get; set; } = "Active"; // Active, Acknowledged, Resolved
        public bool IsAuto { get; set; } = true;
        public DateTime CreatedOn { get; set; }

        public string SourceKey { get; set; }  // identifies the triggering condition, e.g. "STAFF:3"; null for old manual alerts
        public string Module { get; set; }     // module that raised it: Admissions, Doctors, Appointments, Billing
        public string AcknowledgedBy { get; set; }
        public DateTime? ResolvedOn { get; set; }

        public bool IsActive => Status == "Active";
        public bool IsAcknowledged => Status == "Acknowledged";

        public int SeverityRank => Severity == "High" ? 0 : Severity == "Medium" ? 1 : 2;
    }
}

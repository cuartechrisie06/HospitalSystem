using System;
using HospitalSystem.Models;

namespace HospitalSystem.Data
{
    // What each role may do. Checked through IPermissionGuard (Services/SecurityServices.cs).
    public enum Permission
    {
        ViewActivityLog,
        ManageDoctors,           // add / edit / deactivate / reactivate doctors, change duty status
        DeactivatePatients,      // deactivate / reactivate patient records
        CreateBills,             // manual bills (admission bills are created automatically for everyone)
                                 // (bills and admissions can't be cancelled by anyone)
        AddBillCharges,          // add medicines, supplies, procedures to a bill
        RemoveBillCharges,
        AdjustBills,             // discounts, senior/PWD, VAT, HMO coverage
        RecordPayments,
        ManageChargeSchedule,
        ViewFinancials           // hospital-wide outstanding balances and billing alerts
    }

    // The role policy itself: pure, no state, so it can be checked for any user.
    public static class Permissions
    {
        public const string AdminRole = "Administrator";
        public const string NurseRole = "Nurse";

        public static bool Can(User user, Permission permission)
        {
            if (user == null) return false;
            if (user.IsAdmin) return true;

            // Nurses (and any other role, least privilege): clinical work plus adding
            // charges for what the patient used. Money handling and records admin stay with admins.
            return permission == Permission.AddBillCharges;
        }
    }

    // An InvalidOperationException so the views' existing error handling shows it as a message.
    public class PermissionDeniedException : InvalidOperationException
    {
        public PermissionDeniedException(string message) : base(message) { }
    }
}

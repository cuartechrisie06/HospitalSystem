using System;
using HospitalSystem.Models;

namespace HospitalSystem.Data
{
    // What each role may do. Views use Can() to hide what the user can't do; HospitalData
    // calls Demand() on the protected operations, so a hidden button that slips through
    // still can't change anything (and the attempt is logged).
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

    public static class Permissions
    {
        public const string AdminRole = "Administrator";
        public const string NurseRole = "Nurse";

        public static bool Can(Permission permission) => Can(HospitalData.CurrentUser, permission);

        public static bool Can(User user, Permission permission)
        {
            if (user == null) return false;
            if (user.IsAdmin) return true;

            // Nurses (and any other role, least privilege): clinical work plus adding
            // charges for what the patient used. Money handling and records admin stay with admins.
            return permission == Permission.AddBillCharges;
        }

        public static void Demand(Permission permission, string action)
        {
            if (Can(permission)) return;

            var user = HospitalData.CurrentUser;
            HospitalData.LogActivity("Security", "Access Denied",
                $"{(user != null ? user.DisplayName + " (" + user.Role + ")" : "Nobody signed in")} tried to {action}", "⛔");
            throw new PermissionDeniedException("You don't have permission to " + action + ". Ask an administrator.");
        }
    }

    // An InvalidOperationException so the views' existing error handling shows it as a message.
    public class PermissionDeniedException : InvalidOperationException
    {
        public PermissionDeniedException(string message) : base(message) { }
    }
}

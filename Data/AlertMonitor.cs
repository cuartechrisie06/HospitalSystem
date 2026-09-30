using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data
{
    // Raises and resolves alerts from the state of the other modules; nobody creates alerts
    // by hand. Evaluate() runs after every logged module action (HospitalData.LogActivity),
    // at startup and when the dashboard opens (for time-based rules such as overdue appointments).
    //
    // Each rule yields a condition with a stable key. A new key raises an alert, a key that
    // is still present updates its alert's details, and a key that disappears resolves it.
    public static class AlertMonitor
    {
        public const string ModuleName = "Alerts";

        public const double HighOccupancyRate = 0.8;
        public static readonly TimeSpan LongWait = TimeSpan.FromHours(4);
        public static readonly TimeSpan AppointmentGrace = TimeSpan.FromHours(1);
        public const int HmoOverdueDays = 30;

        private static bool evaluating;

        private class Condition
        {
            public string Key, Module, Title, Message, Severity;
        }

        public static void Evaluate()
        {
            if (evaluating) return;   // raising an alert logs, and logging would evaluate again
            evaluating = true;
            try
            {
                var current = Conditions().ToDictionary(c => c.Key);

                foreach (var alert in HospitalData.Alerts.Where(a => a.SourceKey != null).ToList())
                {
                    Condition c;
                    if (current.TryGetValue(alert.SourceKey, out c))
                    {
                        if (alert.Title != c.Title || alert.Message != c.Message || alert.Severity != c.Severity)
                            HospitalData.UpdateAlert(alert, c.Title, c.Message, c.Severity);
                        current.Remove(alert.SourceKey);
                    }
                    else
                    {
                        HospitalData.AutoResolveAlert(alert);
                    }
                }

                foreach (var c in current.Values)
                    HospitalData.RaiseAlert(c.Key, c.Module, c.Title, c.Message, c.Severity);
            }
            catch (MySqlException)
            {
                // Alerts are advisory: a database hiccup here must never break the action that triggered it.
            }
            finally
            {
                evaluating = false;
            }
        }

        private static IEnumerable<Condition> Conditions()
        {
            return BedConditions()
                .Concat(WaitingListConditions())
                .Concat(StaffConditions())
                .Concat(AppointmentConditions())
                .Concat(BillingConditions());
        }

        // Admissions: overall occupancy and ICU capacity.
        private static IEnumerable<Condition> BedConditions()
        {
            var beds = HospitalData.Beds;
            int total = beds.Count;
            int free = beds.Count(b => !b.IsOccupied);

            if (total > 0 && free == 0)
                yield return new Condition
                {
                    Key = "BEDS:OCCUPANCY", Module = "Admissions", Severity = "High",
                    Title = "No beds available",
                    Message = $"All {total} beds are occupied. New admissions go to the waiting list."
                };
            else if (total > 0 && (double)(total - free) / total >= HighOccupancyRate)
                yield return new Condition
                {
                    Key = "BEDS:OCCUPANCY", Module = "Admissions", Severity = "Medium",
                    Title = "High bed occupancy",
                    Message = $"{total - free} of {total} beds occupied ({(double)(total - free) / total:P0}); {free} left."
                };

            var icu = beds.Where(b => b.Ward == "ICU").ToList();
            int icuFree = icu.Count(b => !b.IsOccupied);
            if (icu.Count > 0 && icuFree <= 1)
                yield return new Condition
                {
                    Key = "BEDS:ICU", Module = "Admissions", Severity = "High",
                    Title = icuFree == 0 ? "ICU full" : "ICU bed critical",
                    Message = icuFree == 0 ? $"All {icu.Count} ICU beds are occupied." : $"Only 1 of {icu.Count} ICU beds left."
                };
        }

        // Admissions: patients on the waiting list.
        private static IEnumerable<Condition> WaitingListConditions()
        {
            var waiting = HospitalData.PendingAdmissions();
            if (waiting.Count == 0) yield break;

            var longest = waiting.First();   // ordered by request time
            int free = HospitalData.AvailableBeds().Count;
            string message = $"{waiting.Count} patient(s) waiting for a bed; longest wait {HospitalData.FormatDuration(longest.WaitingTime)} " +
                             $"({HospitalData.PatientName(longest.PatientId)}).";
            if (free > 0)
                message += $" {free} bed(s) free now: assign them in Admissions.";

            yield return new Condition
            {
                Key = "ADMISSIONS:WAITING", Module = "Admissions",
                Severity = longest.WaitingTime >= LongWait || free > 0 ? "High" : "Medium",
                Title = "Patients waiting for admission",
                Message = message
            };
        }

        // Doctors: a department whose doctors are all off duty.
        private static IEnumerable<Condition> StaffConditions()
        {
            foreach (var dept in HospitalData.Departments)
            {
                var doctors = HospitalData.Doctors.Where(d => d.DepartmentId == dept.Id && d.IsActive).ToList();
                if (doctors.Count > 0 && !doctors.Any(d => d.IsOnDuty))
                    yield return new Condition
                    {
                        Key = "STAFF:" + dept.Id, Module = "Doctors", Severity = "Medium",
                        Title = "Staff shortage",
                        Message = $"{dept.Name} has no doctor on duty ({doctors.Count} off duty)."
                    };
            }
        }

        // Appointments: a doctor who is off duty or deactivated but still has patients booked
        // for the rest of today, and appointments whose time passed without being closed.
        private static IEnumerable<Condition> AppointmentConditions()
        {
            DateTime now = DateTime.Now;

            var remainingToday = HospitalData.Appointments
                .Where(a => a.IsOpen && a.ScheduledOn >= now && a.ScheduledOn.Date == now.Date);
            foreach (var group in remainingToday.GroupBy(a => a.DoctorId))
            {
                var doctor = HospitalData.GetDoctor(group.Key);
                if (doctor == null || (doctor.IsActive && doctor.IsOnDuty)) continue;

                var next = group.OrderBy(a => a.ScheduledOn).First();
                yield return new Condition
                {
                    Key = "APPT:DOCTOR:" + group.Key, Module = "Appointments", Severity = "Medium",
                    Title = "Doctor unavailable for booked patients",
                    Message = $"{HospitalData.DoctorName(group.Key)} is {(doctor.IsActive ? "off duty" : "deactivated")} " +
                              $"but has {group.Count()} appointment(s) left today (next {next.ScheduledOn:hh:mm tt}, {HospitalData.PatientName(next.PatientId)})."
                };
            }

            var overdue = HospitalData.Appointments
                .Where(a => a.IsOpen && a.ScheduledOn < now - AppointmentGrace)
                .OrderBy(a => a.ScheduledOn)
                .ToList();
            if (overdue.Count > 0)
                yield return new Condition
                {
                    Key = "APPT:OVERDUE", Module = "Appointments", Severity = "Low",
                    Title = "Appointments not closed",
                    Message = $"{overdue.Count} past appointment(s) still {string.Join("/", overdue.Select(a => a.Status).Distinct())}; " +
                              $"oldest {overdue[0].ScheduledOn:MMM dd hh:mm tt}. Mark them completed or cancelled."
                };
        }

        // Billing: patients discharged with an unpaid balance, and HMO coverage not collected in time.
        private static IEnumerable<Condition> BillingConditions()
        {
            var open = HospitalData.OpenBills().ToList();

            var dischargedUnpaid = open
                .Where(b => b.Balance > 0 && b.AdmissionId.HasValue &&
                            HospitalData.Admissions.Any(a => a.Id == b.AdmissionId.Value && a.Status == "Discharged"))
                .ToList();
            if (dischargedUnpaid.Count > 0)
                yield return new Condition
                {
                    Key = "BILLING:DISCHARGED_UNPAID", Module = "Billing", Severity = "Low",
                    Title = "Discharged with unpaid balance",
                    Message = $"{dischargedUnpaid.Count} discharged patient(s) owe {dischargedUnpaid.Sum(b => b.Balance):N2} in total."
                };

            var hmoOverdue = open
                .Where(b => b.HmoBalance > 0 && b.BillDate < DateTime.Now.AddDays(-HmoOverdueDays))
                .ToList();
            if (hmoOverdue.Count > 0)
                yield return new Condition
                {
                    Key = "BILLING:HMO_OVERDUE", Module = "Billing", Severity = "Low",
                    Title = "HMO payments overdue",
                    Message = $"{hmoOverdue.Sum(b => b.HmoBalance):N2} from HMOs is over {HmoOverdueDays} days old on {hmoOverdue.Count} bill(s)."
                };
        }
    }
}

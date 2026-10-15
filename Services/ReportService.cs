using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    public interface IReportService
    {
        // Per day: admissions, discharges, patients in house at the end of the day, occupancy,
        // new waiting-list requests and appointments.
        ReportTable DailyCensus(DateTime from, DateTime to);

        // Per day: bills issued and amounts billed vs collected, by payment method.
        // Needs the ViewFinancials permission.
        ReportTable Revenue(DateTime from, DateTime to);
    }

    public class ReportService : IReportService
    {
        public const int MaxDays = 366;

        private readonly IAdmissionService admissions;
        private readonly IAppointmentService appointments;
        private readonly IBedService beds;
        private readonly IBillingService billing;
        private readonly IPermissionGuard guard;
        private readonly IActivityLogger log;

        public ReportService(IAdmissionService admissions, IAppointmentService appointments, IBedService beds,
            IBillingService billing, IPermissionGuard guard, IActivityLogger log)
        {
            this.admissions = admissions;
            this.appointments = appointments;
            this.beds = beds;
            this.billing = billing;
            this.guard = guard;
            this.log = log;
        }

        public ReportTable DailyCensus(DateTime from, DateTime to)
        {
            var days = Days(from, to);
            int totalBeds = beds.TotalCount();
            var stays = admissions.All().Where(a => !a.IsPending && a.Status != AdmissionStatus.Cancelled).ToList();
            var allAdmissions = admissions.All();
            var appts = appointments.All().Where(a => a.Status != AppointmentStatus.Cancelled).ToList();

            var report = new ReportTable
            {
                Title = "Daily Census",
                Subtitle = Period(from, to),
                Columns = new[] { "Date", "Admitted", "Discharged", "In house", "Occupancy", "Waitlisted", "Appointments", "Completed" },
                RightAligned = new[] { false, true, true, true, true, true, true, true }
            };

            int sumAdmitted = 0, sumDischarged = 0, sumWaitlisted = 0, sumAppts = 0, sumCompleted = 0;
            foreach (var day in days)
            {
                DateTime end = day.AddDays(1);
                int admitted = stays.Count(a => a.AdmittedOn.Date == day);
                int discharged = stays.Count(a => a.DischargedOn.HasValue && a.DischargedOn.Value.Date == day);
                int inHouse = stays.Count(a => a.AdmittedOn < end && (!a.DischargedOn.HasValue || a.DischargedOn.Value >= end));
                // Requests that had to wait for a bed (admitted later, still waiting, or taken off the list).
                int waitlisted = allAdmissions.Count(a => a.RequestedOn.Date == day &&
                    (a.IsPending || a.Status == AdmissionStatus.Cancelled || a.AdmittedOn > a.RequestedOn));
                int booked = appts.Count(a => a.ScheduledOn.Date == day);
                int completed = appts.Count(a => a.ScheduledOn.Date == day && a.Status == AppointmentStatus.Completed);

                report.Rows.Add(new[]
                {
                    day.ToString("ddd, MMM dd yyyy"), N(admitted), N(discharged), N(inHouse),
                    totalBeds == 0 ? "-" : ((double)inHouse / totalBeds).ToString("P0"), N(waitlisted), N(booked), N(completed)
                });

                sumAdmitted += admitted; sumDischarged += discharged; sumWaitlisted += waitlisted;
                sumAppts += booked; sumCompleted += completed;
            }

            report.TotalRow = new[] { "Total", N(sumAdmitted), N(sumDischarged), "", "", N(sumWaitlisted), N(sumAppts), N(sumCompleted) };

            report.Notes.Add($"Now: {beds.OccupiedCount()} of {totalBeds} beds occupied ({beds.OccupancyRate():0}%), " +
                             $"{admissions.Pending().Count} patient(s) on the waiting list.");
            foreach (var ward in beds.All().GroupBy(b => b.Ward).OrderBy(g => g.Key))
                report.Notes.Add($"    {ward.Key}: {ward.Count(b => b.IsOccupied)} of {ward.Count()} occupied");

            log.Log("Reports", "Generated", $"Daily census for {report.Subtitle}", "📊");
            return report;
        }

        public ReportTable Revenue(DateTime from, DateTime to)
        {
            guard.Demand(Permission.ViewFinancials, "view revenue reports");
            var days = Days(from, to);
            var bills = billing.All().Where(b => b.Status != BillStatus.Cancelled).ToList();
            var payments = bills.SelectMany(b => b.Payments).ToList();

            var report = new ReportTable
            {
                Title = "Revenue by Day",
                Subtitle = Period(from, to),
                Columns = new[] { "Date", "Bills", "Billed", "Cash", "Card", "HMO", "Other", "Collected" },
                RightAligned = new[] { false, true, true, true, true, true, true, true }
            };

            int sumBills = 0;
            decimal sumBilled = 0, sumCash = 0, sumCard = 0, sumHmo = 0, sumOther = 0;
            foreach (var day in days)
            {
                var issued = bills.Where(b => b.BillDate.Date == day).ToList();
                var paid = payments.Where(p => p.PaymentDate.Date == day).ToList();
                decimal cash = Sum(paid, PaymentMethod.Cash), card = Sum(paid, PaymentMethod.Card), hmo = Sum(paid, PaymentMethod.HMO);
                decimal other = paid.Where(p => p.Method != PaymentMethod.Cash && p.Method != PaymentMethod.Card && p.Method != PaymentMethod.HMO)
                                    .Sum(p => p.Amount);
                decimal billed = issued.Sum(b => b.TotalAmount);

                report.Rows.Add(new[]
                {
                    day.ToString("ddd, MMM dd yyyy"), N(issued.Count), M(billed), M(cash), M(card), M(hmo), M(other), M(cash + card + hmo + other)
                });

                sumBills += issued.Count; sumBilled += billed; sumCash += cash; sumCard += card; sumHmo += hmo; sumOther += other;
            }

            report.TotalRow = new[]
            {
                "Total", N(sumBills), M(sumBilled), M(sumCash), M(sumCard), M(sumHmo), M(sumOther), M(sumCash + sumCard + sumHmo + sumOther)
            };

            report.Notes.Add("\"Billed\" is the amount due on bills dated that day; \"Collected\" is payments received that day on any bill.");
            report.Notes.Add($"Now outstanding: {M(billing.OutstandingBalance())} from patients, {M(billing.OutstandingHmo())} from HMOs.");

            log.Log("Reports", "Generated", $"Revenue report for {report.Subtitle}", "📊");
            return report;
        }

        private static List<DateTime> Days(DateTime from, DateTime to)
        {
            Validation.Require(from.Date <= to.Date, "The start date must be on or before the end date.");
            int count = (int)(to.Date - from.Date).TotalDays + 1;
            Validation.Require(count <= MaxDays, $"Please choose a period of at most {MaxDays} days.");
            return Enumerable.Range(0, count).Select(i => from.Date.AddDays(i)).ToList();
        }

        private static string Period(DateTime from, DateTime to) =>
            from.Date == to.Date ? from.ToString("MMMM dd, yyyy") : from.ToString("MMM dd, yyyy") + " to " + to.ToString("MMM dd, yyyy");

        private static decimal Sum(IEnumerable<Payment> payments, PaymentMethod method) =>
            payments.Where(p => p.Method == method).Sum(p => p.Amount);

        private static string N(int value) => value.ToString(CultureInfo.CurrentCulture);

        private static string M(decimal value) => value.ToString("N2");
    }
}

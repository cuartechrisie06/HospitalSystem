using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    public interface IChargeScheduleService
    {
        List<ChargeSchedule> All();
        ChargeSchedule Add(ChargeSchedule c);

        // Only affects bills generated from now on; existing bills keep the price they were issued with.
        void Update(ChargeSchedule c);
    }

    public interface IBillingService
    {
        List<Bill> All();
        Bill Get(int id);
        Bill OpenForAdmission(int admissionId);
        Bill OpenForAppointment(int appointmentId);
        List<Bill> Open();

        // Still owed by patients, across all bills.
        decimal OutstandingBalance();

        // Approved HMO coverage not yet received from the HMOs.
        decimal OutstandingHmo();

        decimal PatientOutstanding(int patientId);
        int PatientBillsWithBalance(int patientId);

        // Line items from the charge schedule that apply to the admission's ward,
        // with per-day charges covering the days stayed so far.
        List<BillItem> AdmissionChargesFor(Admission admission);

        // Starting line items for a bill: scheduled admission charges, or the consultation fee.
        List<BillItem> DefaultChargesFor(Admission admission, Appointment appointment);

        Bill Create(Bill b, IEnumerable<BillItem> items);

        // Admission -> billing trigger: guarantees every admission has an open bill with the
        // charges from the pre-set schedule for its ward. Returns the existing open bill if one is
        // already linked, so admitting never double-bills.
        Bill EnsureAdmissionBill(Admission admission);

        // On discharge, bring the per-day charges in line with the real length of stay.
        void SyncAdmissionBillOnDischarge(Admission admission);

        void AddItem(Bill b, BillItem item);
        void RemoveItem(Bill b, int itemId);
        void RecordPayment(Bill b, Payment p);

        // Replaces a bill's discount, senior citizen/PWD discount, VAT rate and HMO coverage.
        void UpdateAdjustments(Bill b, BillAdjustments adj);
    }

    public class ChargeScheduleService : IChargeScheduleService
    {
        private readonly IChargeScheduleRepository repository;
        private readonly IActivityLogger log;
        private readonly IPermissionGuard guard;
        private readonly List<ChargeSchedule> schedules;

        public ChargeScheduleService(IChargeScheduleRepository repository, IActivityLogger log, IPermissionGuard guard)
        {
            this.repository = repository;
            this.log = log;
            this.guard = guard;
            schedules = repository.GetAll();
        }

        public List<ChargeSchedule> All() => schedules.ToList();

        public ChargeSchedule Add(ChargeSchedule c)
        {
            guard.Demand(Permission.ManageChargeSchedule, "change the admission charge schedule");
            c.IsActive = true;

            repository.Insert(c);
            schedules.Add(c);
            log.Log("Billing", "Schedule Added", $"Added scheduled charge \"{c.Description}\" ({c.UnitPrice:N2}, {c.Basis})", "📋");
            return c;
        }

        public void Update(ChargeSchedule c)
        {
            guard.Demand(Permission.ManageChargeSchedule, "change the admission charge schedule");
            repository.Update(c);
            log.Log("Billing", "Schedule Updated",
                $"Scheduled charge \"{c.Description}\" updated ({c.UnitPrice:N2}, {c.Basis}{(c.IsActive ? "" : ", inactive")})", "📋");
        }
    }

    public class BillingService : IBillingService
    {
        private readonly IBillRepository repository;
        private readonly IChargeScheduleService charges;
        private readonly IBedService beds;
        private readonly IPatientService patients;
        private readonly IDoctorService doctors;
        private readonly IUserSession session;
        private readonly IPermissionGuard guard;
        private readonly IActivityLogger log;
        private readonly List<Bill> bills;

        public BillingService(IBillRepository repository, IChargeScheduleService charges, IBedService beds,
            IPatientService patients, IDoctorService doctors, IUserSession session, IPermissionGuard guard,
            IActivityLogger log)
        {
            this.repository = repository;
            this.charges = charges;
            this.beds = beds;
            this.patients = patients;
            this.doctors = doctors;
            this.session = session;
            this.guard = guard;
            this.log = log;
            bills = repository.GetAll();
        }

        public List<Bill> All() => bills.ToList();

        public Bill Get(int id) => bills.FirstOrDefault(b => b.Id == id);

        public Bill OpenForAdmission(int admissionId) =>
            bills.FirstOrDefault(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled);

        public Bill OpenForAppointment(int appointmentId) =>
            bills.FirstOrDefault(b => b.AppointmentId == appointmentId && b.Status != BillStatus.Cancelled);

        public List<Bill> Open() => bills.Where(b => b.Status != BillStatus.Cancelled).ToList();

        public decimal OutstandingBalance() => Open().Sum(b => b.Balance);

        public decimal OutstandingHmo() => Open().Sum(b => b.HmoBalance);

        public decimal PatientOutstanding(int patientId) =>
            Open().Where(b => b.PatientId == patientId).Sum(b => b.Balance);

        public int PatientBillsWithBalance(int patientId) =>
            Open().Count(b => b.PatientId == patientId && b.Balance > 0);

        public List<BillItem> AdmissionChargesFor(Admission admission)
        {
            var bed = beds.Get(admission.BedId);
            string ward = bed != null ? bed.Ward : null;
            int days = admission.CalculateDaysStayed();

            return charges.All()
                .Where(c => c.AppliesTo(ward))
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Description)
                .Select(c => c.ToBillItem(days))
                .ToList();
        }

        public List<BillItem> DefaultChargesFor(Admission admission, Appointment appointment)
        {
            var items = new List<BillItem>();

            if (admission != null)
                items.AddRange(AdmissionChargesFor(admission));

            if (appointment != null)
            {
                items.Add(new BillItem
                {
                    Description = "Consultation - " + doctors.NameOf(appointment.DoctorId),
                    Category = BillCategory.Consultation,
                    Quantity = 1,
                    UnitPrice = BillingRates.ConsultationFee
                });
            }

            return items;
        }

        public Bill Create(Bill b, IEnumerable<BillItem> items)
        {
            var user = session.CurrentUser;
            b.BillDate = b.BillDate == default ? DateTime.Now : b.BillDate;
            b.CreatedAt = DateTime.Now;
            b.CreatedBy = user != null ? user.Username : null;
            b.Status = BillStatus.Unpaid;
            // New bills start at the standard VAT rate; it can be changed per bill under Adjustments.
            if (b.Adjustments.VatRate == 0)
                b.Adjustments.VatRate = BillingRates.DefaultVatRate;
            foreach (var item in items)
                b.AddItem(item);
            b.CalculateTotal();
            b.CalculateBalance();

            repository.Insert(b);
            bills.Add(b);
            log.Log("Billing", "Created", $"Bill {b.BillNo} created for {patients.NameOf(b.PatientId)} ({b.TotalAmount:N2})", "🧾");
            return b;
        }

        public Bill EnsureAdmissionBill(Admission admission)
        {
            // Waitlisted: no bed means no ward and no room rate yet; the bill opens on AssignBed.
            if (admission == null || admission.IsPending) return null;

            var existing = OpenForAdmission(admission.Id);
            if (existing != null) return existing;

            var items = DefaultChargesFor(admission, null);
            return Create(new Bill
            {
                PatientId = admission.PatientId,
                AdmissionId = admission.Id,
                Notes = "Auto-generated on admission"
            }, items);
        }

        // The bill was opened at admit with 1 day. A deposit already paid doesn't block this:
        // the total only grows, so it never drops below what was paid.
        // Never lets a billing hiccup break the discharge itself.
        public void SyncAdmissionBillOnDischarge(Admission admission)
        {
            if (admission == null) return;

            var bill = OpenForAdmission(admission.Id);
            if (bill == null) return;

            int days = admission.CalculateDaysStayed();
            var changed = bill.SyncPerDayItems(days);
            if (changed.Count == 0) return;

            try
            {
                repository.UpdateItems(bill, changed);
                log.Log("Billing", "Updated",
                    $"Per-day charges on {bill.BillNo} updated to {days} day(s) at discharge ({bill.TotalAmount:N2})", "🧾");
            }
            catch (MySqlException)
            {
                // Discharge already succeeded; the bill can still be adjusted in Billing.
            }
        }

        public void AddItem(Bill b, BillItem item)
        {
            guard.Demand(Permission.AddBillCharges, "add charges to bills");
            b.AddItem(item);
            repository.InsertItem(b, item);
            log.Log("Billing", "Item Added", $"Added \"{item.Description}\" to {b.BillNo} ({item.Amount:N2})", "🧾");
        }

        public void RemoveItem(Bill b, int itemId)
        {
            guard.Demand(Permission.RemoveBillCharges, "remove charges from bills");
            var item = b.Items.FirstOrDefault(i => i.Id == itemId);
            b.RemoveItem(itemId);
            repository.DeleteItem(b, itemId);
            log.Log("Billing", "Item Removed", $"Removed \"{item.Description}\" from {b.BillNo}", "🧾");
        }

        public void RecordPayment(Bill b, Payment p)
        {
            guard.Demand(Permission.RecordPayments, "record payments");
            var user = session.CurrentUser;
            if (p.PaymentDate == default)
                p.PaymentDate = DateTime.Now;
            p.ReceivedBy = user != null ? user.DisplayName : null;
            b.ApplyPayment(p);

            repository.InsertPayment(b, p);
            log.Log("Billing", "Payment",
                $"{p.MethodLabel} payment of {p.Amount:N2} received for {b.BillNo}" +
                (string.IsNullOrEmpty(p.Details) ? "" : $" ({p.Details})") +
                $". Patient balance {b.Balance:N2}" + (b.HmoBalance > 0 ? $", HMO outstanding {b.HmoBalance:N2}" : ""), "💰");
        }

        public void UpdateAdjustments(Bill b, BillAdjustments adj)
        {
            guard.Demand(Permission.AdjustBills, "apply discounts, VAT or HMO coverage");
            b.ApplyAdjustments(adj);
            repository.SaveTotals(b);
            log.Log("Billing", "Adjusted",
                $"Adjusted {b.BillNo}: discounts {(b.DiscountAmount + b.StatutoryDiscountAmount):N2}, VAT {b.VatAmount:N2}, " +
                $"HMO {b.HmoAmount:N2}, amount due {b.TotalAmount:N2}", "🧾");
        }
    }
}

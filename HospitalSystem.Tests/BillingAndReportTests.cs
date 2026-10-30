using System;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Models;
using HospitalSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalSystem.Tests
{
    [TestClass]
    public class BillingServiceTests
    {
        private static Admission Admit(AppServices app, int patientId, int bedId) =>
            app.Admissions.Add(new Admission { PatientId = patientId, DoctorId = 1, BedId = bedId, Diagnosis = "Fever" });

        [TestMethod]
        public void AdmissionBillUsesTheScheduleForTheWard()
        {
            var app = TestApp.Start(out _);
            var bill = app.Billing.EnsureAdmissionBill(Admit(app, 1, 7));   // bed 7: Ward

            CollectionAssert.AreEquivalent(new[] { "Admission fee", "Room - Ward" }, bill.Items.Select(i => i.Description).ToArray());
            Assert.AreEqual(BillingRates.DefaultVatRate, bill.Adjustments.VatRate);
            Assert.AreEqual(BillStatus.Unpaid, bill.Status);
        }

        [TestMethod]
        public void IcuBedGetsTheIcuRateAndAdmittingNeverDoubleBills()
        {
            var app = TestApp.Start(out _);
            var admission = Admit(app, 1, 13);   // bed 13: ICU
            var bill = app.Billing.EnsureAdmissionBill(admission);
            Assert.IsTrue(bill.Items.Any(i => i.Description == "Room - ICU"));
            Assert.AreSame(bill, app.Billing.EnsureAdmissionBill(admission));
            Assert.AreEqual(1, app.Billing.All().Count);
        }

        [TestMethod]
        public void SemiPrivateBedGetsTheSemiPrivateRate()
        {
            var app = TestApp.Start(out _);
            var bill = app.Billing.EnsureAdmissionBill(Admit(app, 1, 3));   // bed 3: Semi-Private
            var room = bill.Items.Single(i => i.Category == BillCategory.Room);
            Assert.AreEqual(BedLayout.SemiPrivateRate, room.UnitPrice);
        }

        [TestMethod]
        public void LayoutHasTheAgreedRooms()
        {
            var app = TestApp.Start(out _);
            var beds = app.Beds.All();
            Assert.AreEqual(16, beds.Count);
            Func<string, int[]> bedsPerRoom = ward => beds.Where(b => b.Ward == ward).GroupBy(b => b.RoomNo).Select(g => g.Count()).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 1 }, bedsPerRoom(BedLayout.Private));
            CollectionAssert.AreEqual(new[] { 2, 2 }, bedsPerRoom(BedLayout.SemiPrivate));
            CollectionAssert.AreEqual(new[] { 3, 3 }, bedsPerRoom(BedLayout.Ward));
            CollectionAssert.AreEqual(new[] { 2, 2 }, bedsPerRoom(BedLayout.Icu));
        }

        [TestMethod]
        public void WaitlistedAdmissionHasNoBillUntilItGetsABed()
        {
            var app = TestApp.Start(out _);
            var a = app.Admissions.Add(new Admission { PatientId = 2, DoctorId = 1, BedId = 0, Diagnosis = "Obs" });
            Assert.IsNull(app.Billing.EnsureAdmissionBill(a));
        }

        [TestMethod]
        public void ItemsArePricedAndValidated()
        {
            var app = TestApp.Start(out _);
            var bill = app.Billing.EnsureAdmissionBill(Admit(app, 1, 1));
            decimal before = bill.Subtotal;

            app.Billing.AddItem(bill, new BillItem { Description = "Paracetamol", Category = BillCategory.Medicine, Quantity = 3, UnitPrice = 12.50m });
            Assert.AreEqual(before + 37.50m, bill.Subtotal);

            Assert.ThrowsException<ValidationException>(() => app.Billing.AddItem(bill, new BillItem { Description = "", Quantity = 1, UnitPrice = 1 }));
            Assert.ThrowsException<ValidationException>(() => app.Billing.AddItem(bill, new BillItem { Description = "X", Quantity = 0, UnitPrice = 1 }));
            Assert.ThrowsException<ValidationException>(() => app.Billing.AddItem(bill, new BillItem { Description = "X", Quantity = 1, UnitPrice = -1 }));
        }

        [TestMethod]
        public void PaymentReducesTheBalanceAndIsCreditedToTheCashier()
        {
            var app = TestApp.Start(out _);
            var bill = app.Billing.EnsureAdmissionBill(Admit(app, 1, 7));   // Ward
            decimal due = bill.Balance;

            var payment = new Payment { BillId = bill.Id, Amount = 1000m, Method = PaymentMethod.Cash, AmountTendered = 1000m };
            app.Billing.RecordPayment(bill, payment);
            Assert.AreEqual(due - 1000m, bill.Balance);
            Assert.AreEqual("System Administrator", payment.ReceivedBy);
            Assert.AreEqual(due - 1000m, app.Billing.PatientOutstanding(1));
        }

        [TestMethod]
        public void NurseCanAddChargesButNotTakePaymentsOrDiscount()
        {
            var app = TestApp.Start(out _, signInAs: "nurse");
            var bill = app.Billing.EnsureAdmissionBill(Admit(app, 1, 1));

            app.Billing.AddItem(bill, new BillItem { Description = "Gauze", Category = BillCategory.Other, Quantity = 1, UnitPrice = 20m });
            Assert.ThrowsException<PermissionDeniedException>(() =>
                app.Billing.RecordPayment(bill, new Payment { BillId = bill.Id, Amount = 10m, Method = PaymentMethod.Cash }));
            Assert.ThrowsException<PermissionDeniedException>(() =>
                app.Billing.UpdateAdjustments(bill, new BillAdjustments { DiscountValue = 10, DiscountReason = "x", VatRate = 12 }));
        }

        [TestMethod]
        public void SeniorDiscountAndVatFollowTheRules()
        {
            var app = TestApp.Start(out _);
            var bill = app.Billing.EnsureAdmissionBill(Admit(app, 2, 1));   // patient 2 is 70
            decimal plain = bill.TotalAmount;

            Assert.ThrowsException<InvalidOperationException>(() =>
                app.Billing.UpdateAdjustments(bill, new BillAdjustments { Eligibility = DiscountEligibility.SeniorCitizen, VatRate = 12 }));

            app.Billing.UpdateAdjustments(bill, new BillAdjustments { Eligibility = DiscountEligibility.SeniorCitizen, EligibilityIdNo = "OSCA-1", VatRate = 12 });
            Assert.IsTrue(bill.StatutoryDiscountAmount > 0);
            Assert.IsTrue(bill.TotalAmount < plain);
        }
    }

    [TestClass]
    public class AlertTests
    {
        [TestMethod]
        public void FillingEveryBedRaisesAnAlertThatClearsAfterADischarge()
        {
            var app = TestApp.Start(out var repos);
            for (int i = 1; i <= 16; i++)
            {
                if (i > 3)
                    app.Patients.Add(new Patient { FullName = "P" + i, Birthdate = DateTime.Today.AddYears(-30), Gender = "Male", Contact = "09170000000" });
                app.Admissions.Add(new Admission { PatientId = i, DoctorId = 1, BedId = i, Diagnosis = "x" });
            }

            var alert = app.Alerts.Active().SingleOrDefault(a => a.SourceKey == "BEDS:OCCUPANCY");
            Assert.IsNotNull(alert);
            Assert.AreEqual("No beds available", alert.Title);

            app.Admissions.Discharge(app.Admissions.Active().First());
            Assert.IsFalse(app.Alerts.Open().Any(a => a.SourceKey == "BEDS:OCCUPANCY" && a.Title == "No beds available"));
        }
    }
}

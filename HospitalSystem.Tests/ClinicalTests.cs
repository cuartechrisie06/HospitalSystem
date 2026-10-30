using System;
using System.Linq;
using HospitalSystem.Models;
using HospitalSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalSystem.Tests
{
    [TestClass]
    public class PatientServiceTests
    {
        private static Patient Valid() => new Patient
        {
            FullName = "Ana Cruz", Birthdate = DateTime.Today.AddYears(-30), Gender = "Female", Contact = "09171112222"
        };

        [TestMethod]
        public void ValidPatientIsRegisteredAsActive()
        {
            var app = TestApp.Start(out _);
            var p = app.Patients.Add(Valid());
            Assert.IsTrue(p.Id > 0);
            Assert.AreEqual(PatientStatus.Active, p.Status);
            Assert.AreEqual(30, p.Age);
        }

        [TestMethod]
        public void InvalidDataIsRefusedByTheServiceItself()
        {
            var app = TestApp.Start(out _);
            Action<Action<Patient>> refused = change =>
            {
                var p = Valid();
                change(p);
                Assert.ThrowsException<ValidationException>(() => app.Patients.Add(p));
            };

            refused(p => p.FullName = "  ");
            refused(p => p.Birthdate = DateTime.Today.AddDays(1));
            refused(p => p.Birthdate = null);
            refused(p => p.Gender = null);
            refused(p => p.Contact = "12345");
            refused(p => p.Contact = "");
            Assert.AreEqual(3, app.Patients.All().Count);
        }

        [TestMethod]
        public void DeactivationIsRefusedWhileAdmittedAndRecordsWhoAndWhyOtherwise()
        {
            var app = TestApp.Start(out _);
            var patient = app.Patients.Get(1);
            var admission = app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 1, Diagnosis = "Fever" });
            Assert.ThrowsException<ValidationException>(() => app.Patients.Deactivate(patient, "Transferred"));

            app.Admissions.Discharge(admission);
            app.Patients.Deactivate(patient, "Transferred");
            Assert.IsFalse(patient.IsActive);
            Assert.AreEqual("System Administrator", patient.DeactivatedBy);
            Assert.AreEqual("Transferred", patient.DeactivationReason);
            Assert.IsNotNull(patient.DeactivatedAt);

            app.Patients.Activate(patient);
            Assert.IsTrue(patient.IsActive);
            Assert.AreEqual("System Administrator", patient.ReactivatedBy);
        }
    }

    [TestClass]
    public class AppointmentServiceTests
    {
        private static Appointment Booking(DateTime when, int patientId = 1, int doctorId = 1) =>
            new Appointment { PatientId = patientId, DoctorId = doctorId, DepartmentId = 1, ScheduledOn = when };

        [TestMethod]
        public void DoctorCantBeBookedTwiceWithin30Minutes()
        {
            var app = TestApp.Start(out _);
            var when = DateTime.Today.AddDays(1).AddHours(9);
            app.Appointments.Add(Booking(when));

            Assert.ThrowsException<ValidationException>(() => app.Appointments.Add(Booking(when.AddMinutes(20), patientId: 2)));
            app.Appointments.Add(Booking(when.AddMinutes(30), patientId: 2));       // exactly 30 minutes later is fine
            app.Appointments.Add(Booking(when.AddMinutes(10), patientId: 3, doctorId: 2));   // another doctor is fine
        }

        [TestMethod]
        public void BookingMadeOnAnotherWorkstationStillBlocksTheSlot()
        {
            var app = TestApp.Start(out var repos);
            var when = DateTime.Today.AddDays(1).AddHours(9);
            // Written straight to the "database", so this workstation's memory doesn't know about it.
            repos.AppointmentStore.Insert(Booking(when, patientId: 2));

            Assert.ThrowsException<ValidationException>(() => app.Appointments.Add(Booking(when)));
        }

        [TestMethod]
        public void CancelledAppointmentFreesTheSlot()
        {
            var app = TestApp.Start(out _);
            var when = DateTime.Today.AddDays(1).AddHours(9);
            var first = app.Appointments.Add(Booking(when));
            app.Appointments.Cancel(first);
            app.Appointments.Add(Booking(when, patientId: 2));
        }

        [TestMethod]
        public void InactivePeopleAndPastDatesAreRefused()
        {
            var app = TestApp.Start(out _);
            var when = DateTime.Today.AddDays(1).AddHours(9);

            app.Patients.Deactivate(app.Patients.Get(3));
            Assert.ThrowsException<ValidationException>(() => app.Appointments.Add(Booking(when, patientId: 3)));

            app.Doctors.Deactivate(app.Doctors.Get(2), "On leave");
            Assert.ThrowsException<ValidationException>(() => app.Appointments.Add(Booking(when, doctorId: 2)));

            Assert.ThrowsException<ValidationException>(() => app.Appointments.Add(Booking(DateTime.Today.AddDays(-1))));
        }

        [TestMethod]
        public void RescheduleChecksForClashesButNotWithItself()
        {
            var app = TestApp.Start(out _);
            var nine = DateTime.Today.AddDays(1).AddHours(9);
            var a = app.Appointments.Add(Booking(nine));
            app.Appointments.Add(Booking(nine.AddHours(2), patientId: 2));

            app.Appointments.Reschedule(a, nine.AddMinutes(15));   // overlaps only its own old slot
            Assert.ThrowsException<ValidationException>(() => app.Appointments.Reschedule(a, nine.AddHours(2).AddMinutes(10)));
        }
    }

    [TestClass]
    public class AdmissionServiceTests
    {
        [TestMethod]
        public void AdmittingOccupiesTheBedAndDischargeFreesIt()
        {
            var app = TestApp.Start(out var repos);
            var a = app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 1, Diagnosis = "Fever" });
            Assert.AreEqual(AdmissionStatus.Active, a.Status);
            Assert.IsTrue(repos.BedStore.Table.Get(1).IsOccupied);
            Assert.AreEqual(app.Beds.TotalCount() - 1, app.Beds.Available().Count);

            app.Admissions.Discharge(a);
            Assert.IsFalse(repos.BedStore.Table.Get(1).IsOccupied);
            Assert.AreEqual(app.Beds.TotalCount(), app.Beds.Available().Count);
        }

        [TestMethod]
        public void BedTakenOnAnotherWorkstationIsRefusedAndNothingIsSaved()
        {
            var app = TestApp.Start(out var repos);
            repos.BedStore.Table.Get(1).IsOccupied = true;   // another workstation admitted someone here

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 1, Diagnosis = "Fever" }));
            StringAssert.Contains(ex.Message, "no longer available");
            Assert.AreEqual(0, repos.AdmissionStore.Table.Rows.Count);
            Assert.IsTrue(app.Beds.Get(1).IsOccupied, "local copy now shows the bed as taken");
        }

        [TestMethod]
        public void PatientCantBeAdmittedTwiceOrWithoutADiagnosis()
        {
            var app = TestApp.Start(out _);
            Assert.ThrowsException<ValidationException>(() =>
                app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 1, Diagnosis = " " }));

            app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 1, Diagnosis = "Fever" });
            Assert.ThrowsException<ValidationException>(() =>
                app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 2, Diagnosis = "Fever" }));
            Assert.IsFalse(app.Beds.Get(2).IsOccupied);
        }

        [TestMethod]
        public void WaitingListThenBedAssignment()
        {
            var app = TestApp.Start(out _);
            var a = app.Admissions.Add(new Admission { PatientId = 2, DoctorId = 1, BedId = 0, Diagnosis = "Observation" });
            Assert.IsTrue(a.IsPending);
            Assert.AreEqual(1, app.Admissions.Pending().Count);

            app.Admissions.AssignBed(a, 3);
            Assert.IsTrue(a.IsActive);
            Assert.IsTrue(app.Beds.Get(3).IsOccupied);
            Assert.ThrowsException<InvalidOperationException>(() => app.Admissions.AssignBed(a, 4));
        }

        [TestMethod]
        public void DischargeRaisesTheEventBillingListensTo()
        {
            var app = TestApp.Start(out _);
            var a = app.Admissions.Add(new Admission { PatientId = 1, DoctorId = 1, BedId = 1, Diagnosis = "Fever" });
            Admission seen = null;
            app.Admissions.Discharged += x => seen = x;
            app.Admissions.Discharge(a);
            Assert.AreSame(a, seen);
        }
    }
}

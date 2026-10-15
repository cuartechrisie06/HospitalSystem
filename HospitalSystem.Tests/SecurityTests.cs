using System;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Models;
using HospitalSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalSystem.Tests
{
    [TestClass]
    public class PasswordHasherTests
    {
        [TestMethod]
        public void HashVerifiesOnlyTheRightPassword()
        {
            string hash = PasswordHasher.Hash("s3cret!");
            Assert.IsTrue(PasswordHasher.IsHashed(hash));
            Assert.IsTrue(PasswordHasher.Verify("s3cret!", hash));
            Assert.IsFalse(PasswordHasher.Verify("S3cret!", hash));
            Assert.IsFalse(PasswordHasher.Verify("", hash));
        }

        [TestMethod]
        public void SamePasswordGetsADifferentSaltEachTime()
        {
            Assert.AreNotEqual(PasswordHasher.Hash("admin"), PasswordHasher.Hash("admin"));
        }

        [TestMethod]
        public void PlainTextIsNeverAcceptedAsAHash()
        {
            Assert.IsFalse(PasswordHasher.IsHashed("admin"));
            Assert.IsFalse(PasswordHasher.Verify("admin", "admin"));
            Assert.IsFalse(PasswordHasher.Verify("admin", "pbkdf2-sha256$broken"));
        }
    }

    [TestClass]
    public class AuthServiceTests
    {
        [TestMethod]
        public void PlainTextPasswordsAreHashedOnStartup()
        {
            TestApp.Start(out var repos, signInAs: null);
            Assert.IsTrue(repos.UserStore.Rows.All(u => PasswordHasher.IsHashed(u.Password)));
            Assert.IsTrue(repos.LogStore.Has("Security", "Passwords Hashed"));
        }

        [TestMethod]
        public void SignInWorksWithTheOriginalPasswordAfterHashing()
        {
            var app = TestApp.Start(out _, signInAs: null);
            Assert.IsNotNull(app.Auth.SignIn("ADMIN", "admin"));
            Assert.IsTrue(app.Session.IsAdmin);
        }

        [TestMethod]
        public void WrongPasswordAndUnknownUserAreRejectedAndLogged()
        {
            var app = TestApp.Start(out var repos, signInAs: null);
            Assert.IsNull(app.Auth.SignIn("admin", "nope"));
            Assert.IsNull(app.Auth.SignIn("ghost", "x"));
            Assert.AreEqual(2, repos.LogStore.Rows.Count(r => r.Action == "Sign-in Failed"));
            Assert.AreEqual(1, repos.UserStore.Rows.Single(u => u.Username == "admin").FailedAttempts);
        }

        [TestMethod]
        public void FiveFailuresLockTheAccountEvenWithTheRightPassword()
        {
            var app = TestApp.Start(out var repos, signInAs: null);
            for (int i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
                Assert.IsNull(app.Auth.SignIn("nurse", "wrong"));

            Assert.ThrowsException<AccountLockedException>(() => app.Auth.SignIn("nurse", "wrong"));
            Assert.ThrowsException<AccountLockedException>(() => app.Auth.SignIn("nurse", "nurse"));
            Assert.IsNull(app.Session.CurrentUser);
            Assert.IsTrue(repos.LogStore.Has("Security", "Account Locked"));
        }

        [TestMethod]
        public void LockExpiresAndSuccessResetsTheCounter()
        {
            var app = TestApp.Start(out var repos, signInAs: null);
            var row = repos.UserStore.Rows.Single(u => u.Username == "nurse");
            row.LockedUntil = DateTime.Now.AddMinutes(-1);
            row.FailedAttempts = 3;

            Assert.IsNotNull(app.Auth.SignIn("nurse", "nurse"));
            Assert.AreEqual(0, row.FailedAttempts);
            Assert.IsNull(row.LockedUntil);
        }
    }

    [TestClass]
    public class PermissionTests
    {
        [TestMethod]
        public void NurseHasOnlyClinicalWorkAndAddingCharges()
        {
            var nurse = new User { Role = Permissions.NurseRole };
            foreach (Permission p in Enum.GetValues(typeof(Permission)))
                Assert.AreEqual(p == Permission.AddBillCharges, Permissions.Can(nurse, p), p.ToString());
        }

        [TestMethod]
        public void AdminCanDoEverythingAndNobodySignedInCanDoNothing()
        {
            var admin = new User { Role = Permissions.AdminRole };
            foreach (Permission p in Enum.GetValues(typeof(Permission)))
            {
                Assert.IsTrue(Permissions.Can(admin, p));
                Assert.IsFalse(Permissions.Can(null, p));
            }
        }

        [TestMethod]
        public void RefusedActionThrowsAndIsLogged()
        {
            var app = TestApp.Start(out var repos, signInAs: "nurse");
            Assert.ThrowsException<PermissionDeniedException>(() => app.Patients.Deactivate(app.Patients.Get(1)));
            Assert.IsTrue(app.Patients.Get(1).IsActive);
            Assert.IsTrue(repos.LogStore.Rows.Any(r => r.Action == "Access Denied" && r.Username == "nurse"));
        }
    }
}

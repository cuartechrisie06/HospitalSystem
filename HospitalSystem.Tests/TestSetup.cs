using HospitalSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalSystem.Tests
{
    [TestClass]
    public static class TestSetup
    {
        // Real sign-ins use 100,000 hashing rounds (about a second each); the tests don't need to wait for that.
        [AssemblyInitialize]
        public static void FastPasswordHashing(TestContext context) => PasswordHasher.Iterations = 1000;
    }
}

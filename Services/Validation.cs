using System;
using System.Text.RegularExpressions;

namespace HospitalSystem.Services
{
    // A rule the data broke. An InvalidOperationException, like the models' own rule errors,
    // so the screens' existing error handling (and the app-wide handler) shows it as a message.
    public class ValidationException : InvalidOperationException
    {
        public ValidationException(string message) : base(message) { }
    }

    // Thrown when an account is locked after too many failed sign-ins.
    public class AccountLockedException : InvalidOperationException
    {
        public AccountLockedException(string message) : base(message) { }
    }

    // Rules shared by the screens (to point at the field) and the services (to enforce them).
    public static class Validation
    {
        // Philippine mobile format: 09171234567 or +639171234567
        public static readonly Regex PhilippineMobile = new Regex(@"^(09\d{9}|\+639\d{9})$");

        public const string PhoneMessage = "Contact must be a valid PH mobile number, e.g. 09171234567 or +639171234567.";

        public static void Require(bool condition, string message)
        {
            if (!condition) throw new ValidationException(message);
        }

        public static void RequireText(string value, string message) =>
            Require(!string.IsNullOrWhiteSpace(value), message);
    }
}

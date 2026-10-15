using System;

namespace HospitalSystem.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }   // PBKDF2 hash (see Services/PasswordHasher.cs)
        public string DisplayName { get; set; }
        public string Role { get; set; }

        public int FailedAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }

        public bool IsAdmin => Role == "Administrator";

        public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.Now;
    }
}

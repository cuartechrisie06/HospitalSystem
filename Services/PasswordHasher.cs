using System;
using System.Security.Cryptography;

namespace HospitalSystem.Services
{
    // Salted PBKDF2 password hashes, stored as "pbkdf2-sha256$iterations$salt$hash" (base64 parts).
    // Built into .NET Framework, so no extra package is needed.
    public static class PasswordHasher
    {
        private const string Prefix = "pbkdf2-sha256";
        // About a second per sign-in on .NET Framework: slow enough to make guessing stolen hashes costly.
        // Internal so the tests can lower it; each hash stores its own count, so old hashes keep verifying.
        internal static int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            byte[] hash = Derive(password, salt, Iterations);
            return string.Join("$", Prefix, Iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool Verify(string password, string stored)
        {
            if (password == null || !IsHashed(stored)) return false;

            var parts = stored.Split('$');
            int iterations;
            if (parts.Length != 4 || !int.TryParse(parts[1], out iterations)) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Derive(password, salt, iterations);
            return FixedTimeEquals(actual, expected);
        }

        public static bool IsHashed(string stored) =>
            stored != null && stored.StartsWith(Prefix + "$", StringComparison.Ordinal);

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(HashSize);
        }

        // Compares every byte so the time taken doesn't reveal how much of the hash matched.
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

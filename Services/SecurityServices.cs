using System;
using System.Collections.Generic;
using System.Linq;
using HospitalSystem.Data;
using HospitalSystem.Data.Repositories;
using HospitalSystem.Models;

namespace HospitalSystem.Services
{
    // Signing in: only the login screen needs this.
    public interface IAuthService
    {
        // Signs in and records the attempt, successful or not, in the activity log. Null if it failed.
        User SignIn(string username, string password);
    }

    // Who is signed in: for screens and services that only need to know the current user.
    public interface IUserSession
    {
        User CurrentUser { get; }
        bool IsAdmin { get; }
        void SignOut();
    }

    // The list of accounts, for filters such as the activity log's user picker.
    public interface IUserDirectory
    {
        List<User> All();
    }

    // What the signed-in user may do. Views use Can() to hide what the user can't do; services
    // call Demand() on the protected operations, so a hidden button that slips through
    // still can't change anything (and the attempt is logged).
    public interface IPermissionGuard
    {
        bool Can(Permission permission);
        void Demand(Permission permission, string action);
    }

    public class AuthService : IAuthService, IUserSession, IUserDirectory, IReloadable
    {
        public const int MaxFailedAttempts = 5;
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        private readonly IUserRepository repository;
        private readonly IActivityLogger log;
        private List<User> users;

        public AuthService(IUserRepository repository, IActivityLogger log)
        {
            this.repository = repository;
            this.log = log;
            users = repository.GetAll();
            HashPlainTextPasswords();
        }

        public User CurrentUser { get; private set; }

        public bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;

        public List<User> All() => users.ToList();

        public void Reload() => users = repository.GetAll();

        // Throws AccountLockedException while the account is locked.
        public User SignIn(string username, string password)
        {
            // Fresh from the database, so a lock or a failed attempt on another workstation counts.
            var user = repository.Get(username);

            if (user == null)
            {
                log.Log("Security", "Sign-in Failed", $"Failed sign-in attempt for unknown username \"{username}\"", "🔒", username);
                return null;
            }

            if (user.IsLocked)
            {
                log.Log("Security", "Sign-in Blocked", $"Sign-in attempt for locked account \"{user.Username}\"", "🔒", user.Username);
                throw new AccountLockedException(LockedMessage(user));
            }

            if (!PasswordHasher.Verify(password, user.Password))
            {
                user.FailedAttempts++;
                bool lockNow = user.FailedAttempts >= MaxFailedAttempts;
                if (lockNow)
                {
                    user.LockedUntil = DateTime.Now.Add(LockoutDuration);
                    user.FailedAttempts = 0;
                }
                repository.UpdateLoginState(user);

                log.Log("Security", "Sign-in Failed", $"Failed sign-in attempt for username \"{user.Username}\"", "🔒", user.Username);
                if (lockNow)
                {
                    log.Log("Security", "Account Locked",
                        $"{user.Username} locked for {LockoutDuration.TotalMinutes:0} minutes after {MaxFailedAttempts} failed sign-ins", "⛔", user.Username);
                    throw new AccountLockedException(LockedMessage(user));
                }
                return null;
            }

            if (user.FailedAttempts != 0 || user.LockedUntil.HasValue)
            {
                user.FailedAttempts = 0;
                user.LockedUntil = null;
                repository.UpdateLoginState(user);
            }

            CurrentUser = user;
            log.Log("Security", "Signed In", $"{user.DisplayName} ({user.Role}) signed in", "🔑");
            return user;
        }

        public void SignOut()
        {
            if (CurrentUser != null)
                log.Log("Security", "Signed Out", $"{CurrentUser.DisplayName} signed out", "🚪");
            CurrentUser = null;
        }

        private static string LockedMessage(User user)
        {
            int minutes = Math.Max(1, (int)Math.Ceiling((user.LockedUntil.Value - DateTime.Now).TotalMinutes));
            return $"Too many failed sign-ins. This account is locked for {minutes} more minute(s).";
        }

        // Databases from before password hashing (and the seed in schema.sql) hold plain text.
        // Replace each with its hash the first time the app runs against them.
        private void HashPlainTextPasswords()
        {
            int upgraded = 0;
            foreach (var user in users.Where(u => !PasswordHasher.IsHashed(u.Password)))
            {
                user.Password = PasswordHasher.Hash(user.Password);
                repository.UpdatePassword(user);
                upgraded++;
            }

            if (upgraded > 0)
                log.Log("Security", "Passwords Hashed", $"Replaced {upgraded} plain-text password(s) with secure hashes", "🔐",
                    ActivityItem.SystemUser);
        }
    }

    public class PermissionGuard : IPermissionGuard
    {
        private readonly IUserSession session;
        private readonly IActivityLogger log;

        public PermissionGuard(IUserSession session, IActivityLogger log)
        {
            this.session = session;
            this.log = log;
        }

        public bool Can(Permission permission) => Permissions.Can(session.CurrentUser, permission);

        public void Demand(Permission permission, string action)
        {
            if (Can(permission)) return;

            var user = session.CurrentUser;
            log.Log("Security", "Access Denied",
                $"{(user != null ? user.DisplayName + " (" + user.Role + ")" : "Nobody signed in")} tried to {action}", "⛔");
            throw new PermissionDeniedException("You don't have permission to " + action + ". Ask an administrator.");
        }
    }
}

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

    public class AuthService : IAuthService, IUserSession, IUserDirectory
    {
        private readonly List<User> users;
        private readonly IActivityLogger log;

        public AuthService(IUserRepository repository, IActivityLogger log)
        {
            users = repository.GetAll();
            this.log = log;
        }

        public User CurrentUser { get; private set; }

        public bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;

        public List<User> All() => users.ToList();

        public User SignIn(string username, string password)
        {
            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            if (user == null)
            {
                log.Log("Security", "Sign-in Failed", $"Failed sign-in attempt for username \"{username}\"", "🔒", username);
                return null;
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

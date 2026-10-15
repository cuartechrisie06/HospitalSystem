using System;
using System.Configuration;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using HospitalSystem.Data;
using HospitalSystem.Forms;
using HospitalSystem.Services;

namespace HospitalSystem
{
    internal static class Program
    {
        private static AppServices services;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Anything a screen doesn't handle itself ends up here instead of closing the app.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ShowError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowError(e.ExceptionObject as Exception);

            try
            {
                services = AppServices.Create(new MySqlConnectionFactory(ConnectionString()));
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Can't connect to the hospital database. Make sure MySQL is running and " +
                                "the hospital_system database is set up (see README).\n\n" + ex.Message,
                    "Database unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var login = new LoginForm(services.Auth))
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return;
            }

            Application.Run(new DashboardForm(services));
        }

        // From App.config ("HospitalSystem"), falling back to the XAMPP default.
        private static string ConnectionString()
        {
            var setting = ConfigurationManager.ConnectionStrings["HospitalSystem"];
            return setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString)
                ? setting.ConnectionString
                : MySqlConnectionFactory.DefaultConnectionString;
        }

        private static void ShowError(Exception ex)
        {
            if (ex == null) return;

            // Rule broken (validation, permission, "bed no longer available"...): just tell the user.
            if (ex is InvalidOperationException)
            {
                MessageBox.Show(ex.Message, "Can't do that", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string title = ex is MySqlException ? "Database error" : "Unexpected error";
            string hint = ex is MySqlException
                ? "The database could not complete the request. Check that MySQL is still running, then try again."
                : "Something went wrong. The action may not have been saved; please check and try again.";

            try
            {
                services?.Log.Log("System", "Error", $"{ex.GetType().Name}: {ex.Message}", "💥");
            }
            catch (Exception)
            {
                // The error may be the database itself; showing the message matters more than logging it.
            }

            MessageBox.Show(hint + "\n\n" + ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using HospitalSystem.Data;
using HospitalSystem.Forms;
using HospitalSystem.Services;

namespace HospitalSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppServices services;
            try
            {
                services = AppServices.Create(new MySqlConnectionFactory());
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
    }
}

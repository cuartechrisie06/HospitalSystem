namespace HospitalSystem.Services
{
    // A service that keeps a copy of its data in memory. Reload() re-reads it from the database,
    // so changes made on other workstations show up (AppServices.Reload calls every one).
    public interface IReloadable
    {
        void Reload();
    }
}

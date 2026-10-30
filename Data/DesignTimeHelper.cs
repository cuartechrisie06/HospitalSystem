using System.ComponentModel;

namespace HospitalSystem.Data
{
    // Control.DesignMode is unreliable inside a control's own constructor
    // (Site isn't assigned yet), so views/forms must check this instead
    // before touching the services or the database.
    public static class DesignTimeHelper
    {
        public static bool IsDesignMode =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }
}

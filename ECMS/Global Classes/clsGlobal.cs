using ECMS_Business;

namespace ECMS
{
    // Shared information for the whole application (who is logged in).
    public static class clsGlobal
    {
        public static clsUser? CurrentUser { get; set; }
    }
}
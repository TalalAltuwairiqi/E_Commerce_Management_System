using System.Data;
using ECMS_DataAccess;

namespace ECMS_Business
{
    // Business Layer: the forms call this class, and this class calls the Data Access Layer.
    // Right now it only passes calls through; real business rules arrive in the next stages.
    public static class clsDatabase
    {
        public static bool TestConnection(out string message)
        {
            return clsDatabaseData.TestConnection(out message);
        }

        public static DataTable GetTableRowCounts()
        {
            return clsDatabaseData.GetTableRowCounts();
        }
    }
}
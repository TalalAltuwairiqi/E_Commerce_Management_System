using System;
using System.Configuration;

namespace ECMS_DataAccess
{
    // Reads the connection string from App.config (so it is not hard-coded in the code).
    public static class clsDataAccessSettings
    {
        public static string ConnectionString
        {
            get
            {
                var connectionString = ConfigurationManager.ConnectionStrings["ECMS_DB"]?.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "Connection string 'ECMS_DB' was not found. Check that App.config is in the ECMS project.");

                return connectionString;
            }
        }
    }
}
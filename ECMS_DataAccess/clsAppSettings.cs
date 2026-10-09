using System;
using System.Configuration;
using System.Globalization;

namespace ECMS_DataAccess
{
    // Reads business settings from App.config (the <appSettings> section).
    public static class clsAppSettings
    {
        private const decimal DefaultTaxRate = 0.15m;

        // Tax rate as a fraction: 0.15 means 15%.
        public static decimal TaxRate
        {
            get
            {
                string? raw = ConfigurationManager.AppSettings["TaxRate"];

                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate)
                    && rate >= 0 && rate <= 1)
                    return rate;

                return DefaultTaxRate;
            }
        }
    }
}
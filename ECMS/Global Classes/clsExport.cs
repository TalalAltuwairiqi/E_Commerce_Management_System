using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ECMS
{
    // Saves what a grid shows as a CSV file (it opens in Excel).
    public static class clsExport
    {
        // Asks where to save, then writes the visible columns. Returns the saved path (null if cancelled).
        public static string? GridToCsv(DataGridView grid, string defaultFileName)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV file (*.csv)|*.csv";
                dialog.FileName = defaultFileName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";

                if (dialog.ShowDialog() != DialogResult.OK)
                    return null;

                var columns = grid.Columns.Cast<DataGridViewColumn>()
                                  .Where(c => c.Visible)
                                  .OrderBy(c => c.DisplayIndex)
                                  .ToList();

                StringBuilder text = new StringBuilder();
                text.AppendLine(string.Join(",", columns.Select(c => Escape(c.HeaderText))));

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    text.AppendLine(string.Join(",", columns.Select(c => Escape(Format(row.Cells[c.Index].Value)))));
                }

                // UTF-8 with a signature so Excel shows non-English text correctly
                File.WriteAllText(dialog.FileName, text.ToString(), new UTF8Encoding(true));
                return dialog.FileName;
            }
        }

        private static string Format(object? value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            if (value is DateTime date) return date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            if (value is decimal money) return money.ToString("0.00", CultureInfo.InvariantCulture);
            if (value is bool flag) return flag ? "Yes" : "No";

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string Escape(string text)
        {
            // A cell that starts with = or @ could be run as a formula by Excel, so it is marked as text.
            if (text.StartsWith("=") || text.StartsWith("@"))
                text = "'" + text;

            if (text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + text.Replace("\"", "\"\"") + "\"";

            return text;
        }
    }
}
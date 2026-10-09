using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    // Shared look-and-feel and small helpers used by all forms.
    public static class clsUiHelper
    {
        public static readonly Color PrimaryColor = Color.FromArgb(31, 58, 95);
        public static readonly Color AccentColor = Color.FromArgb(46, 117, 182);

        public static void StyleGrid(DataGridView grid)
        {
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(244, 247, 251);
            grid.DefaultCellStyle.SelectionBackColor = AccentColor;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.RowTemplate.Height = 26;
        }

        // Sets the header text, width weight, format and visibility of one grid column.
        public static void SetColumn(DataGridView grid, string name, string header, float weight,
                                     string? format = null, bool visible = true)
        {
            DataGridViewColumn? column = grid.Columns[name];
            if (column == null)
                return;

            column.HeaderText = header;
            column.FillWeight = weight;
            column.Visible = visible;

            if (format != null)
                column.DefaultCellStyle.Format = format;
        }

        public static void StyleButton(Button button, bool primary = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = AccentColor;
            button.BackColor = primary ? AccentColor : Color.White;
            button.ForeColor = primary ? Color.White : PrimaryColor;
            button.Cursor = Cursors.Hand;
        }

        // Returns the number in the given column of the selected row (null if nothing is selected).
        public static int? GetSelectedId(DataGridView grid, string columnName)
        {
            if (grid.CurrentRow == null)
                return null;

            object? value = grid.CurrentRow.Cells[columnName].Value;
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "ECMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ShowInfo(string message)
        {
            MessageBox.Show(message, "ECMS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowException(Exception ex)
        {
            MessageBox.Show("Something went wrong. Please check the database connection and try again.\n\n" + ex.Message,
                "ECMS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(message, "ECMS", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                   == DialogResult.Yes;
        }
    }
}
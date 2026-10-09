using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    public enum enReportType
    {
        BestSellers,
        MonthlySales,
        SalesByCategory
    }

    // One window for the three reports: a chart on top and the numbers below.
    public partial class frmReport : Form
    {
        private readonly enReportType _type;

        private readonly Label lblTitle = new Label();
        private readonly Label lblFrom = new Label();
        private readonly DateTimePicker dtpFrom = new DateTimePicker();
        private readonly Label lblTo = new Label();
        private readonly DateTimePicker dtpTo = new DateTimePicker();
        private readonly Button btnRun = new Button();
        private readonly clsBarChart chart = new clsBarChart();
        private readonly DataGridView dgvReport = new DataGridView();
        private readonly Label lblTotal = new Label();
        private readonly Button btnExport = new Button();
        private readonly Button btnClose = new Button();

        public frmReport(enReportType type)
        {
            _type = type;
            BuildUI();
            LoadReport();
        }

        private string ReportName
        {
            get
            {
                switch (_type)
                {
                    case enReportType.BestSellers: return "Best Sellers";
                    case enReportType.MonthlySales: return "Monthly Sales";
                    default: return "Sales by Category";
                }
            }
        }

        private void BuildUI()
        {
            Text = ReportName;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1000, 680);
            MinimumSize = new Size(860, 540);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = ReportName;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 500, 36);

            // The date range is only used by "Sales by Category"
            bool showDates = _type == enReportType.SalesByCategory;

            lblFrom.Text = "From";
            lblFrom.AutoSize = false;
            lblFrom.SetBounds(20, 62, 45, 24);
            dtpFrom.SetBounds(68, 58, 150, 27);
            dtpFrom.Format = DateTimePickerFormat.Short;
            dtpFrom.Value = DateTime.Today.AddMonths(-12);

            lblTo.Text = "To";
            lblTo.AutoSize = false;
            lblTo.SetBounds(240, 62, 30, 24);
            dtpTo.SetBounds(272, 58, 150, 27);
            dtpTo.Format = DateTimePickerFormat.Short;
            dtpTo.Value = DateTime.Today;

            btnRun.Text = "Show";
            btnRun.SetBounds(444, 54, 110, 34);
            clsUiHelper.StyleButton(btnRun, true);
            btnRun.Click += (s, e) => LoadReport();

            lblFrom.Visible = showDates;
            dtpFrom.Visible = showDates;
            lblTo.Visible = showDates;
            dtpTo.Visible = showDates;
            btnRun.Visible = showDates;

            chart.SetBounds(20, 100, 960, 230);
            chart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            clsUiHelper.StyleGrid(dgvReport);
            dgvReport.SetBounds(20, 340, 960, 270);
            dgvReport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lblTotal.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTotal.AutoSize = false;
            lblTotal.SetBounds(20, 630, 600, 28);
            lblTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            btnExport.Text = "Export to CSV";
            btnExport.SetBounds(720, 624, 150, 38);
            btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnExport);
            btnExport.Click += (s, e) => ExportCsv();

            btnClose.Text = "Close";
            btnClose.SetBounds(880, 624, 100, 38);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();

            CancelButton = btnClose;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblFrom, dtpFrom, lblTo, dtpTo, btnRun, chart, dgvReport, lblTotal, btnExport, btnClose
            });
        }

        private void LoadReport()
        {
            try
            {
                clsUser? user = clsGlobal.CurrentUser;
                List<KeyValuePair<string, decimal>> points = new List<KeyValuePair<string, decimal>>();
                DataTable table;

                switch (_type)
                {
                    case enReportType.BestSellers:
                        table = clsReport.GetBestSellers(user, 10);
                        dgvReport.DataSource = table;

                        clsUiHelper.SetColumn(dgvReport, "ProductID", "ID", 20, null, false);
                        clsUiHelper.SetColumn(dgvReport, "Name", "Product", 110);
                        clsUiHelper.SetColumn(dgvReport, "Category", "Category", 70);
                        clsUiHelper.SetColumn(dgvReport, "UnitsSold", "Units Sold", 45);
                        clsUiHelper.SetColumn(dgvReport, "Revenue", "Revenue (before tax)", 60, "N2");

                        foreach (DataRow row in table.Rows)
                            points.Add(new KeyValuePair<string, decimal>(Convert.ToString(row["Name"]) ?? string.Empty,
                                                                         Convert.ToDecimal(row["UnitsSold"])));

                        chart.SetData(points, "Top 10 products by units sold", "N0");
                        break;

                    case enReportType.MonthlySales:
                        table = clsReport.GetMonthlySales(user);
                        dgvReport.DataSource = table;

                        clsUiHelper.SetColumn(dgvReport, "SalesYear", "Year", 20, null, false);
                        clsUiHelper.SetColumn(dgvReport, "SalesMonth", "Month", 20, null, false);
                        clsUiHelper.SetColumn(dgvReport, "Period", "Month", 50);
                        clsUiHelper.SetColumn(dgvReport, "OrdersCount", "Orders", 40);
                        clsUiHelper.SetColumn(dgvReport, "Revenue", "Revenue (incl. tax)", 60, "N2");

                        foreach (DataRow row in table.Rows)
                            points.Add(new KeyValuePair<string, decimal>(Convert.ToString(row["Period"]) ?? string.Empty,
                                                                         Convert.ToDecimal(row["Revenue"])));

                        chart.SetData(points, "Revenue per month (cancelled orders are not counted)", "N2");
                        break;

                    default:
                        table = clsReport.GetSalesByCategory(user, dtpFrom.Value, dtpTo.Value);
                        dgvReport.DataSource = table;

                        clsUiHelper.SetColumn(dgvReport, "Category", "Category", 80);
                        clsUiHelper.SetColumn(dgvReport, "UnitsSold", "Units Sold", 45);
                        clsUiHelper.SetColumn(dgvReport, "Revenue", "Revenue (before tax)", 60, "N2");

                        foreach (DataRow row in table.Rows)
                            points.Add(new KeyValuePair<string, decimal>(Convert.ToString(row["Category"]) ?? string.Empty,
                                                                         Convert.ToDecimal(row["Revenue"])));

                        chart.SetData(points, "Revenue per category from " + dtpFrom.Value.ToString("yyyy-MM-dd") +
                                              " to " + dtpTo.Value.ToString("yyyy-MM-dd"), "N2");
                        break;
                }

                decimal total = 0;
                foreach (DataRow row in table.Rows)
                    total += Convert.ToDecimal(row["Revenue"]);

                lblTotal.Text = "Total revenue: " + total.ToString("N2");
            }
            catch (UnauthorizedAccessException ex)
            {
                clsUiHelper.ShowError(ex.Message);
            }
            catch (ArgumentException ex)
            {
                clsUiHelper.ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void ExportCsv()
        {
            try
            {
                string? path = clsExport.GridToCsv(dgvReport, ReportName.Replace(" ", "_"));
                if (path != null)
                    clsUiHelper.ShowInfo("The report was saved:\n" + path);
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
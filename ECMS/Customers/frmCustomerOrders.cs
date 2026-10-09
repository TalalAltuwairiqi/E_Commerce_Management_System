using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Read-only list of the orders of one customer (uses the stored procedure sp_GetCustomerOrders).
    public partial class frmCustomerOrders : Form
    {
        private readonly int _customerID;
        private readonly string _customerName;

        private readonly Label lblTitle = new Label();
        private readonly DataGridView dgvOrders = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnClose = new Button();

        public frmCustomerOrders(int customerID, string customerName)
        {
            _customerID = customerID;
            _customerName = customerName;
            BuildUI();
            LoadOrders();
        }

        private void BuildUI()
        {
            Text = "Customer Orders";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(820, 460);
            MinimumSize = new Size(640, 340);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Orders of " + _customerName;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 780, 36);

            clsUiHelper.StyleGrid(dgvOrders);
            dgvOrders.SetBounds(20, 60, 780, 330);
            dgvOrders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 410, 400, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            btnClose.Text = "Close";
            btnClose.SetBounds(680, 405, 120, 38);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();

            CancelButton = btnClose;

            Controls.AddRange(new Control[] { lblTitle, dgvOrders, lblCount, btnClose });
        }

        private void LoadOrders()
        {
            try
            {
                dgvOrders.DataSource = clsCustomer.GetOrders(_customerID);

                clsUiHelper.SetColumn(dgvOrders, "OrderID", "Order #", 40);
                clsUiHelper.SetColumn(dgvOrders, "OrderDate", "Date", 80, "yyyy-MM-dd HH:mm");
                clsUiHelper.SetColumn(dgvOrders, "Status", "Status", 60);
                clsUiHelper.SetColumn(dgvOrders, "TotalAmount", "Total", 60, "N2");
                clsUiHelper.SetColumn(dgvOrders, "PaidAmount", "Paid", 60, "N2");
                clsUiHelper.SetColumn(dgvOrders, "RemainingAmount", "Remaining", 60, "N2");

                lblCount.Text = dgvOrders.Rows.Count + " orders";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
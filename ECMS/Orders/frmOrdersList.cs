
using ECMS_Business;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    public partial class frmOrdersList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly Label lblStatus = new Label();
        private readonly ComboBox cmbStatus = new ComboBox();
        private readonly CheckBox chkUnpaid = new CheckBox();
        private readonly DataGridView dgvOrders = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnNew = new Button();
        private readonly Button btnDetails = new Button();
        private readonly Button btnClose = new Button();

        public frmOrdersList()
        {
            BuildUI();
            LoadOrders();
        }

        private void BuildUI()
        {
            Text = "Orders";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1100, 600);
            MinimumSize = new Size(960, 460);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Orders";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 280, 27);
            txtSearch.TextChanged += (s, e) => LoadOrders();

            lblStatus.Text = "Status";
            lblStatus.AutoSize = false;
            lblStatus.SetBounds(384, 62, 55, 24);
            cmbStatus.SetBounds(442, 58, 170, 27);
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.Items.Add("All statuses");
            foreach (string status in clsOrder.Statuses)
                cmbStatus.Items.Add(status);
            cmbStatus.SelectedIndex = 0;                                   // set BEFORE the event is connected
            cmbStatus.SelectedIndexChanged += (s, e) => LoadOrders();

            chkUnpaid.Text = "Unpaid orders only";
            chkUnpaid.SetBounds(640, 58, 220, 27);
            chkUnpaid.CheckedChanged += (s, e) => LoadOrders();

            clsUiHelper.StyleGrid(dgvOrders);
            dgvOrders.SetBounds(20, 100, 1060, 420);
            dgvOrders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvOrders.DoubleClick += (s, e) => ShowDetails();
            dgvOrders.DataBindingComplete += (s, e) => GreyOutCancelled();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 553, 400, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnNew, "New Order", 650, true);
            SetupButton(btnDetails, "View Details", 810, false);
            SetupButton(btnClose, "Close", 960, false);

            btnNew.Click += (s, e) => NewOrder();
            btnDetails.Click += (s, e) => ShowDetails();
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, lblStatus, cmbStatus, chkUnpaid,
                dgvOrders, lblCount, btnNew, btnDetails, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 545, 140, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        private void LoadOrders()
        {
            try
            {
                string status = cmbStatus.SelectedIndex > 0 ? (cmbStatus.SelectedItem?.ToString() ?? string.Empty) : string.Empty;

                dgvOrders.DataSource = clsOrder.GetAll(txtSearch.Text, status, chkUnpaid.Checked);

                clsUiHelper.SetColumn(dgvOrders, "OrderID", "Order #", 35);
                clsUiHelper.SetColumn(dgvOrders, "CustomerName", "Customer", 90);
                clsUiHelper.SetColumn(dgvOrders, "CreatedBy", "Created By", 55);
                clsUiHelper.SetColumn(dgvOrders, "OrderDate", "Date", 80, "yyyy-MM-dd HH:mm");
                clsUiHelper.SetColumn(dgvOrders, "Status", "Status", 55);
                clsUiHelper.SetColumn(dgvOrders, "TotalAmount", "Total", 55, "N2");
                clsUiHelper.SetColumn(dgvOrders, "PaidAmount", "Paid", 55, "N2");
                clsUiHelper.SetColumn(dgvOrders, "RemainingAmount", "Remaining", 55, "N2");

                lblCount.Text = dgvOrders.Rows.Count + " orders";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Cancelled orders are shown in grey
        private void GreyOutCancelled()
        {
            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                bool cancelled = Convert.ToString(row.Cells["Status"].Value) == "Cancelled";
                row.DefaultCellStyle.ForeColor = cancelled ? Color.Gray : Color.Black;
            }
        }

        private void NewOrder()
        {
            using (frmNewOrder form = new frmNewOrder())
            {
                form.ShowDialog(this);
            }

            LoadOrders();
        }

        private void ShowDetails()
        {
            int? id = clsUiHelper.GetSelectedId(dgvOrders, "OrderID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select an order first.");
                return;
            }

            using (frmOrderDetails form = new frmOrderDetails(id.Value))
            {
                form.ShowDialog(this);
            }

            LoadOrders();   // the status or the stock may have changed
        }
    }
}
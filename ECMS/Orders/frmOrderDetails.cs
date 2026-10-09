using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // One order: customer, items, payments, totals, and the actions (next status / cancel).
    public partial class frmOrderDetails : Form
    {
        private readonly int _orderID;
        private string _currentStatus = string.Empty;
        private bool _notFound;

        private readonly Label lblTitle = new Label();
        private readonly Label lblStatus = new Label();
        private readonly Label lblCustomer = new Label();
        private readonly Label lblDate = new Label();
        private readonly Label lblCreatedBy = new Label();
        private readonly Label lblNotes = new Label();
        private readonly DataGridView dgvItems = new DataGridView();
        private readonly DataGridView dgvPayments = new DataGridView();
        private readonly Label lblSubtotal = new Label();
        private readonly Label lblTax = new Label();
        private readonly Label lblTotal = new Label();
        private readonly Label lblPaid = new Label();
        private readonly Label lblRemaining = new Label();
        private readonly Button btnNext = new Button();
        private readonly Button btnCancelOrder = new Button();
        private readonly Button btnAddPayment = new Button();
        private readonly Button btnClose = new Button();

        public frmOrderDetails(int orderID)
        {
            _orderID = orderID;
            BuildUI();
            LoadOrder();

            Shown += (s, e) =>
            {
                if (_notFound)
                {
                    clsUiHelper.ShowError("This order no longer exists.");
                    Close();
                }
            };
        }

        private void BuildUI()
        {
            Text = "Order Details";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(900, 580);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 300, 36);

            lblStatus.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblStatus.AutoSize = false;
            lblStatus.SetBounds(330, 16, 250, 30);

            AddCaption("Customer", 20, 60, 90);
            SetupValue(lblCustomer, 110, 60, 400);
            AddCaption("Date", 540, 60, 60);
            SetupValue(lblDate, 600, 60, 280);
            AddCaption("Created by", 20, 86, 90);
            SetupValue(lblCreatedBy, 110, 86, 400);
            AddCaption("Notes", 540, 86, 60);
            SetupValue(lblNotes, 600, 86, 280);

            AddCaption("Items", 20, 120, 200);
            clsUiHelper.StyleGrid(dgvItems);
            dgvItems.SetBounds(20, 146, 860, 170);

            AddCaption("Payments", 20, 326, 200);
            clsUiHelper.StyleGrid(dgvPayments);
            dgvPayments.SetBounds(20, 352, 500, 130);

            SetupTotal("Subtotal", lblSubtotal, 352, false);
            SetupTotal("Tax", lblTax, 378, false);
            SetupTotal("Total", lblTotal, 406, true);
            SetupTotal("Paid", lblPaid, 440, false);
            SetupTotal("Remaining", lblRemaining, 466, true);

            btnNext.SetBounds(20, 520, 260, 40);
            clsUiHelper.StyleButton(btnNext, true);
            btnNext.Click += (s, e) => MoveToNextStatus();

            btnCancelOrder.Text = "Cancel Order";
            btnCancelOrder.SetBounds(300, 520, 180, 40);
            clsUiHelper.StyleButton(btnCancelOrder);
            btnCancelOrder.ForeColor = Color.Firebrick;
            btnCancelOrder.Click += (s, e) => CancelOrder();

            btnAddPayment.Text = "Add Payment";
            btnAddPayment.SetBounds(500, 520, 180, 40);
            clsUiHelper.StyleButton(btnAddPayment);
            btnAddPayment.Click += (s, e) => AddPayment();

            btnClose.Text = "Close";
            btnClose.SetBounds(700, 520, 180, 40);
            clsUiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();

            CancelButton = btnClose;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblStatus, lblCustomer, lblDate, lblCreatedBy, lblNotes,
                dgvItems, dgvPayments, btnNext, btnCancelOrder, btnAddPayment, btnClose
            });
        }

        private void AddCaption(string text, int x, int y, int width)
        {
            Label label = new Label { Text = text, AutoSize = false, ForeColor = Color.Gray };
            label.SetBounds(x, y, width, 22);
            Controls.Add(label);
        }

        private static void SetupValue(Label label, int x, int y, int width)
        {
            label.AutoSize = false;
            label.SetBounds(x, y, width, 22);
        }

        private void SetupTotal(string caption, Label value, int y, bool bold)
        {
            Font font = bold ? new Font("Segoe UI", 11F, FontStyle.Bold) : new Font("Segoe UI", 10F);

            Label label = new Label { Text = caption, AutoSize = false, Font = font };
            label.SetBounds(560, y, 170, 26);
            Controls.Add(label);

            value.AutoSize = false;
            value.Font = font;
            value.TextAlign = ContentAlignment.MiddleRight;
            value.SetBounds(730, y, 150, 26);
            Controls.Add(value);
        }

        private static Color StatusColor(string status)
        {
            switch (status)
            {
                case "Pending": return Color.DarkOrange;
                case "Processing": return Color.RoyalBlue;
                case "Shipped": return Color.DarkViolet;
                case "Delivered": return Color.DarkGreen;
                default: return Color.Gray;       // Cancelled
            }
        }

        // Reads the order from the database and fills the whole screen
        private void LoadOrder()
        {
            try
            {
                DataTable header = clsOrder.GetHeader(_orderID);
                if (header.Rows.Count == 0)
                {
                    _notFound = true;
                    return;
                }

                DataRow row = header.Rows[0];
                _currentStatus = (string)row["Status"];

                lblTitle.Text = "Order #" + _orderID;
                lblStatus.Text = _currentStatus;
                lblStatus.ForeColor = StatusColor(_currentStatus);

                lblCustomer.Text = (string)row["CustomerName"];
                lblDate.Text = ((DateTime)row["OrderDate"]).ToString("yyyy-MM-dd HH:mm");
                lblCreatedBy.Text = (string)row["CreatedBy"];
                lblNotes.Text = row.IsNull("Notes") ? "-" : (string)row["Notes"];

                decimal remaining = Convert.ToDecimal(row["RemainingAmount"]);

                lblSubtotal.Text = Convert.ToDecimal(row["Subtotal"]).ToString("N2");
                lblTax.Text = Convert.ToDecimal(row["TaxAmount"]).ToString("N2");
                lblTotal.Text = Convert.ToDecimal(row["TotalAmount"]).ToString("N2");
                lblPaid.Text = Convert.ToDecimal(row["PaidAmount"]).ToString("N2");
                lblRemaining.Text = remaining.ToString("N2");
                lblRemaining.ForeColor = (remaining > 0 && _currentStatus != "Cancelled") ? Color.Firebrick : Color.Black;

                dgvItems.DataSource = clsOrder.GetItems(_orderID);
                clsUiHelper.SetColumn(dgvItems, "OrderItemID", "ID", 20, null, false);
                clsUiHelper.SetColumn(dgvItems, "ProductID", "ProductID", 20, null, false);
                clsUiHelper.SetColumn(dgvItems, "ProductName", "Product", 120);
                clsUiHelper.SetColumn(dgvItems, "Quantity", "Quantity", 35);
                clsUiHelper.SetColumn(dgvItems, "UnitPrice", "Unit Price", 50, "N2");
                clsUiHelper.SetColumn(dgvItems, "LineTotal", "Line Total", 50, "N2");

                dgvPayments.DataSource = clsOrder.GetPayments(_orderID);
                clsUiHelper.SetColumn(dgvPayments, "PaymentID", "ID", 20, null, false);
                clsUiHelper.SetColumn(dgvPayments, "PaymentDate", "Date", 70, "yyyy-MM-dd HH:mm");
                clsUiHelper.SetColumn(dgvPayments, "Method", "Method", 45);
                clsUiHelper.SetColumn(dgvPayments, "Amount", "Amount", 45, "N2");
                clsUiHelper.SetColumn(dgvPayments, "ReceivedBy", "Received By", 50);

                // Buttons depend on the status
                string? next = clsOrder.GetNextStatus(_currentStatus);
                btnNext.Visible = next != null;
                btnNext.Text = next == null ? string.Empty : "Mark as " + next;
                btnCancelOrder.Visible = clsOrder.CanCancel(_currentStatus);
                btnAddPayment.Visible = remaining > 0 && _currentStatus != "Cancelled";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void MoveToNextStatus()
        {
            string? next = clsOrder.GetNextStatus(_currentStatus);
            if (next == null)
                return;

            if (!clsUiHelper.Confirm("Change the status of order #" + _orderID + " to '" + next + "'?"))
                return;

            try
            {
                if (!clsOrder.ChangeStatus(_orderID, next, clsGlobal.CurrentUser, out string error))
                    clsUiHelper.ShowError(error);
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }

            LoadOrder();
        }

        private void CancelOrder()
        {
            if (!clsUiHelper.Confirm("Cancel order #" + _orderID + "?\n\nThe stock of its items will be returned."))
                return;

            try
            {
                if (clsOrder.Cancel(_orderID, clsGlobal.CurrentUser, out string error))
                    clsUiHelper.ShowInfo("The order was cancelled and its stock was returned.");
                else
                    clsUiHelper.ShowError(error);
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }

            LoadOrder();
        }

        private void AddPayment()
        {
            using (frmAddPayment form = new frmAddPayment(_orderID))
            {
                form.ShowDialog(this);
            }

            LoadOrder();
        }
    }
}
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Records a payment. orderID == null: the user chooses the order. orderID given: the order is fixed.
    public partial class frmAddPayment : Form
    {
        private readonly int? _orderID;
        private decimal _remaining;

        private readonly Label lblTitle = new Label();
        private readonly ComboBox cmbOrder = new ComboBox();
        private readonly Label lblBalance = new Label();
        private readonly NumericUpDown nudAmount = new NumericUpDown();
        private readonly Button btnFull = new Button();
        private readonly ComboBox cmbMethod = new ComboBox();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmAddPayment(int? orderID = null)
        {
            _orderID = orderID;
            BuildUI();
            LoadOrders();
        }

        private void BuildUI()
        {
            Text = "Add Payment";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(500, 410);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Add Payment";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 460, 36);

            AddLabel("Order *", 20, 64);
            cmbOrder.SetBounds(20, 88, 460, 27);
            cmbOrder.DropDownStyle = ComboBoxStyle.DropDown;
            cmbOrder.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbOrder.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbOrder.SelectedIndexChanged += (s, e) => ShowBalance();

            lblBalance.ForeColor = Color.Gray;
            lblBalance.AutoSize = false;
            lblBalance.SetBounds(20, 122, 460, 40);

            AddLabel("Amount *", 20, 172);
            nudAmount.SetBounds(20, 196, 200, 27);
            nudAmount.DecimalPlaces = 2;
            nudAmount.Minimum = 0.01m;
            nudAmount.Maximum = 10000000;
            nudAmount.ThousandsSeparator = true;

            btnFull.Text = "Pay full remaining";
            btnFull.SetBounds(240, 192, 200, 34);
            clsUiHelper.StyleButton(btnFull);
            btnFull.Click += (s, e) =>
            {
                if (_remaining >= nudAmount.Minimum)
                    nudAmount.Value = Math.Min(_remaining, nudAmount.Maximum);
            };

            AddLabel("Method *", 20, 238);
            cmbMethod.SetBounds(20, 262, 200, 27);
            cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMethod.Items.AddRange(clsPayment.Methods);
            cmbMethod.SelectedIndex = 0;

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 306, 460, 34);

            btnSave.Text = "Save Payment";
            btnSave.SetBounds(20, 350, 220, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(260, 350, 220, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, cmbOrder, lblBalance, nudAmount, btnFull, cmbMethod, lblError, btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, 300, 22);
            Controls.Add(label);
        }

        // Fills the list with the orders that can still receive a payment
        private void LoadOrders()
        {
            try
            {
                DataTable orders = clsPayment.GetPayableOrders();
                orders.Columns.Add("DisplayText", typeof(string));

                foreach (DataRow row in orders.Rows)
                {
                    decimal remaining = Convert.ToDecimal(row["RemainingAmount"]);
                    row["DisplayText"] = "#" + row["OrderID"] + "   -   " + row["CustomerName"] +
                                         "   -   remaining " + remaining.ToString("N2");
                }

                cmbOrder.DisplayMember = "DisplayText";
                cmbOrder.ValueMember = "OrderID";
                cmbOrder.DataSource = orders;
                cmbOrder.SelectedIndex = -1;

                if (_orderID.HasValue)
                {
                    cmbOrder.SelectedValue = _orderID.Value;
                    cmbOrder.Enabled = false;   // the order is fixed

                    if (!(cmbOrder.SelectedValue is int))
                    {
                        lblError.Text = "This order is fully paid or cancelled, so it cannot receive payments.";
                        btnSave.Enabled = false;
                        btnFull.Enabled = false;
                    }
                }

                ShowBalance();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Shows total / paid / remaining of the selected order and limits the amount
        private void ShowBalance()
        {
            _remaining = 0;
            lblBalance.Text = string.Empty;

            if (cmbOrder.SelectedItem is DataRowView view)
            {
                decimal total = Convert.ToDecimal(view["TotalAmount"]);
                decimal paid = Convert.ToDecimal(view["PaidAmount"]);
                _remaining = Convert.ToDecimal(view["RemainingAmount"]);

                lblBalance.Text = "Total: " + total.ToString("N2") + "      Paid: " + paid.ToString("N2") +
                                  "      Remaining: " + _remaining.ToString("N2");

                nudAmount.Maximum = Math.Max(_remaining, nudAmount.Minimum);
                if (nudAmount.Value > nudAmount.Maximum)
                    nudAmount.Value = nudAmount.Maximum;
            }
            else
            {
                nudAmount.Maximum = 10000000;
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (!(cmbOrder.SelectedValue is int orderID))
            {
                lblError.Text = "Please select an order from the list.";
                return;
            }

            string method = cmbMethod.SelectedItem?.ToString() ?? string.Empty;

            try
            {
                if (!clsPayment.Add(orderID, nudAmount.Value, method, clsGlobal.CurrentUser, out string error))
                {
                    lblError.Text = error;
                    return;
                }

                clsUiHelper.ShowInfo("The payment was saved.");
                DialogResult = DialogResult.OK;   // closes the form
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
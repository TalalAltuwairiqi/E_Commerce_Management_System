
using ECMS_Business;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    public partial class frmPaymentsList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly Label lblMethod = new Label();
        private readonly ComboBox cmbMethod = new ComboBox();
        private readonly DataGridView dgvPayments = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdd = new Button();
        private readonly Button btnOrder = new Button();
        private readonly Button btnClose = new Button();

        public frmPaymentsList()
        {
            BuildUI();
            LoadPayments();
        }

        private void BuildUI()
        {
            Text = "Payments";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1000, 580);
            MinimumSize = new Size(900, 440);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Payments";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 300, 27);
            txtSearch.TextChanged += (s, e) => LoadPayments();

            lblMethod.Text = "Method";
            lblMethod.AutoSize = false;
            lblMethod.SetBounds(404, 62, 60, 24);
            cmbMethod.SetBounds(468, 58, 170, 27);
            cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMethod.Items.Add("All methods");
            foreach (string method in clsPayment.Methods)
                cmbMethod.Items.Add(method);
            cmbMethod.SelectedIndex = 0;                                   // set BEFORE the event is connected
            cmbMethod.SelectedIndexChanged += (s, e) => LoadPayments();

            clsUiHelper.StyleGrid(dgvPayments);
            dgvPayments.SetBounds(20, 100, 960, 410);
            dgvPayments.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvPayments.DoubleClick += (s, e) => ShowOrder();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 533, 440, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnAdd, "Add Payment", 570, true);
            SetupButton(btnOrder, "View Order", 720, false);
            SetupButton(btnClose, "Close", 860, false);

            btnAdd.Click += (s, e) => AddPayment();
            btnOrder.Click += (s, e) => ShowOrder();
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, lblMethod, cmbMethod, dgvPayments, lblCount,
                btnAdd, btnOrder, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 525, 130, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        private void LoadPayments()
        {
            try
            {
                string method = cmbMethod.SelectedIndex > 0 ? (cmbMethod.SelectedItem?.ToString() ?? string.Empty) : string.Empty;

                DataTable payments = clsPayment.GetAll(txtSearch.Text, method);
                dgvPayments.DataSource = payments;

                clsUiHelper.SetColumn(dgvPayments, "PaymentID", "ID", 25);
                clsUiHelper.SetColumn(dgvPayments, "OrderID", "Order #", 30);
                clsUiHelper.SetColumn(dgvPayments, "CustomerName", "Customer", 90);
                clsUiHelper.SetColumn(dgvPayments, "PaymentDate", "Date", 75, "yyyy-MM-dd HH:mm");
                clsUiHelper.SetColumn(dgvPayments, "Method", "Method", 45);
                clsUiHelper.SetColumn(dgvPayments, "Amount", "Amount", 50, "N2");
                clsUiHelper.SetColumn(dgvPayments, "ReceivedBy", "Received By", 55);

                decimal total = 0;
                foreach (DataRow row in payments.Rows)
                    total += Convert.ToDecimal(row["Amount"]);

                lblCount.Text = payments.Rows.Count + " payments     Total: " + total.ToString("N2");
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void AddPayment()
        {
            using (frmAddPayment form = new frmAddPayment(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadPayments();
            }
        }

        // Opens the order of the selected payment
        private void ShowOrder()
        {
            int? orderID = clsUiHelper.GetSelectedId(dgvPayments, "OrderID");
            if (orderID == null)
            {
                clsUiHelper.ShowError("Please select a payment first.");
                return;
            }

            using (frmOrderDetails form = new frmOrderDetails(orderID.Value))
            {
                form.ShowDialog(this);
            }

            LoadPayments();
        }
    }
}
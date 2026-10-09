
using ECMS_Business;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    public partial class frmCustomersList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly CheckBox chkActiveOnly = new CheckBox();
        private readonly DataGridView dgvCustomers = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnToggle = new Button();
        private readonly Button btnOrders = new Button();
        private readonly Button btnClose = new Button();

        public frmCustomersList()
        {
            BuildUI();
            LoadCustomers();
        }

        private void BuildUI()
        {
            Text = "Customers";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1000, 580);
            MinimumSize = new Size(900, 440);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Customers";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 320, 27);
            txtSearch.TextChanged += (s, e) => LoadCustomers();

            chkActiveOnly.Text = "Show active customers only";
            chkActiveOnly.SetBounds(430, 58, 260, 27);
            chkActiveOnly.CheckedChanged += (s, e) => LoadCustomers();

            clsUiHelper.StyleGrid(dgvCustomers);
            dgvCustomers.SetBounds(20, 100, 960, 410);
            dgvCustomers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvCustomers.DoubleClick += (s, e) => EditSelected();
            dgvCustomers.DataBindingComplete += (s, e) => GreyOutInactiveRows();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 533, 300, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnAdd, "Add", 340, true);
            SetupButton(btnEdit, "Edit", 470, false);
            SetupButton(btnToggle, "Activate / Deactivate", 600, false);
            SetupButton(btnOrders, "View Orders", 730, false);
            SetupButton(btnClose, "Close", 860, false);

            btnToggle.Font = new Font("Segoe UI", 9F);   // longer text

            btnAdd.Click += (s, e) => AddNew();
            btnEdit.Click += (s, e) => EditSelected();
            btnToggle.Click += (s, e) => ToggleActive();
            btnOrders.Click += (s, e) => ViewOrders();
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, chkActiveOnly, dgvCustomers, lblCount,
                btnAdd, btnEdit, btnToggle, btnOrders, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 525, 120, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        private void LoadCustomers()
        {
            try
            {
                dgvCustomers.DataSource = clsCustomer.GetAll(txtSearch.Text, chkActiveOnly.Checked);

                clsUiHelper.SetColumn(dgvCustomers, "CustomerID", "ID", 30);
                clsUiHelper.SetColumn(dgvCustomers, "PersonID", "PersonID", 30, null, false);
                clsUiHelper.SetColumn(dgvCustomers, "FullName", "Customer", 90);
                clsUiHelper.SetColumn(dgvCustomers, "Phone", "Phone", 70);
                clsUiHelper.SetColumn(dgvCustomers, "Email", "Email", 110);
                clsUiHelper.SetColumn(dgvCustomers, "Address", "Address", 100);
                clsUiHelper.SetColumn(dgvCustomers, "CreatedAt", "Created", 65, "yyyy-MM-dd");
                clsUiHelper.SetColumn(dgvCustomers, "IsActive", "Active", 40);

                lblCount.Text = dgvCustomers.Rows.Count + " customers";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Inactive customers are shown in grey
        private void GreyOutInactiveRows()
        {
            foreach (DataGridViewRow row in dgvCustomers.Rows)
            {
                bool isActive = Convert.ToBoolean(row.Cells["IsActive"].Value);
                row.DefaultCellStyle.ForeColor = isActive ? Color.Black : Color.Gray;
            }
        }

        private void AddNew()
        {
            using (frmAddEditCustomer form = new frmAddEditCustomer(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadCustomers();
            }
        }

        private void EditSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvCustomers, "CustomerID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a customer first.");
                return;
            }

            try
            {
                clsCustomer? customer = clsCustomer.Find(id.Value);
                if (customer == null)
                {
                    clsUiHelper.ShowError("This customer no longer exists.");
                    LoadCustomers();
                    return;
                }

                using (frmAddEditCustomer form = new frmAddEditCustomer(customer))
                {
                    form.ShowDialog(this);
                    LoadCustomers();   // the person's data may have changed even if the form was cancelled
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void ToggleActive()
        {
            int? id = clsUiHelper.GetSelectedId(dgvCustomers, "CustomerID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a customer first.");
                return;
            }

            try
            {
                clsCustomer? customer = clsCustomer.Find(id.Value);
                if (customer == null)
                {
                    clsUiHelper.ShowError("This customer no longer exists.");
                    LoadCustomers();
                    return;
                }

                string action = customer.IsActive ? "deactivate" : "activate";
                if (!clsUiHelper.Confirm("Do you want to " + action + " this customer?"))
                    return;

                customer.IsActive = !customer.IsActive;

                if (!customer.Save(out string error))
                    clsUiHelper.ShowError(error);

                LoadCustomers();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void ViewOrders()
        {
            int? id = clsUiHelper.GetSelectedId(dgvCustomers, "CustomerID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a customer first.");
                return;
            }

            string name = Convert.ToString(dgvCustomers.CurrentRow?.Cells["FullName"].Value) ?? string.Empty;

            using (frmCustomerOrders form = new frmCustomerOrders(id.Value, name))
            {
                form.ShowDialog(this);
            }
        }
    }
}
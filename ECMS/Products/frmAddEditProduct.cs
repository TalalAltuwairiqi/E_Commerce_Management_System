using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Used for both "Add product" (product == null) and "Edit product".
    // The stock can only be set when the product is created. After that it changes through
    // stock movements (orders and the Inventory screens), so here it is read-only.
    public partial class frmAddEditProduct : Form
    {
        private readonly clsProduct _product;

        private readonly Label lblTitle = new Label();
        private readonly TextBox txtName = new TextBox();
        private readonly ComboBox cmbCategory = new ComboBox();
        private readonly NumericUpDown nudPrice = new NumericUpDown();
        private readonly NumericUpDown nudReorder = new NumericUpDown();
        private readonly NumericUpDown nudStock = new NumericUpDown();
        private readonly Label lblStockHint = new Label();
        private readonly TextBox txtDescription = new TextBox();
        private readonly CheckBox chkActive = new CheckBox();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmAddEditProduct(clsProduct? product = null)
        {
            _product = product ?? new clsProduct();
            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            bool isNew = _product.IsNew;
            string title = isNew ? "Add Product" : "Edit Product";

            Text = title;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(460, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 420, 36);

            AddLabel("Product name *", 20, 64);
            txtName.SetBounds(20, 88, 420, 27);
            txtName.MaxLength = 100;

            AddLabel("Category *", 20, 130);
            cmbCategory.SetBounds(20, 154, 200, 27);
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;

            AddLabel("Price *", 240, 130);
            nudPrice.SetBounds(240, 154, 200, 27);
            nudPrice.DecimalPlaces = 2;
            nudPrice.Minimum = 0;
            nudPrice.Maximum = 1000000;
            nudPrice.ThousandsSeparator = true;

            AddLabel("Reorder level (low-stock alert)", 20, 196);
            nudReorder.SetBounds(20, 220, 200, 27);
            nudReorder.Minimum = 0;
            nudReorder.Maximum = 100000;

            AddLabel(isNew ? "Initial stock" : "Current stock", 240, 196);
            nudStock.SetBounds(240, 220, 200, 27);
            nudStock.Minimum = 0;
            nudStock.Maximum = 1000000;
            nudStock.Enabled = isNew;   // read-only when editing

            lblStockHint.Text = isNew
                ? "The initial stock is saved in the stock history."
                : "Change the stock in Inventory > Stock Adjustment.";
            lblStockHint.ForeColor = Color.Gray;
            lblStockHint.Font = new Font("Segoe UI", 9F);
            lblStockHint.AutoSize = false;
            lblStockHint.SetBounds(20, 254, 420, 20);

            AddLabel("Description", 20, 284);
            txtDescription.SetBounds(20, 308, 420, 70);
            txtDescription.Multiline = true;
            txtDescription.MaxLength = 1000;

            chkActive.Text = "Active product";
            chkActive.SetBounds(20, 390, 300, 26);

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 422, 420, 34);

            btnSave.Text = "Save";
            btnSave.SetBounds(20, 464, 200, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(240, 464, 200, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, txtName, cmbCategory, nudPrice, nudReorder, nudStock, lblStockHint,
                txtDescription, chkActive, lblError, btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, 215, 22);
            Controls.Add(label);
        }

        private void LoadValues()
        {
            try
            {
                DataTable categories = clsCategory.GetNames();
                cmbCategory.DisplayMember = "Name";
                cmbCategory.ValueMember = "CategoryID";
                cmbCategory.DataSource = categories;

                if (_product.CategoryID > 0)
                    cmbCategory.SelectedValue = _product.CategoryID;
                else
                    cmbCategory.SelectedIndex = categories.Rows.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }

            txtName.Text = _product.Name;
            nudPrice.Value = Math.Min(_product.Price, nudPrice.Maximum);
            nudReorder.Value = Math.Min(_product.ReorderLevel, (int)nudReorder.Maximum);
            nudStock.Value = Math.Min(_product.IsNew ? _product.InitialStock : _product.StockQuantity, (int)nudStock.Maximum);
            txtDescription.Text = _product.Description ?? string.Empty;
            chkActive.Checked = _product.IsActive;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (cmbCategory.SelectedValue == null)
            {
                lblError.Text = "Please create a category first (Catalog > Categories).";
                return;
            }

            _product.Name = txtName.Text;
            _product.CategoryID = Convert.ToInt32(cmbCategory.SelectedValue);
            _product.Price = nudPrice.Value;
            _product.ReorderLevel = (int)nudReorder.Value;
            _product.Description = txtDescription.Text;
            _product.IsActive = chkActive.Checked;

            if (_product.IsNew)
                _product.InitialStock = (int)nudStock.Value;

            try
            {
                if (!_product.Save(clsGlobal.CurrentUser, out string error))
                {
                    lblError.Text = error;
                    return;
                }

                DialogResult = DialogResult.OK;   // closes the form
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
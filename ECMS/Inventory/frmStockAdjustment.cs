using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Restock or correct the stock of one product. Every change is saved in the stock history with a reason.
    public partial class frmStockAdjustment : Form
    {
        private readonly int? _productID;

        private readonly Label lblTitle = new Label();
        private readonly ComboBox cmbProduct = new ComboBox();
        private readonly Label lblInfo = new Label();
        private readonly ComboBox cmbOperation = new ComboBox();
        private readonly NumericUpDown nudQty = new NumericUpDown();
        private readonly Label lblNewStock = new Label();
        private readonly TextBox txtReason = new TextBox();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmStockAdjustment(int? productID = null)
        {
            _productID = productID;
            BuildUI();
            LoadProducts();
        }

        private void BuildUI()
        {
            Text = "Stock Adjustment";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(500, 484);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Stock Adjustment";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 460, 36);

            AddLabel("Product *", 20, 64);
            cmbProduct.SetBounds(20, 88, 460, 27);
            cmbProduct.DropDownStyle = ComboBoxStyle.DropDown;
            cmbProduct.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbProduct.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbProduct.SelectedIndexChanged += (s, e) => UpdatePreview();

            lblInfo.ForeColor = Color.Gray;
            lblInfo.AutoSize = false;
            lblInfo.SetBounds(20, 122, 460, 24);

            AddLabel("Operation *", 20, 158);
            cmbOperation.SetBounds(20, 182, 460, 27);
            cmbOperation.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbOperation.Items.AddRange(new object[]
            {
                "Restock - stock received (adds)",
                "Correction - increase the stock (adds)",
                "Correction - decrease the stock (removes: damaged, lost...)"
            });
            cmbOperation.SelectedIndex = 0;
            cmbOperation.SelectedIndexChanged += (s, e) => UpdatePreview();

            AddLabel("Quantity *", 20, 224);
            nudQty.SetBounds(20, 248, 200, 27);
            nudQty.Minimum = 1;
            nudQty.Maximum = 1000000;
            nudQty.Value = 1;
            nudQty.ValueChanged += (s, e) => UpdatePreview();

            lblNewStock.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblNewStock.AutoSize = false;
            lblNewStock.SetBounds(240, 248, 240, 27);
            lblNewStock.TextAlign = ContentAlignment.MiddleLeft;

            AddLabel("Reason *", 20, 290);
            txtReason.SetBounds(20, 314, 460, 70);
            txtReason.Multiline = true;
            txtReason.MaxLength = 250;

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 392, 460, 34);

            btnSave.Text = "Save";
            btnSave.SetBounds(20, 432, 220, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(260, 432, 220, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, cmbProduct, lblInfo, cmbOperation, nudQty, lblNewStock, txtReason, lblError, btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, 300, 22);
            Controls.Add(label);
        }

        private void LoadProducts()
        {
            try
            {
                DataTable products = clsProduct.GetAll(string.Empty, 0, false, false);   // all products, also inactive
                products.Columns.Add("DisplayText", typeof(string));

                foreach (DataRow row in products.Rows)
                {
                    bool active = Convert.ToBoolean(row["IsActive"]);
                    row["DisplayText"] = row["Name"] + "   |   stock " + row["StockQuantity"] + (active ? "" : "   (inactive)");
                }

                cmbProduct.DisplayMember = "DisplayText";
                cmbProduct.ValueMember = "ProductID";
                cmbProduct.DataSource = products;
                cmbProduct.SelectedIndex = -1;

                if (_productID.HasValue)
                    cmbProduct.SelectedValue = _productID.Value;

                UpdatePreview();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // + for restock / increase, - for decrease
        private int GetSignedQuantity()
        {
            int quantity = (int)nudQty.Value;
            return cmbOperation.SelectedIndex == (int)enStockOperation.DecreaseCorrection ? -quantity : quantity;
        }

        // Shows the current stock and what the stock will be after the change
        private void UpdatePreview()
        {
            lblInfo.Text = string.Empty;
            lblNewStock.Text = string.Empty;

            if (cmbProduct.SelectedItem is DataRowView view)
            {
                int stock = Convert.ToInt32(view["StockQuantity"]);
                int reorder = Convert.ToInt32(view["ReorderLevel"]);
                int newStock = stock + GetSignedQuantity();

                lblInfo.Text = "Current stock: " + stock + "      Reorder level: " + reorder;
                lblNewStock.Text = "New stock: " + newStock;
                lblNewStock.ForeColor = newStock < 0 ? Color.Firebrick : Color.DarkGreen;
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (!(cmbProduct.SelectedValue is int productID))
            {
                lblError.Text = "Please select a product from the list.";
                return;
            }

            enStockOperation operation = (enStockOperation)cmbOperation.SelectedIndex;

            try
            {
                if (!clsInventory.Adjust(productID, operation, (int)nudQty.Value, txtReason.Text,
                                         clsGlobal.CurrentUser, out int newStock, out string error))
                {
                    lblError.Text = error;
                    return;
                }

                clsUiHelper.ShowInfo("The stock was updated. New stock: " + newStock);
                DialogResult = DialogResult.OK;   // closes the form
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
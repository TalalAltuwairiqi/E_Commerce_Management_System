using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // The "cart" of the system: choose a customer, add products, see the totals, save the order.
    public partial class frmNewOrder : Form
    {
        private readonly clsOrder _order = new clsOrder();

        private readonly Label lblTitle = new Label();
        private readonly ComboBox cmbCustomer = new ComboBox();
        private readonly ComboBox cmbProduct = new ComboBox();
        private readonly NumericUpDown nudQty = new NumericUpDown();
        private readonly Button btnAddItem = new Button();
        private readonly Label lblProductInfo = new Label();
        private readonly DataGridView dgvItems = new DataGridView();
        private readonly Button btnRemove = new Button();
        private readonly TextBox txtNotes = new TextBox();
        private readonly Label lblSubtotalCaption = new Label();
        private readonly Label lblSubtotal = new Label();
        private readonly Label lblTaxCaption = new Label();
        private readonly Label lblTax = new Label();
        private readonly Label lblTotalCaption = new Label();
        private readonly Label lblTotal = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmNewOrder()
        {
            BuildUI();
            LoadCustomers();
            LoadProducts();
            RefreshItems();
        }

        private void BuildUI()
        {
            Text = "New Order";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(980, 630);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "New Order";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            AddLabel("Customer *", 20, 56, 300);
            SetupSearchableCombo(cmbCustomer, 20, 80, 440);

            AddLabel("Product *", 20, 120, 300);
            SetupSearchableCombo(cmbProduct, 20, 144, 440);
            cmbProduct.SelectedIndexChanged += (s, e) => ShowProductInfo();

            AddLabel("Quantity", 480, 120, 100);
            nudQty.SetBounds(480, 144, 100, 27);
            nudQty.Minimum = 1;
            nudQty.Maximum = 100000;
            nudQty.Value = 1;

            btnAddItem.Text = "Add to Order";
            btnAddItem.SetBounds(600, 140, 150, 34);
            clsUiHelper.StyleButton(btnAddItem, true);
            btnAddItem.Click += (s, e) => AddItem();

            lblProductInfo.ForeColor = Color.Gray;
            lblProductInfo.Font = new Font("Segoe UI", 9F);
            lblProductInfo.AutoSize = false;
            lblProductInfo.SetBounds(20, 176, 740, 22);

            clsUiHelper.StyleGrid(dgvItems);
            dgvItems.SetBounds(20, 206, 940, 200);

            btnRemove.Text = "Remove Selected";
            btnRemove.SetBounds(20, 412, 170, 34);
            clsUiHelper.StyleButton(btnRemove);
            btnRemove.Click += (s, e) => RemoveSelected();

            AddLabel("Notes", 20, 458, 200);
            txtNotes.SetBounds(20, 482, 590, 80);
            txtNotes.Multiline = true;
            txtNotes.MaxLength = 250;

            SetupTotalRow(lblSubtotalCaption, lblSubtotal, "Subtotal", 458, 24, false);
            SetupTotalRow(lblTaxCaption, lblTax, "Tax", 490, 24, false);
            SetupTotalRow(lblTotalCaption, lblTotal, "Total", 526, 30, true);

            btnSave.Text = "Save Order";
            btnSave.SetBounds(640, 574, 150, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += (s, e) => SaveOrder();

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(810, 574, 150, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            CancelButton = btnCancel;

            // Closing the window with items in the order asks for confirmation
            FormClosing += (s, e) =>
            {
                if (DialogResult != DialogResult.OK && _order.Items.Count > 0
                    && !clsUiHelper.Confirm("Discard this order?"))
                    e.Cancel = true;
            };

            Controls.AddRange(new Control[]
            {
                lblTitle, cmbCustomer, cmbProduct, nudQty, btnAddItem, lblProductInfo, dgvItems, btnRemove,
                txtNotes, lblSubtotalCaption, lblSubtotal, lblTaxCaption, lblTax, lblTotalCaption, lblTotal,
                btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y, int width)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, width, 22);
            Controls.Add(label);
        }

        // A drop-down list where you can also type to find an item
        private static void SetupSearchableCombo(ComboBox combo, int x, int y, int width)
        {
            combo.SetBounds(x, y, width, 27);
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        private static void SetupTotalRow(Label caption, Label value, string text, int y, int height, bool big)
        {
            Font font = big ? new Font("Segoe UI", 13F, FontStyle.Bold) : new Font("Segoe UI", 10F);

            caption.Text = text;
            caption.Font = font;
            caption.AutoSize = false;
            caption.SetBounds(640, y, 170, height);

            value.Font = font;
            value.AutoSize = false;
            value.TextAlign = ContentAlignment.MiddleRight;
            value.SetBounds(810, y, 150, height);
        }

        // ---------------------------------------------------------------
        // Loading the lists
        // ---------------------------------------------------------------
        private void LoadCustomers()
        {
            try
            {
                DataTable customers = clsCustomer.GetAll(string.Empty, true);   // active customers only
                customers.Columns.Add("DisplayText", typeof(string));

                foreach (DataRow row in customers.Rows)
                    row["DisplayText"] = row["FullName"] + "  (" + row["Phone"] + ")";

                cmbCustomer.DisplayMember = "DisplayText";
                cmbCustomer.ValueMember = "CustomerID";
                cmbCustomer.DataSource = customers;
                cmbCustomer.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void LoadProducts()
        {
            try
            {
                DataTable products = clsProduct.GetAll(string.Empty, 0, true, false);   // active products only
                products.Columns.Add("DisplayText", typeof(string));

                foreach (DataRow row in products.Rows)
                {
                    decimal price = Convert.ToDecimal(row["Price"]);
                    row["DisplayText"] = row["Name"] + "   |   " + price.ToString("N2") + "   |   stock " + row["StockQuantity"];
                }

                DataView sellable = new DataView(products) { RowFilter = "StockQuantity > 0" };

                cmbProduct.DisplayMember = "DisplayText";
                cmbProduct.ValueMember = "ProductID";
                cmbProduct.DataSource = sellable;
                cmbProduct.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void ShowProductInfo()
        {
            lblProductInfo.Text = string.Empty;

            if (cmbProduct.SelectedValue is int productID)
            {
                try
                {
                    clsProduct? product = clsProduct.Find(productID);
                    if (product != null)
                        lblProductInfo.Text = "Price: " + product.Price.ToString("N2") + "     In stock: " + product.StockQuantity;
                }
                catch
                {
                    // the info line is only a hint, so errors here are ignored
                }
            }
        }

        // ---------------------------------------------------------------
        // Building the order
        // ---------------------------------------------------------------
        private void AddItem()
        {
            if (!(cmbProduct.SelectedValue is int productID))
            {
                clsUiHelper.ShowError("Please select a product from the list.");
                return;
            }

            try
            {
                // Read the product again so the price and stock are up to date
                clsProduct? product = clsProduct.Find(productID);
                if (product == null)
                {
                    clsUiHelper.ShowError("This product no longer exists.");
                    return;
                }

                if (!_order.AddItem(product, (int)nudQty.Value, out string error))
                {
                    clsUiHelper.ShowError(error);
                    return;
                }

                nudQty.Value = 1;
                RefreshItems();
                ShowProductInfo();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void RemoveSelected()
        {
            int? productID = clsUiHelper.GetSelectedId(dgvItems, "ProductID");
            if (productID == null)
            {
                clsUiHelper.ShowError("Please select an item in the list first.");
                return;
            }

            _order.RemoveItem(productID.Value);
            RefreshItems();
        }

        // Redraws the items grid and the totals from the order object
        private void RefreshItems()
        {
            DataTable table = new DataTable();
            table.Columns.Add("ProductID", typeof(int));
            table.Columns.Add("Product", typeof(string));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("LineTotal", typeof(decimal));

            foreach (clsOrderItem item in _order.Items)
                table.Rows.Add(item.ProductID, item.ProductName, item.UnitPrice, item.Quantity, item.LineTotal);

            dgvItems.DataSource = table;

            clsUiHelper.SetColumn(dgvItems, "ProductID", "ID", 20, null, false);
            clsUiHelper.SetColumn(dgvItems, "Product", "Product", 120);
            clsUiHelper.SetColumn(dgvItems, "UnitPrice", "Unit Price", 40, "N2");
            clsUiHelper.SetColumn(dgvItems, "Quantity", "Quantity", 30);
            clsUiHelper.SetColumn(dgvItems, "LineTotal", "Line Total", 40, "N2");

            lblSubtotal.Text = _order.Subtotal.ToString("N2");
            lblTaxCaption.Text = "Tax (" + (_order.TaxRate * 100).ToString("0.##") + "%)";
            lblTax.Text = _order.TaxAmount.ToString("N2");
            lblTotal.Text = _order.TotalAmount.ToString("N2");
        }

        // ---------------------------------------------------------------
        // Saving
        // ---------------------------------------------------------------
        private void SaveOrder()
        {
            if (!(cmbCustomer.SelectedValue is int customerID))
            {
                clsUiHelper.ShowError("Please select a customer from the list.");
                return;
            }

            _order.CustomerID = customerID;
            _order.Notes = txtNotes.Text;

            try
            {
                if (!_order.Save(clsGlobal.CurrentUser, out string error))
                {
                    clsUiHelper.ShowError(error);
                    return;
                }

                clsUiHelper.ShowInfo("Order #" + _order.OrderID + " was saved.\n\nTotal: " + _order.SavedTotalAmount.ToString("N2"));
                DialogResult = DialogResult.OK;   // closes the form
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
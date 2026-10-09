
using ECMS_Business;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    public partial class frmProductsList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly Label lblCategory = new Label();
        private readonly ComboBox cmbCategory = new ComboBox();
        private readonly CheckBox chkActiveOnly = new CheckBox();
        private readonly CheckBox chkLowStock = new CheckBox();
        private readonly DataGridView dgvProducts = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnToggle = new Button();
        private readonly Button btnClose = new Button();

        private bool _loadingFilters;   // stops the category list from reloading the grid while it is being filled

        public frmProductsList()
        {
            BuildUI();
            LoadCategoryFilter();
            LoadProducts();
        }

        private void BuildUI()
        {
            Text = "Products";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1040, 600);
            MinimumSize = new Size(960, 460);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Products";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 250, 27);
            txtSearch.TextChanged += (s, e) => LoadProducts();

            lblCategory.Text = "Category";
            lblCategory.AutoSize = false;
            lblCategory.SetBounds(350, 62, 70, 24);
            cmbCategory.SetBounds(424, 58, 200, 27);
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.SelectedIndexChanged += (s, e) => { if (!_loadingFilters) LoadProducts(); };

            chkActiveOnly.Text = "Active only";
            chkActiveOnly.SetBounds(650, 58, 130, 27);
            chkActiveOnly.CheckedChanged += (s, e) => LoadProducts();

            chkLowStock.Text = "Low stock only";
            chkLowStock.SetBounds(790, 58, 200, 27);
            chkLowStock.CheckedChanged += (s, e) => LoadProducts();

            clsUiHelper.StyleGrid(dgvProducts);
            dgvProducts.SetBounds(20, 100, 1000, 430);
            dgvProducts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvProducts.DoubleClick += (s, e) => EditSelected();
            dgvProducts.DataBindingComplete += (s, e) => HighlightRows();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 553, 480, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnAdd, "Add", 510, true);
            SetupButton(btnEdit, "Edit", 640, false);
            SetupButton(btnToggle, "Activate / Deactivate", 770, false);
            SetupButton(btnClose, "Close", 900, false);

            btnToggle.Font = new Font("Segoe UI", 9F);   // longer text

            btnAdd.Click += (s, e) => AddNew();
            btnEdit.Click += (s, e) => EditSelected();
            btnToggle.Click += (s, e) => ToggleActive();
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, lblCategory, cmbCategory, chkActiveOnly, chkLowStock,
                dgvProducts, lblCount, btnAdd, btnEdit, btnToggle, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 545, 120, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        // Fills the category drop-down with "All categories" + every category
        private void LoadCategoryFilter()
        {
            _loadingFilters = true;

            try
            {
                DataTable categories = clsCategory.GetNames();

                DataRow all = categories.NewRow();
                all["CategoryID"] = 0;
                all["Name"] = "All categories";
                categories.Rows.InsertAt(all, 0);

                cmbCategory.DisplayMember = "Name";
                cmbCategory.ValueMember = "CategoryID";
                cmbCategory.DataSource = categories;
                cmbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
            finally
            {
                _loadingFilters = false;
            }
        }

        private void LoadProducts()
        {
            try
            {
                object? selected = cmbCategory.SelectedValue;
                int categoryID = selected is int id ? id : 0;

                dgvProducts.DataSource = clsProduct.GetAll(txtSearch.Text, categoryID,
                                                           chkActiveOnly.Checked, chkLowStock.Checked);

                clsUiHelper.SetColumn(dgvProducts, "ProductID", "ID", 28);
                clsUiHelper.SetColumn(dgvProducts, "Name", "Product", 110);
                clsUiHelper.SetColumn(dgvProducts, "CategoryID", "CategoryID", 30, null, false);
                clsUiHelper.SetColumn(dgvProducts, "Category", "Category", 70);
                clsUiHelper.SetColumn(dgvProducts, "Price", "Price", 55, "N2");
                clsUiHelper.SetColumn(dgvProducts, "StockQuantity", "Stock", 40);
                clsUiHelper.SetColumn(dgvProducts, "ReorderLevel", "Reorder Level", 50);
                clsUiHelper.SetColumn(dgvProducts, "IsActive", "Active", 40);

                lblCount.Text = dgvProducts.Rows.Count + " products     (red = low stock, grey = inactive)";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Low-stock products get a light red background; inactive products are shown in grey
        private void HighlightRows()
        {
            foreach (DataGridViewRow row in dgvProducts.Rows)
            {
                bool isActive = Convert.ToBoolean(row.Cells["IsActive"].Value);
                int stock = Convert.ToInt32(row.Cells["StockQuantity"].Value);
                int reorder = Convert.ToInt32(row.Cells["ReorderLevel"].Value);

                row.DefaultCellStyle.ForeColor = isActive ? Color.Black : Color.Gray;
                row.DefaultCellStyle.BackColor = (isActive && stock <= reorder)
                    ? Color.FromArgb(255, 228, 225)
                    : Color.Empty;
            }
        }

        private void AddNew()
        {
            using (frmAddEditProduct form = new frmAddEditProduct(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadProducts();
            }
        }

        private void EditSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvProducts, "ProductID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a product first.");
                return;
            }

            try
            {
                clsProduct? product = clsProduct.Find(id.Value);
                if (product == null)
                {
                    clsUiHelper.ShowError("This product no longer exists.");
                    LoadProducts();
                    return;
                }

                using (frmAddEditProduct form = new frmAddEditProduct(product))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                        LoadProducts();
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Products are never deleted (old orders refer to them): they are deactivated instead.
        private void ToggleActive()
        {
            int? id = clsUiHelper.GetSelectedId(dgvProducts, "ProductID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a product first.");
                return;
            }

            try
            {
                clsProduct? product = clsProduct.Find(id.Value);
                if (product == null)
                {
                    clsUiHelper.ShowError("This product no longer exists.");
                    LoadProducts();
                    return;
                }

                string action = product.IsActive ? "deactivate" : "activate";
                if (!clsUiHelper.Confirm("Do you want to " + action + " '" + product.Name + "'?"))
                    return;

                product.IsActive = !product.IsActive;

                if (!product.Save(clsGlobal.CurrentUser, out string error))
                    clsUiHelper.ShowError(error);

                LoadProducts();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
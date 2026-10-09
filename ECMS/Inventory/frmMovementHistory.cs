using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // The stock history: every sale, cancellation, restock and correction.
    public partial class frmMovementHistory : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblProduct = new Label();
        private readonly ComboBox cmbProduct = new ComboBox();
        private readonly Label lblType = new Label();
        private readonly ComboBox cmbType = new ComboBox();
        private readonly DataGridView dgvMovements = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdjust = new Button();
        private readonly Button btnClose = new Button();

        private bool _loadingFilters;

        public frmMovementHistory()
        {
            BuildUI();
            LoadProductFilter();
            LoadMovements();
        }

        private void BuildUI()
        {
            Text = "Movement History";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1100, 600);
            MinimumSize = new Size(960, 460);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Stock Movement History";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 600, 36);

            lblProduct.Text = "Product";
            lblProduct.AutoSize = false;
            lblProduct.SetBounds(20, 62, 65, 24);
            cmbProduct.SetBounds(90, 58, 300, 27);
            cmbProduct.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProduct.SelectedIndexChanged += (s, e) => { if (!_loadingFilters) LoadMovements(); };

            lblType.Text = "Type";
            lblType.AutoSize = false;
            lblType.SetBounds(420, 62, 45, 24);
            cmbType.SetBounds(468, 58, 180, 27);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("All types");
            foreach (string type in clsInventory.MovementTypes)
                cmbType.Items.Add(type);
            cmbType.SelectedIndex = 0;                                       // set BEFORE the event is connected
            cmbType.SelectedIndexChanged += (s, e) => LoadMovements();

            clsUiHelper.StyleGrid(dgvMovements);
            dgvMovements.SetBounds(20, 100, 1060, 420);
            dgvMovements.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvMovements.DataBindingComplete += (s, e) => ColourChanges();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 553, 500, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            btnAdjust.Text = "Stock Adjustment";
            btnAdjust.SetBounds(800, 545, 160, 38);
            btnAdjust.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnAdjust, true);
            btnAdjust.Click += (s, e) => NewAdjustment();

            btnClose.Text = "Close";
            btnClose.SetBounds(980, 545, 100, 38);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblProduct, cmbProduct, lblType, cmbType, dgvMovements, lblCount, btnAdjust, btnClose
            });
        }

        // "All products" + every product
        private void LoadProductFilter()
        {
            _loadingFilters = true;

            try
            {
                DataTable products = clsProduct.GetAll(string.Empty, 0, false, false);

                DataTable list = new DataTable();
                list.Columns.Add("ProductID", typeof(int));
                list.Columns.Add("Name", typeof(string));
                list.Rows.Add(0, "All products");

                foreach (DataRow row in products.Rows)
                    list.Rows.Add(row["ProductID"], row["Name"]);

                cmbProduct.DisplayMember = "Name";
                cmbProduct.ValueMember = "ProductID";
                cmbProduct.DataSource = list;
                cmbProduct.SelectedIndex = 0;
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

        private void LoadMovements()
        {
            try
            {
                int productID = cmbProduct.SelectedValue is int id ? id : 0;
                string type = cmbType.SelectedIndex > 0 ? (cmbType.SelectedItem?.ToString() ?? string.Empty) : string.Empty;

                dgvMovements.DataSource = clsInventory.GetMovements(productID, type);

                clsUiHelper.SetColumn(dgvMovements, "MovementID", "ID", 25);
                clsUiHelper.SetColumn(dgvMovements, "MovementDate", "Date", 70, "yyyy-MM-dd HH:mm");
                clsUiHelper.SetColumn(dgvMovements, "ProductName", "Product", 90);
                clsUiHelper.SetColumn(dgvMovements, "MovementType", "Type", 55);
                clsUiHelper.SetColumn(dgvMovements, "QuantityChange", "Change", 40, "+#;-#;0");
                clsUiHelper.SetColumn(dgvMovements, "OrderID", "Order #", 35);
                clsUiHelper.SetColumn(dgvMovements, "UserName", "User", 45);
                clsUiHelper.SetColumn(dgvMovements, "Notes", "Notes", 100);

                lblCount.Text = dgvMovements.Rows.Count + " movements (the latest 1000 are shown)";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Stock in = green, stock out = red
        private void ColourChanges()
        {
            foreach (DataGridViewRow row in dgvMovements.Rows)
            {
                int change = Convert.ToInt32(row.Cells["QuantityChange"].Value);
                row.Cells["QuantityChange"].Style.ForeColor = change > 0 ? Color.DarkGreen : Color.Firebrick;
            }
        }

        private void NewAdjustment()
        {
            using (frmStockAdjustment form = new frmStockAdjustment(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadMovements();
            }
        }
    }
}
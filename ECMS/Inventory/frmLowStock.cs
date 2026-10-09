using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Products at or below their reorder level, with a shortcut to restock them.
    public partial class frmLowStock : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly DataGridView dgvLowStock = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnRestock = new Button();
        private readonly Button btnClose = new Button();

        public frmLowStock()
        {
            BuildUI();
            LoadLowStock();
        }

        private void BuildUI()
        {
            Text = "Low Stock";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(860, 520);
            MinimumSize = new Size(700, 380);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Low Stock";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            clsUiHelper.StyleGrid(dgvLowStock);
            dgvLowStock.SetBounds(20, 60, 820, 390);
            dgvLowStock.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvLowStock.DoubleClick += (s, e) => Restock();
            dgvLowStock.DataBindingComplete += (s, e) => TintRows();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 473, 400, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            btnRestock.Text = "Restock Selected";
            btnRestock.SetBounds(560, 465, 170, 38);
            btnRestock.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnRestock, true);
            btnRestock.Click += (s, e) => Restock();

            btnClose.Text = "Close";
            btnClose.SetBounds(740, 465, 100, 38);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { lblTitle, dgvLowStock, lblCount, btnRestock, btnClose });
        }

        private void LoadLowStock()
        {
            try
            {
                dgvLowStock.DataSource = clsInventory.GetLowStock();

                clsUiHelper.SetColumn(dgvLowStock, "ProductID", "ID", 25);
                clsUiHelper.SetColumn(dgvLowStock, "Name", "Product", 110);
                clsUiHelper.SetColumn(dgvLowStock, "Category", "Category", 70);
                clsUiHelper.SetColumn(dgvLowStock, "StockQuantity", "Stock", 40);
                clsUiHelper.SetColumn(dgvLowStock, "ReorderLevel", "Reorder Level", 50);

                int count = dgvLowStock.Rows.Count;
                lblCount.Text = count == 0
                    ? "No product is low on stock."
                    : count + " product(s) at or below the reorder level";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void TintRows()
        {
            foreach (DataGridViewRow row in dgvLowStock.Rows)
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 228, 225);
        }

        private void Restock()
        {
            int? productID = clsUiHelper.GetSelectedId(dgvLowStock, "ProductID");
            if (productID == null)
            {
                clsUiHelper.ShowError("Please select a product first.");
                return;
            }

            using (frmStockAdjustment form = new frmStockAdjustment(productID.Value))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadLowStock();
            }
        }
    }
}
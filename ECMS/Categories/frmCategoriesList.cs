using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    public partial class frmCategoriesList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly DataGridView dgvCategories = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnClose = new Button();

        public frmCategoriesList()
        {
            BuildUI();
            LoadCategories();
        }

        private void BuildUI()
        {
            Text = "Categories";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(820, 540);
            MinimumSize = new Size(700, 400);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Categories";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 320, 27);
            txtSearch.TextChanged += (s, e) => LoadCategories();

            clsUiHelper.StyleGrid(dgvCategories);
            dgvCategories.SetBounds(20, 100, 780, 370);
            dgvCategories.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvCategories.DoubleClick += (s, e) => EditSelected();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 493, 300, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnAdd, "Add", 340, true);
            SetupButton(btnEdit, "Edit", 460, false);
            SetupButton(btnDelete, "Delete", 580, false);
            SetupButton(btnClose, "Close", 690, false);

            btnAdd.Click += (s, e) => AddNew();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnClose.Click += (s, e) => Close();

            // Only an Admin sees the Delete button
            btnDelete.Visible = clsGlobal.CurrentUser?.IsAdmin ?? false;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, dgvCategories, lblCount, btnAdd, btnEdit, btnDelete, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 485, 100, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        private void LoadCategories()
        {
            try
            {
                dgvCategories.DataSource = clsCategory.GetAll(txtSearch.Text);

                clsUiHelper.SetColumn(dgvCategories, "CategoryID", "ID", 25);
                clsUiHelper.SetColumn(dgvCategories, "Name", "Name", 70);
                clsUiHelper.SetColumn(dgvCategories, "Description", "Description", 140);
                clsUiHelper.SetColumn(dgvCategories, "ProductCount", "Products", 35);

                lblCount.Text = dgvCategories.Rows.Count + " categories";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void AddNew()
        {
            using (frmAddEditCategory form = new frmAddEditCategory(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadCategories();
            }
        }

        private void EditSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvCategories, "CategoryID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a category first.");
                return;
            }

            try
            {
                clsCategory? category = clsCategory.Find(id.Value);
                if (category == null)
                {
                    clsUiHelper.ShowError("This category no longer exists.");
                    LoadCategories();
                    return;
                }

                using (frmAddEditCategory form = new frmAddEditCategory(category))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                        LoadCategories();
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void DeleteSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvCategories, "CategoryID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a category first.");
                return;
            }

            if (!clsUiHelper.Confirm("Do you want to delete this category?"))
                return;

            try
            {
                if (clsCategory.Delete(id.Value, clsGlobal.CurrentUser, out string error))
                {
                    clsUiHelper.ShowInfo("The category was deleted.");
                    LoadCategories();
                }
                else
                {
                    clsUiHelper.ShowError(error);
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
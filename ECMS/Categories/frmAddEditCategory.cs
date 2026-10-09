using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Used for both "Add category" (category == null) and "Edit category".
    public partial class frmAddEditCategory : Form
    {
        private readonly clsCategory _category;

        private readonly Label lblTitle = new Label();
        private readonly TextBox txtName = new TextBox();
        private readonly TextBox txtDescription = new TextBox();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmAddEditCategory(clsCategory? category = null)
        {
            _category = category ?? new clsCategory();
            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            string title = _category.IsNew ? "Add Category" : "Edit Category";

            Text = title;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(460, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 420, 36);

            AddLabel("Name *", 20, 64);
            txtName.SetBounds(20, 88, 420, 27);
            txtName.MaxLength = 50;

            AddLabel("Description", 20, 130);
            txtDescription.SetBounds(20, 154, 420, 70);
            txtDescription.Multiline = true;
            txtDescription.MaxLength = 250;

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 232, 420, 34);

            btnSave.Text = "Save";
            btnSave.SetBounds(20, 272, 200, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(240, 272, 200, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lblTitle, txtName, txtDescription, lblError, btnSave, btnCancel });
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, 200, 22);
            Controls.Add(label);
        }

        private void LoadValues()
        {
            txtName.Text = _category.Name;
            txtDescription.Text = _category.Description ?? string.Empty;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            _category.Name = txtName.Text;
            _category.Description = txtDescription.Text;

            try
            {
                if (!_category.Save(out string error))
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
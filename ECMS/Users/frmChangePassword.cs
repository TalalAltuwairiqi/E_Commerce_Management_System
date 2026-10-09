using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // requireCurrent = true  -> "Change my password" (asks for the current password)
    // requireCurrent = false -> Admin resets the password of another user
    public partial class frmChangePassword : Form
    {
        private readonly int _userID;
        private readonly string _username;
        private readonly bool _requireCurrent;

        private readonly Label lblTitle = new Label();
        private readonly TextBox txtCurrent = new TextBox();
        private readonly TextBox txtNew = new TextBox();
        private readonly TextBox txtConfirm = new TextBox();
        private readonly Label lblHint = new Label();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmChangePassword(int userID, string username, bool requireCurrent)
        {
            _userID = userID;
            _username = username;
            _requireCurrent = requireCurrent;
            BuildUI();
        }

        private void BuildUI()
        {
            string title = _requireCurrent ? "Change Password" : "Reset Password";

            Text = title;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = _requireCurrent ? title : title + " - " + _username;
            lblTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 360, 36);
            Controls.Add(lblTitle);

            int y = 64;

            if (_requireCurrent)
            {
                AddLabel("Current password *", y);
                txtCurrent.SetBounds(20, y + 24, 360, 27);
                txtCurrent.UseSystemPasswordChar = true;
                txtCurrent.MaxLength = 64;
                Controls.Add(txtCurrent);
                y += 66;
            }

            AddLabel("New password *", y);
            txtNew.SetBounds(20, y + 24, 360, 27);
            txtNew.UseSystemPasswordChar = true;
            txtNew.MaxLength = 64;
            Controls.Add(txtNew);

            AddLabel("Confirm new password *", y + 66);
            txtConfirm.SetBounds(20, y + 90, 360, 27);
            txtConfirm.UseSystemPasswordChar = true;
            txtConfirm.MaxLength = 64;
            Controls.Add(txtConfirm);

            lblHint.Text = "At least 8 characters, with letters and digits.";
            lblHint.ForeColor = Color.Gray;
            lblHint.Font = new Font("Segoe UI", 9F);
            lblHint.AutoSize = false;
            lblHint.SetBounds(20, y + 124, 360, 20);
            Controls.Add(lblHint);

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, y + 148, 360, 34);
            Controls.Add(lblError);

            int yButtons = y + 190;

            btnSave.Text = "Save";
            btnSave.SetBounds(20, yButtons, 170, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(210, yButtons, 170, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            ClientSize = new Size(400, yButtons + 60);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int y)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(20, y, 360, 22);
            Controls.Add(label);
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (txtNew.Text != txtConfirm.Text)
            {
                lblError.Text = "The two new passwords do not match.";
                return;
            }

            try
            {
                string error;
                bool ok;

                if (_requireCurrent)
                    ok = clsUser.ChangeOwnPassword(clsGlobal.CurrentUser, txtCurrent.Text, txtNew.Text, out error);
                else
                    ok = clsUser.ResetPassword(clsGlobal.CurrentUser, _userID, txtNew.Text, out error);

                if (!ok)
                {
                    lblError.Text = error;
                    return;
                }

                clsUiHelper.ShowInfo("The password was changed successfully.");
                DialogResult = DialogResult.OK;   // closes the form
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
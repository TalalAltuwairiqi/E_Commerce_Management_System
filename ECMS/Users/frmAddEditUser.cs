using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Used for both "Add user" (user == null) and "Edit user".
    // In Edit mode the password is not shown: use "Reset Password" in the users list instead.
    public partial class frmAddEditUser : Form
    {
        private readonly clsUser _user;

        private readonly Label lblTitle = new Label();
        private readonly ComboBox cmbPerson = new ComboBox();     // Add mode: choose a person
        private readonly TextBox txtPerson = new TextBox();       // Edit mode: shows the person (read-only)
        private readonly Button btnPerson = new Button();         // "New Person..." / "Edit Person..."
        private readonly TextBox txtUsername = new TextBox();
        private readonly ComboBox cmbRole = new ComboBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly TextBox txtConfirm = new TextBox();
        private readonly CheckBox chkActive = new CheckBox();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmAddEditUser(clsUser? user = null)
        {
            _user = user ?? new clsUser();
            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            bool isNew = _user.IsNew;
            string title = isNew ? "Add User" : "Edit User";

            Text = title;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 420, 36);

            AddLabel(isNew ? "Select a person *" : "Person", 20, 64, 420);
            cmbPerson.SetBounds(20, 88, 420, 27);
            cmbPerson.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPerson.Visible = isNew;

            txtPerson.SetBounds(20, 88, 420, 27);
            txtPerson.ReadOnly = true;
            txtPerson.Visible = !isNew;

            btnPerson.Text = isNew ? "New Person..." : "Edit Person...";
            btnPerson.SetBounds(20, 128, 200, 34);
            clsUiHelper.StyleButton(btnPerson);
            btnPerson.Click += btnPerson_Click;

            AddLabel("Username *", 20, 176, 200);
            txtUsername.SetBounds(20, 200, 200, 27);
            txtUsername.MaxLength = 50;

            AddLabel("Role *", 240, 176, 200);
            cmbRole.SetBounds(240, 200, 200, 27);
            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.Items.AddRange(new object[] { "Admin", "Staff" });

            int yActive;

            if (isNew)
            {
                AddLabel("Password *", 20, 242, 200);
                txtPassword.SetBounds(20, 266, 200, 27);
                txtPassword.UseSystemPasswordChar = true;
                txtPassword.MaxLength = 64;

                AddLabel("Confirm password *", 240, 242, 200);
                txtConfirm.SetBounds(240, 266, 200, 27);
                txtConfirm.UseSystemPasswordChar = true;
                txtConfirm.MaxLength = 64;

                Controls.Add(txtPassword);
                Controls.Add(txtConfirm);
                yActive = 312;
            }
            else
            {
                yActive = 242;
            }

            chkActive.Text = "Active user";
            chkActive.SetBounds(20, yActive, 300, 26);

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, yActive + 36, 420, 34);

            int yButtons = yActive + 80;

            btnSave.Text = "Save";
            btnSave.SetBounds(20, yButtons, 200, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(240, yButtons, 200, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            ClientSize = new Size(460, yButtons + 60);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, cmbPerson, txtPerson, btnPerson, txtUsername, cmbRole,
                chkActive, lblError, btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y, int width)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, width, 22);
            Controls.Add(label);
        }

        private void LoadValues()
        {
            txtUsername.Text = _user.Username;
            cmbRole.SelectedItem = _user.IsAdmin ? "Admin" : "Staff";
            chkActive.Checked = _user.IsActive;

            try
            {
                if (_user.IsNew)
                    LoadPeopleCombo(null);
                else
                    ShowPersonText();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Fills the list with people who do not have a user account yet
        private void LoadPeopleCombo(int? selectPersonID)
        {
            DataTable people = clsUser.GetPeopleWithoutUser();

            cmbPerson.DisplayMember = "DisplayName";
            cmbPerson.ValueMember = "PersonID";
            cmbPerson.DataSource = people;

            if (selectPersonID.HasValue)
                cmbPerson.SelectedValue = selectPersonID.Value;
            else
                cmbPerson.SelectedIndex = people.Rows.Count > 0 ? 0 : -1;
        }

        private void ShowPersonText()
        {
            clsPerson? person = _user.GetPerson();
            txtPerson.Text = person == null ? string.Empty : person.FullName + "  (" + person.Phone + ")";
        }

        private void btnPerson_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_user.IsNew)
                {
                    // Create a brand-new person, then select it in the list
                    using (frmAddEditPerson form = new frmAddEditPerson(null))
                    {
                        if (form.ShowDialog(this) == DialogResult.OK)
                            LoadPeopleCombo(form.SavedPersonID);
                    }
                }
                else
                {
                    clsPerson? person = _user.GetPerson();
                    if (person == null)
                        return;

                    using (frmAddEditPerson form = new frmAddEditPerson(person))
                    {
                        if (form.ShowDialog(this) == DialogResult.OK)
                            ShowPersonText();
                    }
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (_user.IsNew)
            {
                if (cmbPerson.SelectedValue == null)
                {
                    lblError.Text = "Please select a person or create a new one.";
                    return;
                }

                if (txtPassword.Text != txtConfirm.Text)
                {
                    lblError.Text = "The two passwords do not match.";
                    return;
                }

                _user.PersonID = Convert.ToInt32(cmbPerson.SelectedValue);
            }

            _user.Username = txtUsername.Text;
            _user.Role = cmbRole.SelectedItem?.ToString() ?? "Staff";
            _user.IsActive = chkActive.Checked;

            try
            {
                if (!_user.Save(txtPassword.Text, clsGlobal.CurrentUser, out string error))
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
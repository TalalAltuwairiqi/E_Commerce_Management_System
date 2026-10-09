
using ECMS_Business;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    // Admin only (the menu is hidden for Staff, and the Business layer checks the role on every change).
    public partial class frmUsersList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly CheckBox chkActiveOnly = new CheckBox();
        private readonly DataGridView dgvUsers = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnToggle = new Button();
        private readonly Button btnReset = new Button();
        private readonly Button btnClose = new Button();

        public frmUsersList()
        {
            BuildUI();
            LoadUsers();
        }

        private void BuildUI()
        {
            Text = "Users";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1000, 580);
            MinimumSize = new Size(900, 440);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "Users";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 320, 27);
            txtSearch.TextChanged += (s, e) => LoadUsers();

            chkActiveOnly.Text = "Show active users only";
            chkActiveOnly.SetBounds(430, 58, 260, 27);
            chkActiveOnly.CheckedChanged += (s, e) => LoadUsers();

            clsUiHelper.StyleGrid(dgvUsers);
            dgvUsers.SetBounds(20, 100, 960, 410);
            dgvUsers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvUsers.DoubleClick += (s, e) => EditSelected();
            dgvUsers.DataBindingComplete += (s, e) => GreyOutInactiveRows();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 533, 300, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnAdd, "Add", 340, true);
            SetupButton(btnEdit, "Edit", 470, false);
            SetupButton(btnToggle, "Activate / Deactivate", 600, false);
            SetupButton(btnReset, "Reset Password", 730, false);
            SetupButton(btnClose, "Close", 860, false);

            btnToggle.Font = new Font("Segoe UI", 9F);   // longer text

            btnAdd.Click += (s, e) => AddNew();
            btnEdit.Click += (s, e) => EditSelected();
            btnToggle.Click += (s, e) => ToggleActive();
            btnReset.Click += (s, e) => ResetPassword();
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, chkActiveOnly, dgvUsers, lblCount,
                btnAdd, btnEdit, btnToggle, btnReset, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 525, 120, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        private void LoadUsers()
        {
            try
            {
                dgvUsers.DataSource = clsUser.GetAll(txtSearch.Text, chkActiveOnly.Checked);

                clsUiHelper.SetColumn(dgvUsers, "UserID", "ID", 30);
                clsUiHelper.SetColumn(dgvUsers, "PersonID", "PersonID", 30, null, false);
                clsUiHelper.SetColumn(dgvUsers, "FullName", "Name", 90);
                clsUiHelper.SetColumn(dgvUsers, "Username", "Username", 70);
                clsUiHelper.SetColumn(dgvUsers, "Role", "Role", 50);
                clsUiHelper.SetColumn(dgvUsers, "IsActive", "Active", 40);
                clsUiHelper.SetColumn(dgvUsers, "CreatedAt", "Created", 70, "yyyy-MM-dd");

                lblCount.Text = dgvUsers.Rows.Count + " users";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Inactive users are shown in grey
        private void GreyOutInactiveRows()
        {
            foreach (DataGridViewRow row in dgvUsers.Rows)
            {
                bool isActive = Convert.ToBoolean(row.Cells["IsActive"].Value);
                row.DefaultCellStyle.ForeColor = isActive ? Color.Black : Color.Gray;
            }
        }

        private void AddNew()
        {
            using (frmAddEditUser form = new frmAddEditUser(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadUsers();
            }
        }

        private void EditSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvUsers, "UserID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a user first.");
                return;
            }

            try
            {
                clsUser? user = clsUser.Find(id.Value);
                if (user == null)
                {
                    clsUiHelper.ShowError("This user no longer exists.");
                    LoadUsers();
                    return;
                }

                using (frmAddEditUser form = new frmAddEditUser(user))
                {
                    form.ShowDialog(this);
                    LoadUsers();   // the person's name may have changed even if the form was cancelled
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void ToggleActive()
        {
            int? id = clsUiHelper.GetSelectedId(dgvUsers, "UserID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a user first.");
                return;
            }

            try
            {
                clsUser? user = clsUser.Find(id.Value);
                if (user == null)
                {
                    clsUiHelper.ShowError("This user no longer exists.");
                    LoadUsers();
                    return;
                }

                string action = user.IsActive ? "deactivate" : "activate";
                if (!clsUiHelper.Confirm("Do you want to " + action + " the user '" + user.Username + "'?"))
                    return;

                user.IsActive = !user.IsActive;

                if (!user.Save(null, clsGlobal.CurrentUser, out string error))
                    clsUiHelper.ShowError(error);

                LoadUsers();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void ResetPassword()
        {
            int? id = clsUiHelper.GetSelectedId(dgvUsers, "UserID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a user first.");
                return;
            }

            string username = Convert.ToString(dgvUsers.CurrentRow?.Cells["Username"].Value) ?? string.Empty;

            using (frmChangePassword form = new frmChangePassword(id.Value, username, false))
            {
                form.ShowDialog(this);
            }
        }
    }
}
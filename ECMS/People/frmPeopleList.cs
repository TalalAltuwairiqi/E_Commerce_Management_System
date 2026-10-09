
using ECMS_Business;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECMS
{
    public partial class frmPeopleList : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblSearch = new Label();
        private readonly TextBox txtSearch = new TextBox();
        private readonly DataGridView dgvPeople = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnClose = new Button();

        public frmPeopleList()
        {
            BuildUI();
            LoadPeople();
        }

        private void BuildUI()
        {
            Text = "People";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(920, 560);
            MinimumSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = "People";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 12, 400, 36);

            lblSearch.Text = "Search";
            lblSearch.AutoSize = false;
            lblSearch.SetBounds(20, 62, 60, 24);
            txtSearch.SetBounds(84, 58, 320, 27);
            txtSearch.TextChanged += (s, e) => LoadPeople();

            clsUiHelper.StyleGrid(dgvPeople);
            dgvPeople.SetBounds(20, 100, 880, 390);
            dgvPeople.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvPeople.DoubleClick += (s, e) => EditSelected();

            lblCount.AutoSize = false;
            lblCount.SetBounds(20, 513, 300, 24);
            lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            SetupButton(btnAdd, "Add", 420, true);
            SetupButton(btnEdit, "Edit", 540, false);
            SetupButton(btnDelete, "Delete", 660, false);
            SetupButton(btnClose, "Close", 780, false);

            btnAdd.Click += (s, e) => AddNew();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnClose.Click += (s, e) => Close();

            // Only an Admin sees the Delete button
            btnDelete.Visible = clsGlobal.CurrentUser?.IsAdmin ?? false;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSearch, txtSearch, dgvPeople, lblCount, btnAdd, btnEdit, btnDelete, btnClose
            });
        }

        private void SetupButton(Button button, string text, int x, bool primary)
        {
            button.Text = text;
            button.SetBounds(x, 505, 110, 38);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clsUiHelper.StyleButton(button, primary);
        }

        private void LoadPeople()
        {
            try
            {
                dgvPeople.DataSource = clsPerson.GetAll(txtSearch.Text);

                clsUiHelper.SetColumn(dgvPeople, "PersonID", "ID", 30);
                clsUiHelper.SetColumn(dgvPeople, "FirstName", "First Name", 70);
                clsUiHelper.SetColumn(dgvPeople, "LastName", "Last Name", 70);
                clsUiHelper.SetColumn(dgvPeople, "Phone", "Phone", 70);
                clsUiHelper.SetColumn(dgvPeople, "Email", "Email", 110);
                clsUiHelper.SetColumn(dgvPeople, "Address", "Address", 100);
                clsUiHelper.SetColumn(dgvPeople, "DateOfBirth", "Date of Birth", 70, "yyyy-MM-dd");

                lblCount.Text = dgvPeople.Rows.Count + " people";
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void AddNew()
        {
            using (frmAddEditPerson form = new frmAddEditPerson(null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadPeople();
            }
        }

        private void EditSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvPeople, "PersonID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a person first.");
                return;
            }

            try
            {
                clsPerson? person = clsPerson.Find(id.Value);
                if (person == null)
                {
                    clsUiHelper.ShowError("This person no longer exists.");
                    LoadPeople();
                    return;
                }

                using (frmAddEditPerson form = new frmAddEditPerson(person))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                        LoadPeople();
                }
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        private void DeleteSelected()
        {
            int? id = clsUiHelper.GetSelectedId(dgvPeople, "PersonID");
            if (id == null)
            {
                clsUiHelper.ShowError("Please select a person first.");
                return;
            }

            if (!clsUiHelper.Confirm("Do you want to delete this person?"))
                return;

            try
            {
                if (clsPerson.Delete(id.Value, clsGlobal.CurrentUser, out string error))
                {
                    clsUiHelper.ShowInfo("The person was deleted.");
                    LoadPeople();
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
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Used for both "Add customer" (customer == null) and "Edit customer".
    public partial class frmAddEditCustomer : Form
    {
        private readonly clsCustomer _customer;

        private readonly Label lblTitle = new Label();
        private readonly Label lblPerson = new Label();
        private readonly ComboBox cmbPerson = new ComboBox();      // Add mode: choose a person
        private readonly TextBox txtPerson = new TextBox();        // Edit mode: shows the person (read-only)
        private readonly Button btnPerson = new Button();          // "New Person..." / "Edit Person..."
        private readonly CheckBox chkActive = new CheckBox();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        public frmAddEditCustomer(clsCustomer? customer = null)
        {
            _customer = customer ?? new clsCustomer();
            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            string title = _customer.IsNew ? "Add Customer" : "Edit Customer";

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

            lblPerson.Text = _customer.IsNew ? "Select a person *" : "Person";
            lblPerson.AutoSize = false;
            lblPerson.SetBounds(20, 64, 420, 22);

            cmbPerson.SetBounds(20, 88, 420, 27);
            cmbPerson.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPerson.Visible = _customer.IsNew;

            txtPerson.SetBounds(20, 88, 420, 27);
            txtPerson.ReadOnly = true;
            txtPerson.Visible = !_customer.IsNew;

            btnPerson.Text = _customer.IsNew ? "New Person..." : "Edit Person...";
            btnPerson.SetBounds(20, 128, 200, 34);
            clsUiHelper.StyleButton(btnPerson);
            btnPerson.Click += btnPerson_Click;

            chkActive.Text = "Active customer";
            chkActive.SetBounds(20, 184, 300, 26);

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 220, 420, 34);

            btnSave.Text = "Save";
            btnSave.SetBounds(20, 270, 200, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(240, 270, 200, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblPerson, cmbPerson, txtPerson, btnPerson, chkActive, lblError, btnSave, btnCancel
            });
        }

        private void LoadValues()
        {
            chkActive.Checked = _customer.IsActive;

            try
            {
                if (_customer.IsNew)
                    LoadPeopleCombo(null);
                else
                    ShowPersonText();
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }

        // Fills the list with people who are not customers yet
        private void LoadPeopleCombo(int? selectPersonID)
        {
            DataTable people = clsCustomer.GetPeopleWithoutCustomer();

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
            clsPerson? person = _customer.GetPerson();
            txtPerson.Text = person == null ? string.Empty : person.FullName + "  (" + person.Phone + ")";
        }

        private void btnPerson_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_customer.IsNew)
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
                    clsPerson? person = _customer.GetPerson();
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

            if (_customer.IsNew)
            {
                if (cmbPerson.SelectedValue == null)
                {
                    lblError.Text = "Please select a person or create a new one.";
                    return;
                }

                _customer.PersonID = Convert.ToInt32(cmbPerson.SelectedValue);
            }

            _customer.IsActive = chkActive.Checked;

            try
            {
                if (!_customer.Save(out string error))
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
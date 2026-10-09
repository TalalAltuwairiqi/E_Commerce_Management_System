using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // Used for both "Add person" (person == null) and "Edit person".
    public partial class frmAddEditPerson : Form
    {
        private readonly clsPerson _person;

        private readonly Label lblTitle = new Label();
        private readonly TextBox txtFirst = new TextBox();
        private readonly TextBox txtLast = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtAddress = new TextBox();
        private readonly CheckBox chkDob = new CheckBox();
        private readonly DateTimePicker dtpDob = new DateTimePicker();
        private readonly Label lblError = new Label();
        private readonly Button btnSave = new Button();
        private readonly Button btnCancel = new Button();

        // The ID of the person after a successful save (the caller can use it)
        public int SavedPersonID { get; private set; }

        public frmAddEditPerson(clsPerson? person = null)
        {
            _person = person ?? new clsPerson();
            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            string title = _person.IsNew ? "Add Person" : "Edit Person";

            Text = title;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(460, 458);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = clsUiHelper.PrimaryColor;
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 14, 420, 36);

            AddLabel("First name *", 20, 64);
            txtFirst.SetBounds(20, 88, 200, 27);
            txtFirst.MaxLength = 50;

            AddLabel("Last name *", 240, 64);
            txtLast.SetBounds(240, 88, 200, 27);
            txtLast.MaxLength = 50;

            AddLabel("Phone *", 20, 130);
            txtPhone.SetBounds(20, 154, 200, 27);
            txtPhone.MaxLength = 20;

            AddLabel("Email", 240, 130);
            txtEmail.SetBounds(240, 154, 200, 27);
            txtEmail.MaxLength = 100;

            AddLabel("Address", 20, 196);
            txtAddress.SetBounds(20, 220, 420, 60);
            txtAddress.Multiline = true;
            txtAddress.MaxLength = 250;

            chkDob.Text = "Date of birth";
            chkDob.SetBounds(20, 296, 200, 24);
            chkDob.CheckedChanged += (s, e) => dtpDob.Enabled = chkDob.Checked;

            dtpDob.SetBounds(20, 322, 200, 27);
            dtpDob.Format = DateTimePickerFormat.Short;
            dtpDob.MinDate = new DateTime(1920, 1, 1);   // not earlier: the Hijri calendar does not support dates before 1900-04-30
            dtpDob.MaxDate = DateTime.Today;

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 362, 420, 34);

            btnSave.Text = "Save";
            btnSave.SetBounds(20, 402, 200, 40);
            clsUiHelper.StyleButton(btnSave, true);
            btnSave.Click += btnSave_Click;

            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(240, 402, 200, 40);
            clsUiHelper.StyleButton(btnCancel);
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, txtFirst, txtLast, txtPhone, txtEmail, txtAddress,
                chkDob, dtpDob, lblError, btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label { Text = text, AutoSize = false };
            label.SetBounds(x, y, 200, 22);
            Controls.Add(label);
        }

        private void LoadValues()
        {
            txtFirst.Text = _person.FirstName;
            txtLast.Text = _person.LastName;
            txtPhone.Text = _person.Phone;
            txtEmail.Text = _person.Email ?? string.Empty;
            txtAddress.Text = _person.Address ?? string.Empty;

            DateTime dob = _person.DateOfBirth ?? new DateTime(2000, 1, 1);
            if (dob < dtpDob.MinDate) dob = dtpDob.MinDate;
            if (dob > dtpDob.MaxDate) dob = dtpDob.MaxDate;

            dtpDob.Value = dob;
            chkDob.Checked = _person.DateOfBirth.HasValue;
            dtpDob.Enabled = chkDob.Checked;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            _person.FirstName = txtFirst.Text;
            _person.LastName = txtLast.Text;
            _person.Phone = txtPhone.Text;
            _person.Email = txtEmail.Text;
            _person.Address = txtAddress.Text;
            _person.DateOfBirth = chkDob.Checked ? dtpDob.Value.Date : (DateTime?)null;

            try
            {
                if (!_person.Save(out string error))
                {
                    lblError.Text = error;
                    return;
                }

                SavedPersonID = _person.PersonID;
                DialogResult = DialogResult.OK;   // closes the form
            }
            catch (Exception ex)
            {
                clsUiHelper.ShowException(ex);
            }
        }
    }
}
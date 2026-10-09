using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    public partial class frmLogin : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Label lblUsername = new Label();
        private readonly Label lblPassword = new Label();
        private readonly Label lblError = new Label();
        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly Button btnLogin = new Button();
        private readonly Button btnExit = new Button();

        public frmLogin()
        {
           
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "ECMS - Login";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(380, 310);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            lblTitle.Text = "ECMS Login";
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(31, 58, 95);
            lblTitle.AutoSize = false;
            lblTitle.SetBounds(20, 20, 340, 40);

            lblUsername.Text = "Username";
            lblUsername.AutoSize = false;
            lblUsername.SetBounds(20, 80, 340, 22);
            txtUsername.SetBounds(20, 104, 340, 27);

            lblPassword.Text = "Password";
            lblPassword.AutoSize = false;
            lblPassword.SetBounds(20, 142, 340, 22);
            txtPassword.SetBounds(20, 166, 340, 27);
            txtPassword.UseSystemPasswordChar = true;

            lblError.ForeColor = Color.Firebrick;
            lblError.AutoSize = false;
            lblError.SetBounds(20, 204, 340, 24);

            btnLogin.Text = "Login";
            btnLogin.SetBounds(20, 240, 165, 40);
            btnLogin.Click += btnLogin_Click;

            btnExit.Text = "Exit";
            btnExit.SetBounds(195, 240, 165, 40);
            btnExit.DialogResult = DialogResult.Cancel;   // closes the window

            AcceptButton = btnLogin;   // Enter key = Login
            CancelButton = btnExit;    // Esc key = Exit

            Controls.AddRange(new Control[]
            {
                lblTitle, lblUsername, txtUsername, lblPassword, txtPassword, lblError, btnLogin, btnExit
            });

            ActiveControl = txtUsername;
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            try
            {
                enLoginResult result = clsUser.Login(txtUsername.Text, txtPassword.Text, out clsUser? user);

                switch (result)
                {
                    case enLoginResult.Success:
                        clsGlobal.CurrentUser = user;
                        DialogResult = DialogResult.OK;   // closes the login window
                        break;

                    case enLoginResult.Inactive:
                        lblError.Text = "This account is disabled. Contact the administrator.";
                        break;

                    default:
                        lblError.Text = "Invalid username or password.";
                        txtPassword.Clear();
                        txtPassword.Focus();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not log in. Please check the database connection.\n\n" + ex.Message,
                    "ECMS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
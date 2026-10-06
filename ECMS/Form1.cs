using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    // TEMPORARY test form for Stage 2. It will be replaced by the Login form in Stage 3.
    // The controls are created in code so nothing depends on the designer.
    public partial class Form1 : Form
    {
        private readonly Button btnTest = new Button();
        private readonly Label lblResult = new Label();
        private readonly DataGridView dgvCounts = new DataGridView();

        public Form1()
        {
            InitializeComponent();

            Text = "ECMS - Connection Test (temporary)";
            ClientSize = new Size(540, 380);
            StartPosition = FormStartPosition.CenterScreen;

            btnTest.Text = "Test Connection";
            btnTest.SetBounds(20, 20, 160, 36);
            btnTest.Click += btnTest_Click;

            lblResult.AutoSize = false;
            lblResult.SetBounds(20, 66, 500, 44);

            dgvCounts.SetBounds(20, 120, 500, 240);
            dgvCounts.ReadOnly = true;
            dgvCounts.AllowUserToAddRows = false;
            dgvCounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Controls.AddRange(new Control[] { btnTest, lblResult, dgvCounts });
        }

        private void btnTest_Click(object? sender, EventArgs e)
        {
            dgvCounts.DataSource = null;

            bool connected = clsDatabase.TestConnection(out string message);

            if (!connected)
            {
                lblResult.ForeColor = Color.Firebrick;
                lblResult.Text = "FAILED: " + message;
                return;
            }

            lblResult.ForeColor = Color.DarkGreen;
            lblResult.Text = "SUCCESS: " + message;

            try
            {
                dgvCounts.DataSource = clsDatabase.GetTableRowCounts();
            }
            catch (Exception ex)
            {
                lblResult.ForeColor = Color.Firebrick;
                lblResult.Text = "Connected, but reading the tables failed: " + ex.Message;
            }
        }
    }
}
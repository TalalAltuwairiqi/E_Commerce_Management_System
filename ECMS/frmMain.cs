using System;
using System.Drawing;
using System.Windows.Forms;
using ECMS_Business;

namespace ECMS
{
    public partial class frmMain : Form
    {
        private readonly MenuStrip menu = new MenuStrip();
        private readonly StatusStrip status = new StatusStrip();
        private readonly ToolStripStatusLabel lblUser = new ToolStripStatusLabel();
        private readonly Label lblWelcome = new Label();
        private readonly FlowLayoutPanel dashboard = new FlowLayoutPanel();

        private Label? lblCustomers;
        private Label? lblProducts;
        private Label? lblOrders;
        private Label? lblPending;
        private Label? lblLowStock;
        private Label? lblRevenue;       // Admin only
        private Label? lblOutstanding;   // Admin only

        // Program.cs reads this to know whether to show the login window again.
        public bool LogoutRequested { get; private set; }

        public frmMain()
        {
            BuildUI();

            // The numbers are refreshed every time the main window comes back to the front
            Activated += (s, e) => LoadDashboard();
        }

        private void BuildUI()
        {
            var user = clsGlobal.CurrentUser;
            bool isAdmin = user?.IsAdmin ?? false;

            Text = "ECMS - E-Commerce Management System";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1000, 620);
            StartPosition = FormStartPosition.CenterScreen;

            // ----- Menus -----
            var customers = new ToolStripMenuItem("Customers");
            AddItem(customers, "Customers", (s, e) => ShowForm(new frmCustomersList()));
            AddItem(customers, "People", (s, e) => ShowForm(new frmPeopleList()));

            var catalog = new ToolStripMenuItem("Catalog");
            AddItem(catalog, "Products", (s, e) => ShowForm(new frmProductsList()));
            AddItem(catalog, "Categories", (s, e) => ShowForm(new frmCategoriesList()));

            var orders = new ToolStripMenuItem("Orders");
            AddItem(orders, "New Order", (s, e) => ShowForm(new frmNewOrder()));
            AddItem(orders, "All Orders", (s, e) => ShowForm(new frmOrdersList()));
            AddItem(orders, "Payments", (s, e) => ShowForm(new frmPaymentsList()));

            var inventory = new ToolStripMenuItem("Inventory");
            AddItem(inventory, "Stock Adjustment", (s, e) => ShowForm(new frmStockAdjustment()));
            AddItem(inventory, "Movement History", (s, e) => ShowForm(new frmMovementHistory()));
            AddItem(inventory, "Low Stock", (s, e) => ShowForm(new frmLowStock()));

            var reports = new ToolStripMenuItem("Reports");
            AddItem(reports, "Best Sellers", (s, e) => ShowForm(new frmReport(enReportType.BestSellers)));
            AddItem(reports, "Monthly Sales", (s, e) => ShowForm(new frmReport(enReportType.MonthlySales)));
            AddItem(reports, "Sales by Category", (s, e) => ShowForm(new frmReport(enReportType.SalesByCategory)));
            reports.Visible = isAdmin;            // Admin only

            var admin = new ToolStripMenuItem("Administration");
            AddItem(admin, "Users", (s, e) => ShowForm(new frmUsersList()));
            admin.Visible = isAdmin;              // Admin only

            var account = new ToolStripMenuItem("Account");
            AddItem(account, "Change Password", (s, e) => ChangeOwnPassword());
            AddItem(account, "Logout", (s, e) => Logout());
            AddItem(account, "Exit", (s, e) => Close());

            menu.Items.AddRange(new ToolStripItem[]
            {
                customers, catalog, orders, inventory, reports, admin, account
            });
            MainMenuStrip = menu;

            // ----- Status bar -----
            lblUser.Text = user == null
                ? "Not logged in"
                : "Logged in as: " + user.FullName + " (" + user.Role + ")";
            status.Items.Add(lblUser);

            // ----- Welcome text -----
            lblWelcome.Dock = DockStyle.Top;
            lblWelcome.Height = 110;
            lblWelcome.Padding = new Padding(30, 20, 0, 0);
            lblWelcome.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblWelcome.ForeColor = Color.FromArgb(31, 58, 95);
            lblWelcome.Text = user == null
                ? "Welcome"
                : "Welcome, " + user.FullName + "\n" + user.Role;

            // ----- Dashboard cards -----
            dashboard.Dock = DockStyle.Fill;
            dashboard.Padding = new Padding(24, 0, 0, 0);
            dashboard.BackColor = Color.FromArgb(244, 247, 251);

            lblCustomers = AddCard("Active customers", Color.FromArgb(46, 117, 182));
            lblProducts = AddCard("Active products", Color.FromArgb(46, 117, 182));
            lblOrders = AddCard("Total orders", Color.FromArgb(46, 117, 182));
            lblPending = AddCard("Pending orders", Color.DarkOrange);
            lblLowStock = AddCard("Low-stock products", Color.Firebrick);

            if (isAdmin)
            {
                lblRevenue = AddCard("Revenue (cancelled excluded)", Color.DarkGreen);
                lblOutstanding = AddCard("Unpaid balance", Color.Firebrick);
            }

            // Fill-docked control first, then the top and bottom ones (the order the designer uses).
            Controls.Add(dashboard);
            Controls.Add(lblWelcome);
            Controls.Add(status);
            Controls.Add(menu);
        }

        // Creates one card and returns the label that shows its number
        private Label AddCard(string caption, Color accent)
        {
            Panel card = new Panel
            {
                Size = new Size(230, 100),
                Margin = new Padding(0, 20, 20, 0),
                BackColor = Color.White
            };

            Panel stripe = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accent };

            Label value = new Label
            {
                Text = "-",
                AutoSize = false,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 58, 95)
            };
            value.SetBounds(20, 14, 200, 44);

            Label title = new Label
            {
                Text = caption,
                AutoSize = false,
                ForeColor = Color.Gray
            };
            title.SetBounds(20, 62, 205, 24);

            card.Controls.Add(stripe);
            card.Controls.Add(value);
            card.Controls.Add(title);
            dashboard.Controls.Add(card);

            return value;
        }

        // Reads the numbers from the database and shows them in the cards
        private void LoadDashboard()
        {
            try
            {
                clsDashboardStats stats = clsReport.GetDashboard(clsGlobal.CurrentUser);

                if (lblCustomers != null) lblCustomers.Text = stats.ActiveCustomers.ToString("N0");
                if (lblProducts != null) lblProducts.Text = stats.ActiveProducts.ToString("N0");
                if (lblOrders != null) lblOrders.Text = stats.TotalOrders.ToString("N0");
                if (lblPending != null) lblPending.Text = stats.PendingOrders.ToString("N0");
                if (lblLowStock != null) lblLowStock.Text = stats.LowStockProducts.ToString("N0");

                if (lblRevenue != null && stats.Revenue.HasValue)
                    lblRevenue.Text = stats.Revenue.Value.ToString("N2");

                if (lblOutstanding != null && stats.OutstandingBalance.HasValue)
                    lblOutstanding.Text = stats.OutstandingBalance.Value.ToString("N2");
            }
            catch
            {
                // The dashboard is only a summary: if the database is not reachable the cards keep their last values.
            }
        }

        private void AddItem(ToolStripMenuItem parent, string text, EventHandler? onClick = null)
        {
            var item = new ToolStripMenuItem(text);

            if (onClick == null)
                onClick = (s, e) => ComingSoon(text);

            item.Click += onClick;
            parent.DropDownItems.Add(item);
        }

        // Opens a form as a window on top of the main window and disposes it afterwards
        private void ShowForm(Form form)
        {
            using (form)
            {
                form.ShowDialog(this);
            }
        }

        private void ChangeOwnPassword()
        {
            var user = clsGlobal.CurrentUser;
            if (user == null)
                return;

            ShowForm(new frmChangePassword(user.UserID, user.Username, true));
        }

        private void ComingSoon(string name)
        {
            MessageBox.Show("'" + name + "' will be built in an upcoming stage.",
                "ECMS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Logout()
        {
            DialogResult answer = MessageBox.Show("Do you want to log out?", "ECMS",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            clsGlobal.CurrentUser = null;
            LogoutRequested = true;
            Close();
        }
    }
}
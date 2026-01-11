using System;
using System.Windows.Forms;
using WotlkBotGui.Controls;
using WotlkBotGui.Views;
using WotlkBotGui.Services;

namespace WotlkBotGui
{
    public partial class MainWindow : Form
    {
        private SideNavBar navBar;
        private Panel contentPanel;
        
        // Views
        private DashboardView dashboardView;
        private SettingsView settingsView;

        public MainWindow()
        {
            InitializeComponent();
            try { this.Icon = new System.Drawing.Icon("IconWotlkBot.ico"); } catch { }
            SetupLayout();
            Theme.Apply(this);
        }

        private void SetupLayout()
        {
            // NavBar
            navBar = new SideNavBar();
            navBar.Dock = DockStyle.Left;
            navBar.OnNavigate += NavBar_OnNavigate;

            // Content Panel
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(0);

            this.Controls.Add(contentPanel); // Fill takes remaining space
            this.Controls.Add(navBar); // Left docking

            // Init Views
            dashboardView = new DashboardView();
            settingsView = new SettingsView();

            // Default
            SwitchView(dashboardView);
        }

        private void NavBar_OnNavigate(object sender, string tag)
        {
            switch (tag)
            {
                case "DASHBOARD":
                    SwitchView(dashboardView);
                    dashboardView.RefreshBots(); // Refresh data when showing
                    break;
                case "SETTINGS":
                    SwitchView(settingsView);
                    break;
                case "LOGS":
                    // TODO: Implement LogView
                    MessageBox.Show("Logs View not implemented yet.", "Info");
                    break;
            }
        }

        private void SwitchView(UserControl view)
        {
            contentPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(view);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            // Initializing logic if needed
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            BotService.Instance.StopAll();
            base.OnFormClosed(e);
        }
    }
}

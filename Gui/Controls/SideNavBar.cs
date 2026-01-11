using System;
using System.Drawing;
using System.Windows.Forms;

namespace WotlkBotGui.Controls
{
    public class SideNavBar : UserControl
    {
        public event EventHandler<string> OnNavigate;

        private FlowLayoutPanel flowPanel;

        public SideNavBar()
        {
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Width = 200;
            this.Dock = DockStyle.Left;
            this.BackColor = Theme.PanelColor;

            flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.Padding = new Padding(10, 20, 10, 0);

            AddNavButton("Dashboard", "DASHBOARD");
            AddNavButton("Settings", "SETTINGS");
            AddNavButton("Console / Logs", "LOGS");

            this.Controls.Add(flowPanel);
        }

        private void AddNavButton(string text, string tag)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Tag = tag;
            btn.Height = 45;
            btn.Width = 180;
            btn.Margin = new Padding(0, 0, 0, 10);
            btn.Click += (s, e) => OnNavigate?.Invoke(this, tag);
            
            Theme.Apply(btn); // Apply base theme first
            
            // Custom Nav Style
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(10, 0, 0, 0);
            
            flowPanel.Controls.Add(btn);
        }

        private void ApplyTheme()
        {
            this.BackColor = Theme.PanelColor;
        }
    }
}

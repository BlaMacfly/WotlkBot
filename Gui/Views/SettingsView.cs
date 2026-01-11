using System;
using System.Drawing;
using System.Windows.Forms;
using WotlkBotGui.Services;

namespace WotlkBotGui.Views
{
    public class SettingsView : UserControl
    {
        private TextBox txtRealmlist;
        private TextBox txtMaster;
        private Button btnSave;

        public SettingsView()
        {
            InitializeComponent();
            ApplyTheme();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.BackColor;
            this.Padding = new Padding(30);

            Label lblTitle = new Label() { 
                Text = "Global Settings", 
                Font = Theme.HeaderFont, 
                AutoSize = true, 
                Location = new Point(30, 30) 
            };

            Label lblRealm = new Label() { Text = "Realmlist / Host:", Location = new Point(30, 80), AutoSize = true };
            txtRealmlist = new TextBox() { Location = new Point(30, 105), Width = 300 };

            Label lblMaster = new Label() { Text = "Master Character Name:", Location = new Point(30, 150), AutoSize = true };
            txtMaster = new TextBox() { Location = new Point(30, 175), Width = 300 };

            btnSave = new Button() { Text = "Save Settings", Location = new Point(30, 230), Size = new Size(150, 40) };
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblRealm);
            this.Controls.Add(txtRealmlist);
            this.Controls.Add(lblMaster);
            this.Controls.Add(txtMaster);
            this.Controls.Add(btnSave);
        }

        private void ApplyTheme()
        {
            Theme.Apply(this);
        }

        private void LoadSettings()
        {
            txtRealmlist.Text = BotService.Instance.Database.GetHost();
            txtMaster.Text = BotService.Instance.Database.GetMaster();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            BotService.Instance.Database.UpdateConfig(txtRealmlist.Text, txtMaster.Text);
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

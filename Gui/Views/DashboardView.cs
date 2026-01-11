using System;
using System.Drawing;
using System.Windows.Forms;
using WotlkBotGui.Controls;
using WotlkBotGui.Services;
using System.Collections.Generic;

namespace WotlkBotGui.Views
{
    public class DashboardView : UserControl
    {
        private FlowLayoutPanel flowBots;
        private Label lblTitle;
        private Button btnAdd;

        public DashboardView()
        {
            InitializeComponent();
            ApplyTheme();
            RefreshBots();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.BackColor;

            lblTitle = new Label()
            {
                Text = "Dashboard",
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextColor,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            btnAdd = new Button()
            {
                Text = "+ Add Bot",
                Size = new Size(100, 30),
                Location = new Point(20, 60),
            };
            btnAdd.Click += BtnAdd_Click;

            flowBots = new FlowLayoutPanel()
            {
                Dock = DockStyle.Bottom,
                Height = this.Height - 110, // Approximate header space
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            // Resize flowpanel to fill rest
            this.SizeChanged += (s, e) => {
                flowBots.Height = this.Height - 100;
                flowBots.Top = 100;
                flowBots.Width = this.Width;
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(btnAdd);
            this.Controls.Add(flowBots);
        }

        private void ApplyTheme()
        {
            Theme.Apply(this);
        }

        public void RefreshBots()
        {
            flowBots.Controls.Clear();
            List<Bot> bots = BotService.Instance.Database.GetBots();
            
            foreach (var bot in bots)
            {
                var card = new BotCard(bot);
                card.Margin = new Padding(0, 0, 20, 20); // Spacing
                card.OnEditRequested += Card_OnEditRequested;
                flowBots.Controls.Add(card);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            Bot newBot = BotService.Instance.Database.AddBot();
            RefreshBots();
        }

        private void Card_OnEditRequested(object sender, Bot bot)
        {
            // Simple Edit Dialog (or we could navigate to a detail view)
            // For now, let's keep it simple with a popup logic or similar, 
            // but ideally we'd swap the view. 
            // Let's implement a basic Dialog for now reusing the logic from old GUI?
            // Or better, trigger an event for MainWindow to handle or show a Modal.
            
            Form editForm = new Form();
            editForm.Size = new Size(400, 350);
            editForm.Text = "Edit Bot: " + bot.CharName;
            editForm.StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(editForm);

            // Controls
            Label lblAcc = new Label { Text = "Account:", Location = new Point(20, 20), AutoSize = true };
            TextBox txtAcc = new TextBox { Text = bot.AccountName, Location = new Point(120, 20), Width = 200 };
            
            Label lblPass = new Label { Text = "Password:", Location = new Point(20, 50), AutoSize = true };
            TextBox txtPass = new TextBox { Text = bot.Password, Location = new Point(120, 50), Width = 200, UseSystemPasswordChar = true };

            Label lblChar = new Label { Text = "Character:", Location = new Point(20, 80), AutoSize = true };
            TextBox txtChar = new TextBox { Text = bot.CharName, Location = new Point(120, 80), Width = 200 };

            Label lblClass = new Label { Text = "Class:", Location = new Point(20, 110), AutoSize = true };
            ComboBox cmbClass = new ComboBox { Location = new Point(120, 110), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbClass.DataSource = Enum.GetValues(typeof(Cls));
            cmbClass.SelectedItem = (Cls)bot.Class;

            Button btnSave = new Button { Text = "Save", Location = new Point(120, 200), DialogResult = DialogResult.OK };
            Button btnDelete = new Button { Text = "Delete", Location = new Point(220, 200), BackColor = Theme.WarningColor };
            
            btnDelete.Click += (s, ev) => {
                if(MessageBox.Show("Delete this bot?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    BotService.Instance.Database.DeleteBot(bot);
                    editForm.DialogResult = DialogResult.Abort; // Abort = Delete in this context
                    editForm.Close();
                }
            };

            editForm.Controls.AddRange(new Control[] { lblAcc, txtAcc, lblPass, txtPass, lblChar, txtChar, lblClass, cmbClass, btnSave, btnDelete });
            Theme.Apply(editForm); // Re-apply to pick up new controls

            var result = editForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                bot.AccountName = txtAcc.Text;
                bot.Password = txtPass.Text;
                bot.CharName = txtChar.Text;
                bot.Class = (int)cmbClass.SelectedItem;
                
                BotService.Instance.Database.Update(bot);
                RefreshBots();
            }
            else if (result == DialogResult.Abort)
            {
                RefreshBots();
            }
        }
    }
}

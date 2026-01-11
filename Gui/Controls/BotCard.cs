using System;
using System.Drawing;
using System.Windows.Forms;
using WotlkBotGui.Services;

namespace WotlkBotGui.Controls
{
    public class BotCard : UserControl
    {
        public Bot BotModel { get; private set; }
        
        private Label lblName;
        private Label lblClass;
        private Label lblStatus;
        private ProgressBar progressHp;
        private ProgressBar progressMana;
        private Button btnAction;
        private Button btnEdit;
        private System.Windows.Forms.Timer updateTimer;

        public BotCard(Bot bot)
        {
            this.BotModel = bot;
            InitializeComponent();
            ApplyTheme();
            
            this.DoubleBuffered = true;
            this.Padding = new Padding(5);
            
            // Initial Data
            lblName.Text = bot.CharName;
            lblClass.Text = Enum.GetName(typeof(Cls), bot.Class);
            
            // Timer for live updates
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(220, 160);
            this.BackColor = Theme.PanelColor;

            lblName = new Label() { 
                Location = new Point(10, 10), 
                AutoSize = true, 
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextColor 
            };

            lblClass = new Label() { 
                Location = new Point(10, 35), 
                AutoSize = true, 
                Font = Theme.SmallFont,
                ForeColor = Theme.TextDimColor 
            };

            lblStatus = new Label() { 
                Location = new Point(10, 55), 
                AutoSize = true, 
                Text = "Offline",
                ForeColor = Theme.TextDimColor
            };

            progressHp = new ProgressBar() { 
                Location = new Point(10, 80), 
                Size = new Size(200, 10),
                Value = 100
            };
            
            progressMana = new ProgressBar() { 
                Location = new Point(10, 95), 
                Size = new Size(200, 10),
                Value = 100
            };

            btnAction = new Button() {
                Location = new Point(10, 120),
                Size = new Size(95, 30),
                Text = "Start"
            };
            btnAction.Click += BtnAction_Click;

            btnEdit = new Button() {
                Location = new Point(115, 120),
                Size = new Size(95, 30),
                Text = "Edit"
            };
            btnEdit.Click += BtnEdit_Click;

            this.Controls.Add(lblName);
            this.Controls.Add(lblClass);
            this.Controls.Add(lblStatus);
            this.Controls.Add(progressHp);
            this.Controls.Add(progressMana);
            this.Controls.Add(btnAction);
            this.Controls.Add(btnEdit);
        }

        private void ApplyTheme()
        {
            Theme.Apply(this);
            // Custom overrides if needed
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            var bots = BotService.Instance.GetActiveBots();
            var activeBot = bots.Find(b => b.BotModel.ID == BotModel.ID);

            if (activeBot != null)
            {
                lblStatus.Text = activeBot.CurrentStatus;
                
                // Colorize status
                if (activeBot.CurrentStatus.Contains("Combat")) 
                    lblStatus.ForeColor = Theme.WarningColor;
                else 
                    lblStatus.ForeColor = Theme.SuccessColor;

                progressHp.Value = activeBot.HealthPercent;
                progressMana.Value = activeBot.ManaPercent;

                btnAction.Text = "Stop";
                btnAction.BackColor = Theme.WarningColor;
            }
            else
            {
                lblStatus.Text = "Offline";
                lblStatus.ForeColor = Theme.TextDimColor;
                progressHp.Value = 0;
                progressMana.Value = 0;
                btnAction.Text = "Start";
                btnAction.BackColor = Theme.SuccessColor;
            }
        }

        private void BtnAction_Click(object sender, EventArgs e)
        {
            if (BotService.Instance.IsRunning(BotModel))
            {
                BotService.Instance.StopBot(BotModel);
            }
            else
            {
                // We need Host/Master. For now, pull from DB via Service or just assume defaults/globals are stored elsewhere?
                // The BotService has access to DB.
                string host = BotService.Instance.Database.GetHost();
                string master = BotService.Instance.Database.GetMaster();
                
                if (string.IsNullOrEmpty(master))
                {
                    MessageBox.Show("Master Character not set in Settings!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                BotService.Instance.StartBot(BotModel, host, master);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            // Trigger Edit Event (to be handled by parent Dashboard)
            OnEditRequested?.Invoke(this, BotModel);
        }

        public event EventHandler<Bot> OnEditRequested;
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq; // Added for LINQ
using WotlkClient.Clients;

namespace WotlkBotGui.Services
{
    public class BotService
    {
        private static BotService _instance;
        public static BotService Instance => _instance ?? (_instance = new BotService());

        private List<BotMgr> _activeBots = new List<BotMgr>();
        private Database _db;

        // Events
        public event Action<BotMgr> OnBotStarted;
        public event Action<BotMgr> OnBotStopped;

        private BotService()
        {
            _db = new Database();
            _db.Init();
        }

        public Database Database => _db;

        public void StartBot(Bot botModel, string host, string master)
        {
            // Avoid duplicates
            if (_activeBots.Any(b => b.BotModel.ID == botModel.ID))
                return;

            BotMgr mgr = new BotMgr(botModel);
            
            // Run in thread
            Thread t = new Thread(() => mgr.Main(host, 3724, master));
            t.IsBackground = true;
            t.Start();

            _activeBots.Add(mgr);
            OnBotStarted?.Invoke(mgr);
        }

        public void StopBot(Bot botModel)
        {
            var mgr = _activeBots.FirstOrDefault(b => b.BotModel.ID == botModel.ID);
            if (mgr != null)
            {
                mgr.Logout();
                _activeBots.Remove(mgr);
                OnBotStopped?.Invoke(mgr);
            }
        }

        public void StopAll()
        {
            foreach (var mgr in _activeBots.ToList())
            {
                mgr.Logout();
                OnBotStopped?.Invoke(mgr);
            }
            _activeBots.Clear();
        }

        public List<BotMgr> GetActiveBots()
        {
            return _activeBots;
        }

        public bool IsRunning(Bot bot)
        {
            return _activeBots.Any(b => b.BotModel.ID == bot.ID);
        }
    }
}

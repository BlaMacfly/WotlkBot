using System;
using System.Threading;
using WotlkClient.Clients;
using WotlkClient.Constants;
using WotlkClient.Terrain;

namespace WotlkBotGui
{
    public class BotMgr
    {
        private LogonServerClient loginClient;
        private WorldServerClient worldClient;
        
        public Bot BotModel { get; private set; }
        public string Master { get; private set; }
        
        // Status properties
        public string CurrentStatus { get; private set; } = "Idle";
        public int HealthPercent { get; private set; } = 0;
        public int ManaPercent { get; private set; } = 0;
        public string ZoneName { get; private set; } = "-";

        private bool shouldStop = false;

        public BotMgr(Bot bot)
        {
            this.BotModel = bot;
        }

        public void Main(string host, int port, string _master)
        {
            Master = _master;
            shouldStop = false;
            CurrentStatus = "Connecting...";

            LoginCompletedCallBack callback = new LoginCompletedCallBack(LoginComplete);
            try
            {
                loginClient = new LogonServerClient(host, port, BotModel.AccountName, BotModel.Password, callback);
                loginClient.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured: {0}", ex.Message);
                CurrentStatus = "Error";
            }

            while (!shouldStop)
            {
                // Update internal status loop
                if (worldClient != null && worldClient.player != null) 
                {
                    if (worldClient.player.MaxHealth > 0)
                        HealthPercent = (int)((float)worldClient.player.Health / (float)worldClient.player.MaxHealth * 100);
                    
                    if (worldClient.player.MaxMana > 0)
                        ManaPercent = (int)((float)worldClient.player.Mana / (float)worldClient.player.MaxMana * 100);

                    // Update Status Text
                    if (worldClient.combatMgr != null && worldClient.combatMgr.currentTarget != null)
                        CurrentStatus = "Combat (" + worldClient.combatMgr.currentTarget.Health + "hp)";
                    else if (worldClient.player.Health == 0)
                        CurrentStatus = "Dead";
                    else
                        CurrentStatus = "Online";
                }

                Thread.Sleep(500);       
            }
            Console.WriteLine("BotMgr ended");
            loginClient?.HardDisconnect();
            worldClient?.HardDisconnect();
            CurrentStatus = "Disconnected";
            HealthPercent = 0;
            ManaPercent = 0;
        }

        public void LoginComplete(uint result)
        {
            if (result == 0)
            {
                CurrentStatus = "Authenticating...";
                RealmListCompletedCallBack callback = new RealmListCompletedCallBack(RealmListComplete);
                loginClient.RequestRealmlist(callback);
            }
            else
            {
                System.Console.WriteLine("Log in failed");
                CurrentStatus = "Login Failed";
            }
        }

        public void RealmListComplete(uint result)
        {
            if (result == 0)
            {
                Realm? realm = null;
                foreach (Realm r in loginClient.Realmlist)
                {
                    realm = r;
                }
                
                if (realm != null)
                {
                    AuthCompletedCallBack callback = new AuthCompletedCallBack(AuthCompleted);
                    worldClient = new WorldServerClient(BotModel.AccountName, realm.Value, loginClient.mKey, BotModel.CharName, callback);
                    worldClient.Connect();
                }
            }
            else
                System.Console.WriteLine("Realmlist failed");
        }

        public void AuthCompleted(uint result)
        {
            if (result == 0)
            {
                CharEnumCompletedCallBack callback = new CharEnumCompletedCallBack(CharEnumComplete);
                worldClient.CharEnumRequest(callback);
            }
            else
                System.Console.WriteLine("Auth failed");
        }

        public void CharEnumComplete(uint result)
        {
            if (result == 0)
            {
                Character? toLogin = null;
                foreach (Character ch in worldClient.Charlist)
                {
                    if (ch.Name == BotModel.CharName)
                        toLogin = ch;
                }
                
                if (toLogin.HasValue)
                {
                    CharLoginCompletedCallBack callback = new CharLoginCompletedCallBack(CharLoginComplete);
                    worldClient.LoginPlayer(toLogin.Value, callback);
                }
                else
                {
                    CurrentStatus = "Char Not Found";
                }
            }
        }

        public void CharLoginComplete(uint result)
        {
            if (result == 0)
            {
                Console.WriteLine("Logged into world with " + BotModel.CharName);
                CurrentStatus = "Online";
                InviteCallBack callback = new InviteCallBack(InviteRequest);
                worldClient.SetInviteCallback(callback);
            }
            else
                Console.WriteLine("Char login failed");
        }

        public void InviteRequest(string inviter)
        {
            if(inviter == Master)
            {
                worldClient.AcceptInviteRequest();
                // WotlkClient.Clients.Object inv = ObjectMgr.GetInstance().getObject(inviter);
            }
        }

        public void Logout()
        {
            worldClient?.Logout();
            shouldStop = true;
        }

        public void SetVoice(bool enabled)
        {
            if (worldClient != null && worldClient.voiceMgr != null)
                worldClient.voiceMgr.VoiceEnabled = enabled;
        }

        public void SetSocial(bool enabled)
        {
            if (worldClient != null && worldClient.socialMgr != null)
                worldClient.socialMgr.SocialEnabled = enabled;
        }

        public void SetStrategy(bool enabled)
        {
            if (worldClient != null && worldClient.strategyMgr != null)
                worldClient.strategyMgr.StrategyEnabled = enabled;
        }
    }
}

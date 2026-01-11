using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WotlkClient.Constants;
using WotlkClient.Shared;
using WotlkClient.Network;

namespace WotlkClient.Clients
{
    public class StrategyMgr
    {
        private WorldServerClient client;
        private string prefix;
        private bool isRunning = false;
        private Thread loop;

        public bool StrategyEnabled { get; set; } = true;
        
        // Raid Icons
        public enum RaidIcon : byte
        {
            None = 0,
            Star = 1,
            Circle = 2,
            Diamond = 3,
            Triangle = 4,
            Moon = 5,
            Square = 6,
            Cross = 7,  // Kill second
            Skull = 8   // Kill first
        }

        public StrategyMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            loop = new Thread(StrategyLoop);
            loop.IsBackground = true;
            loop.Start();
            Log.WriteLine(LogType.Success, "StrategyMgr started (Tactician).", prefix);
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void StrategyLoop()
        {
            while (isRunning)
            {
                if (StrategyEnabled && client.Connected && client.player != null)
                {
                    try
                    {
                        if (client.combatMgr.AutoCombatEnabled)
                        {
                             ManageRaidTargets();
                        }
                        
                        ManageManaBreaks();
                    }
                    catch (Exception ex)
                    {
                        // Log.WriteLine(LogType.Error, "Strategy Loop Error: " + ex.Message, prefix);
                    }
                }
                Thread.Sleep(1000); // Check every second
            }
        }

        private void ManageRaidTargets()
        {
            // Simple logic: If we are in combat, find the target with highest max health (Elite?) or Mana (Healer?)
            // And mark it with Skull if not marked.
            
            var units = ObjectMgr.GetInstance().GetAllObjects()
                        .Where(o => o.Type == ObjectType.Unit && o.Health > 0)
                        .ToList();

            Object bestTarget = null;
            uint maxHp = 0;

            foreach(var u in units)
            {
                // Check if targeting us or master
                // (Simplified: just take the closest hostile with high HP)
                // TODO: Check reaction
                
                // For now, let's just mark the current target of the bot if it exists
                if (client.combatMgr.currentTarget != null)
                {
                    SetRaidIcon(client.combatMgr.currentTarget.Guid, RaidIcon.Skull);
                    return; 
                }
            }
        }

        private DateTime lastManaWarn = DateTime.MinValue;
        private void ManageManaBreaks()
        {
            // Check self mana
            if (client.player.PowerType == PowerType.Mana)
            {
                int manaPct = (int)((float)client.player.Mana / client.player.MaxMana * 100);
                if (manaPct < 30 && !client.combatMgr.AutoCombatEnabled) // Only if not fighting
                {
                    if ((DateTime.Now - lastManaWarn).TotalSeconds > 30)
                    {
                        client.SendChatMsg(ChatMsg.Say, Languages.Universal, "Pause mana SVP ! Je suis à sec.");
                        client.SendEmote(EmoteType.SIT);
                        lastManaWarn = DateTime.Now;
                    }
                }
            }
        }

        // MSG_RAID_TARGET_UPDATE = 0x321 (801)
        public void SetRaidIcon(WoWGuid targetGuid, RaidIcon icon)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.MSG_GROUP_SET_PLAYER_ICON); 
            packet.Write((byte)icon); // Verify Structure: often Byte(Icon), UInt64(Guid) or SetMode?
            // Actually usually: Mode (0=Set?, 1=Get?), then Guid?
            // Let's guess simple structure for 3.3.5:
            // 1 byte Icon Index
            // 8 bytes Guid target
            
            packet.Write((byte)icon); // Send Icon twice? No, let's try standard Set
            packet.Write(targetGuid.GetOldGuid());
            client.Send(packet);
        }
    }
}

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
    public class HealingMgr
    {
        private WorldServerClient client;
        private string prefix;
        private bool isRunning = false;
        private Thread loop;

        public bool AutoHealEnabled { get; set; } = false;
        public int HealThresholdPercent { get; set; } = 70; // Heals under 70%
        public uint HealSpellId { get; set; } = 5185; // Default: Healing Touch Rank 1 (Druid)

        // Predefined simple spells (Druid/Priest)
        private Dictionary<string, uint> Spells = new Dictionary<string, uint>()
        {
            { "healingtouch", 5185 }, // Druid
            { "rejuv", 774 },         // Druid
            { "regrowth", 8936 },     // Druid
            { "lesserheal", 2050 },   // Priest
            { "heal", 2054 },         // Priest
            { "flashheal", 2061 },    // Priest
            { "renew", 139 },         // Priest
            { "holylight", 635 },     // Paladin
            { "flashoflight", 19750 },// Paladin
            { "healingwave", 331 },   // Shaman
        };

        public HealingMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            loop = new Thread(HealLoop);
            loop.IsBackground = true;
            loop.Start();
            Log.WriteLine(LogType.Success, "HealingMgr started for altruism.", prefix);
        }

        public void Stop()
        {
            isRunning = false;
        }

        public bool SetHealSpell(string name)
        {
            if (Spells.ContainsKey(name.ToLower()))
            {
                HealSpellId = Spells[name.ToLower()];
                return true;
            }
            return false;
        }

        private void HealLoop()
        {
            while (isRunning)
            {
                if (AutoHealEnabled && client.Connected && client.player != null)
                {
                    try
                    {
                        // Decision Making
                        // 1. Identify Most Wounded member (Group or Nearby Friendly)
                        // Group is better via PartyMgr, but fall back to ObjectMgr for self/nearby.

                        Object bestTarget = null;
                        float lowestHealthPct = 100.0f;

                        // Check Self
                        if (client.player.Health > 0)
                        {
                            float myHp = GetHealthPercent(client.player);
                            if (myHp < lowestHealthPct)
                            {
                                lowestHealthPct = myHp;
                                bestTarget = client.player;
                            }
                        }

                        // Check Party via PartyMgr (Remote stats)
                        // But to cast, we need their Object (GUID).
                        // PartyMgr gives us GUID and HP. We need to check if we can Target them.
                        // Ideally we check ObjectMgr to see if they are in range.
                        if (client.partyMgr != null)
                        {
                            foreach (var member in client.partyMgr.Members)
                            {
                                if (member.Health == 0 || member.MaxHealth == 0) continue;
                                float pct = ((float)member.Health / (float)member.MaxHealth) * 100.0f;
                                if (pct < lowestHealthPct)
                                {
                                    // Verify if we have the object in range
                                    Object obj = WotlkClient.Clients.ObjectMgr.GetInstance().getObject(new WoWGuid(member.Guid));
                                    if (obj != null)
                                    {
                                        lowestHealthPct = pct;
                                        bestTarget = obj;
                                    }
                                }
                            }
                        }
                        
                        // Fallback: Check all friendly units nearby if not in party
                        // (Use existing logic but prioritize)
                        if (client.partyMgr == null || client.partyMgr.Members.Count == 0)
                        {
                            var objects = WotlkClient.Clients.ObjectMgr.GetInstance().GetAllObjects();
                            foreach(var obj in objects)
                            {
                                if (obj.Type == ObjectType.Player && obj.Guid.GetOldGuid() != client.player.Guid.GetOldGuid())
                                {
                                     // Assuming friendly players
                                     float hp = GetHealthPercent(obj);
                                     if (hp > 0 && hp < lowestHealthPct)
                                     {
                                         lowestHealthPct = hp;
                                         bestTarget = obj;
                                     }
                                }
                            }
                        }

                        // Action
                        if (bestTarget != null && lowestHealthPct < HealThresholdPercent)
                        {
                            Console.WriteLine($"[Heal] Emergency! {bestTarget.Name} is at {lowestHealthPct:F1}%. Casting...");
                            
                            // Stop moving to cast
                            if (client.movementMgr.isMoving)
                                client.movementMgr.Stop();

                            // Target and Cast
                            // client.SetSelection(bestTarget.Guid); // Not strictly needed for CMSG_CAST_SPELL but good for visual
                            client.CastSpell(bestTarget.Guid.GetOldGuid(), HealSpellId);
                            
                            Thread.Sleep(2500); // Wait for GCD/Cast
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.WriteLine(LogType.Error, "HealLoop Error: " + ex.Message, prefix);
                    }
                }
                Thread.Sleep(1000); // Check every second
            }
        }

        private float GetHealthPercent(Object obj)
        {
            if (obj == null || obj.Health == 0) return 0;
            // MaxHealth default or read?
            // Object class needs MaxHealth. If 0, assume 100 (full) or find field.
            // Wait, Object.cs usually parses Health/MaxHealth.
            // Let's assume MaxHealth is available.
            // If MaxHealth is 0, we can't calculate.
            
            // NOTE: Object.cs might not expose MaxHealth directly.
            // Index for MaxHealth is usually Health + something.
            // UNIT_FIELD_MAXHEALTH = UNIT_FIELD_HEALTH + 2 (usually).
            
            uint maxHealth = GetMaxHealth(obj);
            if (maxHealth == 0) return 100;

            return ((float)obj.Health / (float)maxHealth) * 100.0f;
        }

        private uint GetMaxHealth(Object obj)
        {
             // Try to access via Fields if property not there
             // UNIT_FIELD_MAXHEALTH is index 18 (0x12) typically after UNIT_FIELD_HEALTH (16/0x10) ?
             // Need to check generic structure or OpCodes.
             // Actually, simplest is to assume Class provides it or add it.
             // For now, let's look at Fields:
             // Object.Health is defined.
             
             // Hack: let's try to find it dynamically or assume obj.Fields has it.
             // Constants.UpdateFields.UNIT_FIELD_MAXHEALTH
             if (obj.Fields != null && obj.Fields.Length > (int)UpdateFields.UNIT_FIELD_MAXHEALTH)
             {
                 return obj.Fields[(int)UpdateFields.UNIT_FIELD_MAXHEALTH];
             }
             return 100; // Fail safe
        }
    }
}

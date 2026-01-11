using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WotlkClient.Constants;
using WotlkClient.Shared;
using WotlkClient.Network;
using WotlkClient.AI;

namespace WotlkClient.Clients
{
    public class InitiativeMgr
    {
        private WorldServerClient client;
        private string prefix;
        private bool isRunning = false;
        private Thread loop;
        private Random rnd = new Random();

        // Config
        public bool InitiativeEnabled { get; set; } = true;
        public int BoredomThresholdSeconds { get; set; } = 45;

        // State
        private DateTime lastActionTime = DateTime.Now;

        public InitiativeMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            loop = new Thread(InitiativeLoop);
            loop.IsBackground = true;
            loop.Start();
            Log.WriteLine(LogType.Success, "InitiativeMgr started (Free Will).", prefix);
            lastActionTime = DateTime.Now;
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void NotifyAction()
        {
            lastActionTime = DateTime.Now;
        }

        private void InitiativeLoop()
        {
            while (isRunning)
            {
                if (InitiativeEnabled && client.Connected && client.player != null)
                {
                    try
                    {
                        // Detect Activity
                        if (client.movementMgr.isMoving || client.combatMgr.AutoCombatEnabled == false) // If combat disabled, maybe paused?
                        {
                            // If moving, we are active
                            lastActionTime = DateTime.Now;
                        }

                        // Check Boredom
                        if ((DateTime.Now - lastActionTime).TotalSeconds > BoredomThresholdSeconds)
                        {
                            // We are bored!
                            ProposeActivity();
                            lastActionTime = DateTime.Now; // Reset timer so we don't spam
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.WriteLine(LogType.Error, "InitiativeLoop Error: " + ex.Message, prefix);
                    }
                }
                Thread.Sleep(2000);
            }
        }

        private void ProposeActivity()
        {
            // 1. Check for nearby Quest Givers
            Object qGiver = FindQuestGiver();
            if (qGiver != null)
            {
                string msg = $"Je m'ennuie... Oh, {qGiver.Name} a l'air d'avoir du travail pour nous !";
                client.SendChatMsg(ChatMsg.Say, Languages.Universal, msg);
                
                // Emote Point
                PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_TEXT_EMOTE);
                packet.Write((int)25); // POINT
                packet.Write(qGiver.Guid.GetOldGuid()); // Target
                client.Send(packet);
                
                // Look at him
                client.SetSelection(qGiver.Guid);
                return;
            }

            // 2. Just Random AI Chatter if AI is enabled
            if (client.aiChatMgr.AIEnabled)
            {
                // Trigger AI to say something about the environment or asking for orders
                // We'll simulate a prompt injection
                // For now, hardcoded:
                string[] boredomPhrases = {
                    "On attend quoi, chef ?",
                    "Mes lames s'engourdissent...",
                    "C'est calme. Trop calme.",
                    "Je pourrais manger un Sanglier entier.",
                    "On bouge ?"
                };
                string phrase = boredomPhrases[rnd.Next(boredomPhrases.Length)];
                client.SendChatMsg(ChatMsg.Say, Languages.Universal, phrase);
                
                // Random animation
                PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_TEXT_EMOTE); // standstate maybe better?
                // Emote ID for Yawn = 18 ?
                packet.Write((int)14); // RUDE? NO.
                // client.Send(packet);
            }
        }

        private Object FindQuestGiver()
        {
            var objects = ObjectMgr.GetInstance().GetAllObjects();
            foreach (var obj in objects)
            {
                if (obj.Type == ObjectType.Unit)
                {
                     if (obj.Fields != null && obj.Fields.Length > (int)UpdateFields.UNIT_NPC_FLAGS)
                    {
                        uint flags = obj.Fields[(int)UpdateFields.UNIT_NPC_FLAGS];
                        // UNIT_NPC_FLAG_QUESTGIVER = 2
                        if ((flags & 2) != 0)
                        {
                            return obj;
                        }
                    }
                }
            }
            return null;
        }
    }
}

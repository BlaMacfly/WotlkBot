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
    public class SocialMgr
    {
        private WorldServerClient client;
        private string prefix;
        
        public bool SocialEnabled { get; set; } = true;

        public SocialMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            Log.WriteLine(LogType.Success, "SocialMgr started (Conscience).", prefix);
        }

        public void Stop()
        {
        }

        // --- Event Handlers ---

        // --- Event Handlers ---

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_LEVELUP_INFO)]
        public void HandleLevelUp(PacketIn packet)
        {
            if (!SocialEnabled) return;

            uint newLevel = packet.ReadUInt32();

            // AI Reaction
            GenerateAIReaction($"Le joueur vient de passer niveau {newLevel}. Félicite-le chaleureusement en mentionnant sa puissance grandissante.", EmoteType.CHEER);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_ZONE_UNDER_ATTACK)]
        public void HandleZoneAttack(PacketIn packet)
        {
            if (!SocialEnabled) return;
            // Limit frequency? handled by AI delay naturally
            GenerateAIReaction("La zone est attaquée ! Alerte le joueur avec panique.", EmoteType.ROAR);
        }
        
        public void OnPlayerDeath()
        {
            if (!SocialEnabled) return;
            GenerateAIReaction("Le joueur (toi) vient de mourir. Râle ou supplie qu'on te rez.", EmoteType.CRY);
        }

        public void OnMasterDeath(string masterName)
        {
            if (!SocialEnabled) return;
            GenerateAIReaction($"Ton maître {masterName} vient de mourir ! Crie vengeance ou désespoir.", EmoteType.CRY);
        }

        private void GenerateAIReaction(string contextPrompt, EmoteType emote)
        {
            if (client.aiChatMgr == null || !client.aiChatMgr.AIEnabled) return;

            // Run in thread to allow non-blocking Http request
            ThreadPool.QueueUserWorkItem(state => 
            {
                try 
                {
                    string aiResponse = client.aiChatMgr.GetResponse(contextPrompt, "Événement Jeu");
                    if (!string.IsNullOrEmpty(aiResponse))
                    {
                        client.SendChatMsg(ChatMsg.Say, Languages.Universal, aiResponse);
                        client.SendEmote(emote);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogType.Error, "SocialMgr AI Error: " + ex.Message, prefix);
                }
            });
        }
    }
}

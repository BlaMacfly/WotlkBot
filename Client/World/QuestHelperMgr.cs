using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WotlkClient.Constants;
using WotlkClient.Shared;
using WotlkClient.Network;

namespace WotlkClient.Clients
{
    public class QuestHelperMgr
    {
        private WorldServerClient client;
        private string prefix;
        
        // Mock Knowledge Base
        private Dictionary<string, string> questHints = new Dictionary<string, string>()
        {
            { "kobold", "Je parie qu'ils sont dans la mine au sud." },
            { "mine", "Direction la mine !" },
            { "murloc", "Sûrement près de la rivière ou sur la plage." },
            { "loup", "Les loups rôdent souvent dans la forêt au nord." },
            { "wolf", "Les loups rôdent souvent dans la forêt au nord." },
            { "sanglier", "Il y a des sangliers près des fermes." },
            { "boar", "Il y a des sangliers près des fermes." },
            { "defias", "Attention aux bandits Defias, ils ont des camps un peu partout." },
            { "deliver", "Une livraison ? Regarde ta carte, c'est sûrement indiqué." }
        };

        public QuestHelperMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            Log.WriteLine(LogType.Success, "QuestHelperMgr started.", prefix);
        }

        public void Stop()
        {
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_QUESTGIVER_QUEST_DETAILS)]
        public void HandleQuestDetails(PacketIn packet)
        {
            // Packet layout can be complex (GUID, ID, Title strings...)
            // Standard: Guid (8), QuestID (4), Title (String), Details (String), Objectives (String)
            try
            {
                ulong guid = packet.ReadUInt64();
                ulong questId = packet.ReadUInt32(); // or 32?
                
                // Read Strings
                string title = packet.ReadString();
                string details = packet.ReadString();
                string objectives = packet.ReadString();

                client.SendChatMsg(ChatMsg.Say, Languages.Universal, $"C'est parti pour : {title} !");
                
                // Analyze Title for hints
                foreach(var kvp in questHints)
                {
                    if (title.ToLower().Contains(kvp.Key) || objectives.ToLower().Contains(kvp.Key))
                    {
                        client.SendChatMsg(ChatMsg.Say, Languages.Universal, $"Indice : {kvp.Value}");
                        break; 
                    }
                }
            }
            catch(Exception ex)
            {
                 Log.WriteLine(LogType.Error, "QuestDetails Parse Error: " + ex.Message, prefix);
            }
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_QUESTUPDATE_ADD_KILL)]
        public void HandleQuestUpdateKill(PacketIn packet)
        {
            try
            {
                uint questId = packet.ReadUInt32();
                uint entry = packet.ReadUInt32();
                uint count = packet.ReadUInt32();
                uint required = packet.ReadUInt32();
                ulong guid = packet.ReadUInt64();

                client.SendChatMsg(ChatMsg.Say, Languages.Universal, $"Et de {count} ! Encore {required - count} à avoir.");
                client.SendEmote(EmoteType.CHEER);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogType.Error, "QuestUpdateKill Parse Error: " + ex.Message, prefix);
            }
        }
    }
}

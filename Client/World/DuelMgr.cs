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
    public class DuelMgr
    {
        private WorldServerClient client;
        private string prefix;
        
        public bool AutoAcceptDuel { get; set; } = true;

        public DuelMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            Log.WriteLine(LogType.Success, "DuelMgr started (PvP).", prefix);
        }

        public void Stop()
        {
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_DUEL_REQUESTED)]
        public void HandleDuelRequest(PacketIn packet)
        {
            try
            {
                ulong arbiterGuid = packet.ReadUInt64();
                ulong challengerGuid = packet.ReadUInt64();

                if (AutoAcceptDuel)
                {
                    client.SendChatMsg(ChatMsg.Say, Languages.Universal, "Un duel ? Tu vas tâter de mon acier !");
                    client.SendEmote(EmoteType.ROAR);

                    // Accept
                    PacketOut response = new PacketOut(WorldServerOpCode.CMSG_DUEL_ACCEPTED);
                    response.Write(arbiterGuid);
                    client.Send(response);
                }
            }
            catch(Exception ex)
            {
                Log.WriteLine(LogType.Error, "DuelRequest Error: " + ex.Message, prefix);
            }
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_DUEL_WINNER)]
        public void HandleDuelWinner(PacketIn packet)
        {
            // packet structure: bool(EndDuel?), String(Name), String(Unk)
            /*
             * Wait, 3.3.5a SMSG_DUEL_WINNER might be:
             * uint8 count?
             * loop { string name, ... }
             * Actually usually just a notification. 
             * Let's just assume if we get this, someone won.
             */
             
             // Simple interaction without reading packet (safer)
             client.SendChatMsg(ChatMsg.Say, Languages.Universal, "Bien joué ! C'était intense.");
             client.SendEmote(EmoteType.BOW);
             
             // Stop combat just in case
             client.combatMgr.currentTarget = null;
             client.SendAttackStop();
        }
        
        [PacketHandlerAtribute(WorldServerOpCode.SMSG_DUEL_COUNTDOWN)]
        public void HandleDuelCountdown(PacketIn packet)
        {
             uint count = packet.ReadUInt32();
             if (count == 3000) // 3 seconds
                client.SendChatMsg(ChatMsg.Say, Languages.Orcish, "3...");
             else if (count == 2000)
                client.SendChatMsg(ChatMsg.Say, Languages.Orcish, "2...");
             else if (count == 1000)
                client.SendChatMsg(ChatMsg.Say, Languages.Orcish, "1... PRÊT !");
        }
    }
}

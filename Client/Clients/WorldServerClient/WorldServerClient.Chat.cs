using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using WotlkClient.Shared;
using WotlkClient.AI;
using WotlkClient.Network;
using WotlkClient.Constants;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace WotlkClient.Clients
{
    partial class WorldServerClient
    {
        private ArrayList ChatQueued = new ArrayList();

        private void HandleBotCommand(string message, UInt64 user)
        {
            
            string cmd = message.Substring(4, message.Length - 5);
            Console.WriteLine("bot " + cmd);
            if (cmd == "move forward")
            {
                //MoveForward();
            }
            else if(cmd == "move stop")
            {
                //MoveStop();
            }
            else if(cmd == "heal me")
            {
                CastSpell(user, 2050); // LESSER_HEAL
            }
            else if (cmd == "buff me")
            {
                CastSpell(user, 1243); // PW_FORTITUDE
            }
            else if(cmd == "follow me")
            {   
                WoWGuid fguid = new WoWGuid(user);
                if (ObjectMgr.GetInstance().objectExists(fguid))
                {
                    if (ObjectMgr.GetInstance().getObject(fguid).Position != null && player != null && player.Position != null)
                    {
                        Console.WriteLine("follow");
                        movementMgr.Waypoints.Add(ObjectMgr.GetInstance().getObject(fguid).Position);
                        movementMgr.Start();
                        
                    }
                }
            }
        }



        [PacketHandlerAtribute(WorldServerOpCode.SMSG_CHANNEL_NOTIFY)]
        public void HandleChannelNotify(PacketIn packet)
        {
            
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_MESSAGECHAT)]
        public void HandleChat(PacketIn packet)
        {
            try
            {
                string channel = null;
                UInt64 guid = 0;
                WoWGuid fguid = null, fguid2 = null;
                string username = null;

                byte Type = packet.ReadByte();
                UInt32 Language = packet.ReadUInt32();

                guid = packet.ReadUInt64();
                fguid = new WoWGuid(guid);
                packet.ReadInt32();

                if ((ChatMsg)Type == ChatMsg.Channel)
                {
                    channel = packet.ReadString();
                }

                if (Type == 47)
                    return;
                fguid2 = new WoWGuid(packet.ReadUInt64());

                UInt32 Length = packet.ReadUInt32();
                string Message = Encoding.Default.GetString(packet.ReadBytes((int)Length));

                byte afk = 0;
           
                if (fguid.GetOldGuid() == 0)
                {
                    username = "System";
                }
                else
                {
                    if (ObjectMgr.GetInstance().objectExists(fguid))
                        username = ObjectMgr.GetInstance().getObject(fguid).Name;
                }

                if (Message.StartsWith("bot ") && (ChatMsg)Type == ChatMsg.Whisper)
                {
                    Console.WriteLine("guid " + guid.ToString());
                    HandleBotCommand(Message, guid);
                    return;
                }

                // Handle AI chat for whispers AND Say
                if (((ChatMsg)Type == ChatMsg.Whisper || (ChatMsg)Type == ChatMsg.Say) && 
                    aiChatMgr != null && aiChatMgr.AIEnabled && username != null &&
                    player != null && fguid.GetOldGuid() != player.Guid.GetOldGuid())
                {
                    try
                    {
                        Console.WriteLine($"[AI] Received whisper from {username}: {Message}");
                        
                        // Build Context
                        uint currentMapId = terrainMgr != null ? terrainMgr.MapId : 0;
                        string preciseZone = KnowledgeMgr.GetZoneName(player.Position, (int)currentMapId);
                        
                        // Inject Memory
                        string memories = memoryMgr != null ? memoryMgr.GetContextSummary() : "";
                        
                        string context = $"Ma position : {preciseZone} (Map {currentMapId}). Je suis un {player.Race} {player.Class}. {memories}";

                        string response = aiChatMgr.GetResponse(Message, context);
                        
                        if (!string.IsNullOrEmpty(response))
                        {
                            // Check for AI commands [CMD: ...]
                            if (response.Contains("[CMD:"))
                            {
                                int cmdStart = response.IndexOf("[CMD:");
                                int cmdEnd = response.IndexOf("]", cmdStart);
                                if (cmdEnd > cmdStart)
                                {
                                    string cmdContent = response.Substring(cmdStart + 5, cmdEnd - cmdStart - 5).Trim();
                                    string cleanResponse = response.Remove(cmdStart, cmdEnd - cmdStart + 1).Trim();
                                    
                                    Console.WriteLine($"[AI] Detected Action: {cmdContent}");
                                    
                                    // SAFELY execute command so it doesn't block the chat response if it fails
                                    try 
                                    {
                                        ExecuteAICommand(cmdContent, username);
                                    }
                                    catch(Exception cmdEx)
                                    {
                                        Console.WriteLine($"[AI] Command '{cmdContent}' failed: {cmdEx.Message}");
                                    }
                                    
                                    response = cleanResponse;
                                }
                            }

                            if (!string.IsNullOrEmpty(response))
                            {
                                Console.WriteLine($"[AI] Response: {response}");
                                ChatMsg responseType = (ChatMsg)Type == ChatMsg.Say ? ChatMsg.Say : ChatMsg.Whisper;
                                SendChatMsg(responseType, GetMyLanguage(), response, username);
                            }
                        }
                    }
                    catch (Exception aiEx)
                    {
                        Log.WriteLine(LogType.Error, "AI Response error: {0}", prefix, aiEx.Message);
                    }
                    return;
                }

                if (username == null)
                {
                    ChatQueue que = new ChatQueue();
                    que.GUID = fguid;
                    que.Type = Type;
                    que.Language = Language;
                    if ((ChatMsg)Type == ChatMsg.Channel)
                        que.Channel = channel;
                    que.Length = Length;
                    que.Message = Message;
                    que.AFK = afk;
                    ChatQueued.Add(que);
                    QueryName(guid);
                    return;
                }
                
                Log.WriteLine(LogType.Chat, "[{0}] {1}", prefix, username, Message);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogType.Error, "Exception Occured", prefix);
                Log.WriteLine(LogType.Error, "Message: {0}", prefix, ex.Message);
                Log.WriteLine(LogType.Error, "Stacktrace: {0}", prefix, ex.StackTrace);
            }
        }

        public void SendChatMsg(ChatMsg Type, Languages Language, string Message)
        {
            if (Type != ChatMsg.Whisper || Type != ChatMsg.Channel)
                SendChatMsg(Type, Language, Message, "");
        }

        public void SendChatMsg(ChatMsg Type, Languages Language, string Message, string To)
        {
            // Split message if too long (WoW limit is ~255, keep safe margin at 200)
            const int MAX_LEN = 200;
            if (Message.Length > MAX_LEN)
            {
                for (int i = 0; i < Message.Length; i += MAX_LEN)
                {
                    string chunk = Message.Substring(i, Math.Min(MAX_LEN, Message.Length - i));
                    SendChatMsgSinglePacket(Type, Language, chunk, To);
                    System.Threading.Thread.Sleep(500); // Anti-flood
                }
            }
            else
            {
                SendChatMsgSinglePacket(Type, Language, Message, To);
            }
        }

        private void SendChatMsgSinglePacket(ChatMsg Type, Languages Language, string Message, string To)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_MESSAGECHAT);
            packet.Write((UInt32)Type);
            packet.Write((UInt32)Language);
            if ((Type == ChatMsg.Whisper || Type == ChatMsg.Channel) && To != "")
                packet.Write(To);
            packet.Write(Message);
            Send(packet);

            // TTS: Speak if it's our own message (Say)
            if (Type == ChatMsg.Say && voiceMgr != null)
                voiceMgr.Speak(Message);
        }

        public void SendEmoteMsg(ChatMsg Type, Languages Language, string Message, string To)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_TEXT_EMOTE);
            packet.Write((UInt32)Type);
            packet.Write((UInt32)Language);
            packet.Write(Message);
            Send(packet);
        }

        public void JoinChannel(string channel, string password)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_JOIN_CHANNEL);
            packet.Write((UInt32)0);
            packet.Write((UInt16)0);
            packet.Write(channel);
            packet.Write("");
            Send(packet);
        }

        private Languages GetMyLanguage()
        {
            Race r = (Race)0;
            if (player != null) r = player.Race;

            if ((int)r == 0)
            {
                if (Charlist != null)
                {
                    foreach (var c in Charlist)
                    {
                        if (c.Name != null && mCharname != null && c.Name.Equals(mCharname, StringComparison.OrdinalIgnoreCase))
                        {
                            r = (Race)c.Race;
                            break;
                        }
                    }
                }
            }

            switch(r)
            {
                case Race.Orc:
                case Race.Undead:
                case Race.Tauren:
                case Race.Troll:
                case Race.BloodElf:
                case Race.Goblin:
                    return Languages.Orcish;
                default:
                    return Languages.Common;
            }
        }
        private void ExecuteAICommand(string cmd, string targetName)
        {
            string[] parts = cmd.Split(' ');
            string action = parts[0].ToLower();

            if (action == "come" || action == "follow")
            {
                // Default to targetName if no arg provided, or if arg is "me"
                string target = targetName;
                if (parts.Length > 1 && parts[1] != "me") target = parts[1];

                Console.WriteLine($"[AI] DEBUG: Attempting to follow '{target}' (Origin: '{targetName}')");

                Object obj = ObjectMgr.GetInstance().FindObjectByName(target);
                if (obj != null)
                {
                    if (obj.Position != null)
                    {
                        Console.WriteLine($"[AI] DEBUG: Target found! {obj.Name} at {obj.Position}. Starting movement.");
                        movementMgr.Waypoints.Clear();
                        
                        // Use New Continuous Follow Logic
                        movementMgr.FollowTarget = obj;
                        movementMgr.Start();
                    }
                    else
                    {
                        Console.WriteLine($"[AI] DEBUG: Target found ({obj.Name}) but Request Position is NULL.");
                    }
                }
                else
                {
                    Console.WriteLine($"[AI] DEBUG: Target '{target}' NOT FOUND in ObjectMgr.");
                    // Dump a few names to see what we have
                    /*
                    foreach(var o in ObjectMgr.GetInstance().Objects)
                    {
                        if(o.Value.Name != null) Console.WriteLine($"[AI] DEBUG: Saw object '{o.Value.Name}'");
                    }
                    */
                }
            }
            else if (action == "stop")
            {
                movementMgr.FollowTarget = null;
                movementMgr.Waypoints.Clear();
                movementMgr.Stop();
                MoveStop(player.Position, MovementMgr.MM_GetTime());
            }
            else if (action == "info")
            {
                uint currentMapId = terrainMgr != null ? terrainMgr.MapId : 0;
                string mapName = "Inconnu";
                switch(currentMapId)
                {
                    case 0: mapName = "Royaumes de l'Est"; break;
                    case 1: mapName = "Kalimdor"; break;
                    case 530: mapName = "Outreterre"; break;
                    case 571: mapName = "Norfendre"; break;
                    default: mapName = "Map " + currentMapId; break;
                }
                
                string preciseZone = KnowledgeMgr.GetZoneName(player.Position, (int)currentMapId);
                
                string info = $"Je suis un {player.Race} {player.Class}. Région: {mapName}. Lieu: {preciseZone}. Coordonnées: {player.Position}";
                Console.WriteLine($"[AI] Info request: {info}");
                SendChatMsg(ChatMsg.Say, Languages.Orcish, info);
            }
            else if (action == "trainer" || action == "learn")
            {
                // Find closest trainer
                int actualMapId = (int)(terrainMgr != null ? terrainMgr.MapId : 1); 
                
                var best = KnowledgeMgr.GetClosestTrainer(player.Position, actualMapId);
                
                if (best.HasValue)
                {
                     TrainerLocation t = best.Value;
                     Console.WriteLine($"[AI] Closest Trainer: {t.Name} at {t.ZoneName} ({t.Position})");
                     
                     // Inform user
                     string msg = $"Je vais voir {t.Name} à {t.ZoneName}. Suis-moi !";
                     SendChatMsg(ChatMsg.Say, Languages.Orcish, msg);

                     // Move
                     movementMgr.Waypoints.Clear();
                     movementMgr.Waypoints.Add(t.Position);
                     movementMgr.Start();
                }
                else
                {
                    SendChatMsg(ChatMsg.Say, Languages.Orcish, "Je ne trouve pas de maître de classe dans le coin...");
                }
            }
            else if (action == "emote")
            {
                 if (parts.Length > 1)
                 {
                    string emote = parts[1].ToLower();
                    int id = 0;
                     switch(emote)
                    {
                        case "dance": id = 10; break;
                        case "sleep": id = 28; break;
                        case "sit": id = 13; break;
                        case "wave": id = 3; break;
                        case "bow": id = 2; break;
                        default: int.TryParse(emote, out id); break;
                    }
                    PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_TEXT_EMOTE);
                    packet.Write((int)id); // Text Emote ID
                    packet.Write((int)0);  // Emote Num
                    packet.Write((long)0); // Guid
                    Send(packet);
                 }
            }
        }
    }

    
   

    public struct ChatQueue
    {
        public WoWGuid GUID;
        public byte Type;
        public UInt32 Language;
        public string Channel;
        public UInt32 Length;
        public string Message;
        public byte AFK;

    };
}

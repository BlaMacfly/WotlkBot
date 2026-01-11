using System;
using System.Linq;
using WotlkClient.Clients;
using WotlkClient.Constants;
using WotlkClient.Shared;
using WotlkClient.Network;

namespace WotlkBot
{
    /// <summary>
    /// Processes CLI commands for controlling the bot
    /// </summary>
    public class CommandProcessor
    {
        private readonly WorldServerClient wclient;
        private readonly string charName;
        private bool running = true;

        public CommandProcessor(WorldServerClient client, string characterName)
        {
            wclient = client;
            charName = characterName;
        }

        public bool IsRunning => running;

        /// <summary>
        /// Start the command loop
        /// </summary>
        public void StartCommandLoop()
        {
            Console.WriteLine("");
            Console.WriteLine("=== Bot Command Interface ===");
            Console.WriteLine("Type 'help' for available commands");
            Console.WriteLine("");

            while (running && wclient.Connected)
            {
                Console.Write($"[{charName}]> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                ProcessCommand(input.Trim());
            }
        }

        /// <summary>
        /// Process a single command
        /// </summary>
        public void ProcessCommand(string input)
        {
            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string command = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            try
            {
                switch (command)
                {
                    case "help":
                    case "?":
                        ShowHelp();
                        break;

                    case "say":
                        CmdSay(args);
                        break;

                    case "whisper":
                    case "w":
                        CmdWhisper(args);
                        break;

                    case "yell":
                        CmdYell(args);
                        break;

                    case "follow":
                        CmdFollow(args);
                        break;

                    case "stop":
                        CmdStop();
                        break;

                    case "goto":
                        CmdGoto(args);
                        break;

                    case "cast":
                        CmdCast(args);
                        break;

                    case "attack":
                        CmdAttack(args);
                        break;

                    case "status":
                    case "pos":
                        CmdStatus();
                        break;

                    case "players":
                    case "list":
                        CmdListPlayers();
                        break;

                    case "logout":
                    case "quit":
                    case "exit":
                        CmdLogout();
                        break;

                    case "autoheal":
                        CmdAutoHeal(args);
                        break;

                    case "healat":
                        CmdHealAt(args);
                        break;

                    case "healspell":
                        CmdHealSpell(args);
                        break;

                    case "ai":
                        CmdAI(args);
                        break;

                    case "aiprompt":
                        CmdAIPrompt(args);
                        break;

                    case "aireset":
                        CmdAIReset();
                        break;

                    case "come":
                    case "c":
                        CmdCome(args);
                        break;

                    case "emote":
                    case "e":
                        CmdEmote(args);
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("=== Available Commands ===");
            Console.WriteLine("");
            Console.WriteLine("  CHAT:");
            Console.WriteLine("    say <message>              - Send a message to /say");
            Console.WriteLine("    whisper <name> <message>   - Whisper to a player");
            Console.WriteLine("    yell <message>             - Yell a message");
            Console.WriteLine("");
            Console.WriteLine("  MOVEMENT:");
            Console.WriteLine("    follow <name>              - Follow a player");
            Console.WriteLine("    come <name>                - Move to a player's position");
            Console.WriteLine("    stop                       - Stop all movement");
            Console.WriteLine("    goto <x> <y> <z>           - Move to coordinates");
            Console.WriteLine("    emote <id>                 - Perform an emote (e.g. 1=talk, 10=dance)");
            Console.WriteLine("");
            Console.WriteLine("  COMBAT:");
            Console.WriteLine("    cast <spellId>             - Cast a spell on self");
            Console.WriteLine("    cast <spellId> <target>    - Cast a spell on target");
            Console.WriteLine("    attack <target>            - Attack a target");
            Console.WriteLine("");
            Console.WriteLine("  AUTO-HEAL:");
            Console.WriteLine("    autoheal on/off            - Enable/disable auto-healing");
            Console.WriteLine("    healat <percent>           - Set heal threshold (e.g., 30)");
            Console.WriteLine("    healspell <name|id>        - Set heal spell (e.g., flashheal)");
            Console.WriteLine("");
            Console.WriteLine("  AI CHAT:");
            Console.WriteLine("    ai on/off                  - Enable/disable AI responses");
            Console.WriteLine("    aiprompt <text>            - Set AI personality");
            Console.WriteLine("    aireset                    - Reset AI conversation");
            Console.WriteLine("");
            Console.WriteLine("  INFO:");
            Console.WriteLine("    status                     - Show current position and state");
            Console.WriteLine("    players                    - List nearby players/objects");
            Console.WriteLine("");
            Console.WriteLine("  SYSTEM:");
            Console.WriteLine("    logout                     - Disconnect from server");
            Console.WriteLine("    help                       - Show this help");
            Console.WriteLine("");
        }

        private void CmdSay(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: say <message>");
                return;
            }
            string message = string.Join(" ", args);
            wclient.SendChatMsg(ChatMsg.Say, Languages.Universal, message);
            Console.WriteLine($"[Say] {message}");
        }

        private void CmdWhisper(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: whisper <name> <message>");
                return;
            }
            string target = args[0];
            string message = string.Join(" ", args.Skip(1));
            wclient.SendChatMsg(ChatMsg.Whisper, Languages.Universal, message, target);
            Console.WriteLine($"[Whisper to {target}] {message}");
        }

        private void CmdYell(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: yell <message>");
                return;
            }
            string message = string.Join(" ", args);
            wclient.SendChatMsg(ChatMsg.Yell, Languages.Universal, message);
            Console.WriteLine($"[Yell] {message}");
        }

        private void CmdFollow(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: follow <player name>");
                return;
            }
            string targetName = args[0];
            
            // Find the target in the object manager
            var target = ObjectMgr.GetInstance().FindObjectByName(targetName);
            if (target != null && target.Position != null)
            {
                wclient.movementMgr.Waypoints.Clear();
                wclient.movementMgr.Waypoints.Add(target.Position);
                wclient.movementMgr.Start();
                Console.WriteLine($"Following {targetName}...");
            }
            else
            {
                Console.WriteLine($"Player '{targetName}' not found nearby.");
            }
        }

        private void CmdStop()
        {
            wclient.movementMgr.Waypoints.Clear();
            wclient.movementMgr.Stop();
            if (wclient.player != null && wclient.player.Position != null)
            {
                uint time = MovementMgr.MM_GetTime();
                wclient.MoveStop(wclient.player.Position, time);
            }
            Console.WriteLine("Movement stopped.");
        }

        private void CmdGoto(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: goto <x> <y> <z>");
                return;
            }

            if (!float.TryParse(args[0], out float x) ||
                !float.TryParse(args[1], out float y) ||
                !float.TryParse(args[2], out float z))
            {
                Console.WriteLine("Invalid coordinates. Usage: goto <x> <y> <z>");
                return;
            }

            var destination = new Coordinate(x, y, z, 0);
            wclient.movementMgr.Waypoints.Clear();
            wclient.movementMgr.Waypoints.Add(destination);
            wclient.movementMgr.Start();
            Console.WriteLine($"Moving to ({x}, {y}, {z})...");
        }

        private void CmdCast(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: cast <spellId> [target]");
                return;
            }

            if (!uint.TryParse(args[0], out uint spellId))
            {
                Console.WriteLine("Invalid spell ID.");
                return;
            }

            UInt64 targetGuid;
            if (args.Length > 1)
            {
                // Cast on specified target
                string targetName = args[1];
                var target = ObjectMgr.GetInstance().FindObjectByName(targetName);
                if (target == null)
                {
                    Console.WriteLine($"Target '{targetName}' not found.");
                    return;
                }
                targetGuid = target.Guid.GetOldGuid();
                Console.WriteLine($"Casting spell {spellId} on {targetName}...");
            }
            else
            {
                // Cast on self
                targetGuid = wclient.player.Guid.GetOldGuid();
                Console.WriteLine($"Casting spell {spellId} on self...");
            }

            wclient.CastSpell(targetGuid, spellId);
        }

        private void CmdAttack(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: attack <target name>");
                return;
            }

            string targetName = args[0];
            var target = ObjectMgr.GetInstance().FindObjectByName(targetName);
            if (target == null)
            {
                Console.WriteLine($"Target '{targetName}' not found.");
                return;
            }

            wclient.combatMgr.AttackTarget(target);
            wclient.combatMgr.Start();
            Console.WriteLine($"Attacking {targetName}...");
        }

        private void CmdStatus()
        {
            Console.WriteLine("");
            Console.WriteLine("=== Bot Status ===");
            Console.WriteLine($"  Character: {charName}");
            Console.WriteLine($"  Connected: {wclient.Connected}");
            
            if (wclient.player != null)
            {
                Console.WriteLine($"  Health: {wclient.player.Health}");
                if (wclient.player.Position != null)
                {
                    var pos = wclient.player.Position;
                    Console.WriteLine($"  Position: X={pos.X:F2}, Y={pos.Y:F2}, Z={pos.Z:F2}");
                    Console.WriteLine($"  Orientation: {pos.O:F2}");
                }
            }
            else
            {
                Console.WriteLine("  Player object not yet initialized.");
            }
            
            Console.WriteLine($"  Waypoints: {wclient.movementMgr.Waypoints.Count}");
            //Console.WriteLine($"  Combat targets: {wclient.combatMgr.Targets.Count}");
            Console.WriteLine($"  Auto-heal: {(wclient.healingMgr.AutoHealEnabled ? "ON" : "OFF")} (threshold: {wclient.healingMgr.HealThresholdPercent}%, spell: {wclient.healingMgr.HealSpellId})");
            Console.WriteLine("");
        }

        private void CmdListPlayers()
        {
            Console.WriteLine("");
            Console.WriteLine("=== Nearby Objects ===");
            var objects = ObjectMgr.GetInstance().GetAllObjects();
            if (objects.Count == 0)
            {
                Console.WriteLine("  No objects in range.");
            }
            else
            {
                foreach (var obj in objects)
                {
                    string posStr = obj.Position != null 
                        ? $"({obj.Position.X:F1}, {obj.Position.Y:F1}, {obj.Position.Z:F1})"
                        : "(unknown)";
                    Console.WriteLine($"  - {obj.Name ?? "Unknown"} at {posStr}");
                }
            }
            Console.WriteLine("");
        }

        private void CmdLogout()
        {
            Console.WriteLine("Logging out...");
            wclient.healingMgr.Stop();
            wclient.Logout();
            running = false;
        }

        private void CmdAutoHeal(string[] args)
        {
            if (args.Length == 0)
            {
                // Toggle
                wclient.healingMgr.AutoHealEnabled = !wclient.healingMgr.AutoHealEnabled;
            }
            else
            {
                string arg = args[0].ToLower();
                if (arg == "on" || arg == "true" || arg == "1")
                    wclient.healingMgr.AutoHealEnabled = true;
                else if (arg == "off" || arg == "false" || arg == "0")
                    wclient.healingMgr.AutoHealEnabled = false;
                else
                {
                    Console.WriteLine("Usage: autoheal [on/off]");
                    return;
                }
            }

            if (wclient.healingMgr.AutoHealEnabled)
            {
                wclient.healingMgr.Start();
                Console.WriteLine($"Auto-heal ENABLED (threshold: {wclient.healingMgr.HealThresholdPercent}%, spell: {wclient.healingMgr.HealSpellId})");
            }
            else
            {
                wclient.healingMgr.Stop();
                Console.WriteLine("Auto-heal DISABLED");
            }
        }

        private void CmdHealAt(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Current heal threshold: {wclient.healingMgr.HealThresholdPercent}%");
                Console.WriteLine("Usage: healat <percent>");
                return;
            }

            if (!int.TryParse(args[0], out int percent) || percent < 1 || percent > 99)
            {
                Console.WriteLine("Invalid percentage. Must be between 1 and 99.");
                return;
            }

            wclient.healingMgr.HealThresholdPercent = percent;
            Console.WriteLine($"Heal threshold set to {percent}%");
        }

        private void CmdHealSpell(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Current heal spell ID: {wclient.healingMgr.HealSpellId}");
                Console.WriteLine("Usage: healspell <name|id>");
                Console.WriteLine("Available names: lesserheal, heal, greaterheal, flashheal, renew, shield");
                Console.WriteLine("                 holylight, flashoflight, healingtouch, rejuv, healingwave");
                return;
            }

            // Try to parse as spell ID first
            if (uint.TryParse(args[0], out uint spellId))
            {
                wclient.healingMgr.HealSpellId = spellId;
                Console.WriteLine($"Heal spell ID set to {spellId}");
                return;
            }

            // Try to parse as spell name
            if (wclient.healingMgr.SetHealSpell(args[0]))
            {
                Console.WriteLine($"Heal spell set to {args[0]} (ID: {wclient.healingMgr.HealSpellId})");
            }
            else
            {
                Console.WriteLine($"Unknown spell name: {args[0]}");
                Console.WriteLine("Available: lesserheal, heal, greaterheal, flashheal, renew, shield, holylight, flashoflight, healingtouch, rejuv, healingwave");
            }
        }

        private void CmdAI(string[] args)
        {
            if (args.Length == 0)
            {
                // Toggle
                wclient.aiChatMgr.AIEnabled = !wclient.aiChatMgr.AIEnabled;
            }
            else
            {
                string arg = args[0].ToLower();
                if (arg == "on" || arg == "true" || arg == "1")
                    wclient.aiChatMgr.AIEnabled = true;
                else if (arg == "off" || arg == "false" || arg == "0")
                    wclient.aiChatMgr.AIEnabled = false;
                else
                {
                    Console.WriteLine("Usage: ai [on/off]");
                    return;
                }
            }

            if (wclient.aiChatMgr.AIEnabled)
            {
                if (!wclient.aiChatMgr.IsModelAvailable())
                {
                    Console.WriteLine("[AI] Model not found! Please download Phi-3 GGUF model.");
                    Console.WriteLine($"[AI] Place it at: {wclient.aiChatMgr.ModelPath}");
                    wclient.aiChatMgr.AIEnabled = false;
                    return;
                }
                Console.WriteLine("[AI] Initializing AI (this may take a moment)...");
                if (wclient.aiChatMgr.Initialize())
                {
                    Console.WriteLine("[AI] AI chat ENABLED - whispers will get intelligent responses");
                }
                else
                {
                    wclient.aiChatMgr.AIEnabled = false;
                }
            }
            else
            {
                Console.WriteLine("[AI] AI chat DISABLED");
            }
        }

        private void CmdAIPrompt(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Current AI prompt: {wclient.aiChatMgr.SystemPrompt}");
                Console.WriteLine("Usage: aiprompt <new personality description>");
                return;
            }

            wclient.aiChatMgr.SystemPrompt = string.Join(" ", args);
            wclient.aiChatMgr.ResetConversation();
            Console.WriteLine($"AI prompt updated to: {wclient.aiChatMgr.SystemPrompt}");
        }

        private void CmdCome(string[] args)
        {
            // Find me (the player typing the command is usually implicit in single-player bots, 
            // but here 'args' might be empty if we want to come to the current target or the master)
            // For now, let's assume 'come to me' isn't easily possible without knowing WHO typed it (CLI has no user).
            // So we'll use 'come <playername>'
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: come <player name>");
                return;
            }
            string targetName = args[0];
            var target = ObjectMgr.GetInstance().FindObjectByName(targetName);
            if (target != null && target.Position != null)
            {
                wclient.movementMgr.Waypoints.Clear();
                wclient.movementMgr.Waypoints.Add(target.Position);
                wclient.movementMgr.Start();
                Console.WriteLine($"Moving to {targetName}...");
            }
            else
            {
                Console.WriteLine($"Player '{targetName}' not found nearby.");
            }
        }

        private void CmdEmote(string[] args)
        {
             if (args.Length == 0)
            {
                Console.WriteLine("Usage: emote <id> OR emote <text> (e.g. emote 1 (talk), emote dance)");
                return;
            }

            // Simple text emote implementation if possible, or numeric ID
            if (int.TryParse(args[0], out int emoteId))
            {
                // Send emote packet (OpCode SMSG_EMOTE / CMSG_TEXT_EMOTE / MSG_TEXT_EMOTE?)
                // Actually CMSG_TEXT_EMOTE = 0x104, CMSG_EMOTE = 0x102
                PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_EMOTE);
                packet.Write((int)emoteId);
                wclient.Send(packet);
                Console.WriteLine($"Emote {emoteId} sent.");
            }
            else
            {
                // Text emote (e.g. "dance")
                string text = args[0].ToLower();
                int id = 0;
                switch(text)
                {
                    case "dance": id = 10; break; // EMOTE_STATE_DANCE
                    case "sleep": id = 28; break; // EMOTE_STATE_SLEEP
                    case "sit": id = 13; break; // EMOTE_STATE_SIT
                    case "stand": id = 0; break; // EMOTE_STATE_STAND
                    case "talk": id = 1; break; // EMOTE_ONESHOT_TALK
                    case "bow": id = 2; break; // EMOTE_ONESHOT_BOW
                    case "wave": id = 3; break; // EMOTE_ONESHOT_WAVE
                    case "cheer": id = 4; break; // EMOTE_ONESHOT_CHEER
                    case "eat": id = 7; break; // EMOTE_ONESHOT_EAT
                    default: 
                        Console.WriteLine("Unknown emote name. Try ID."); 
                        return;
                }
                
                PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_EMOTE);
                packet.Write((int)id);
                wclient.Send(packet);
                Console.WriteLine($"Emote {text} ({id}) sent.");
            }
        }

        private void CmdAIReset()
        {
            wclient.aiChatMgr.ResetConversation();
            Console.WriteLine("AI conversation history reset.");
        }
    }
}

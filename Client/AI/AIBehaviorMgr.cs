using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WotlkClient.Clients;
using WotlkClient.Constants;
using WotlkClient.Shared;

namespace WotlkClient.AI
{
    public class AIBehaviorMgr
    {
        private WorldServerClient _client;
        private Dictionary<ulong, DateTime> _greetedPlayers;
        private const double GREET_COOLDOWN_MINUTES = 10;
        private const float DETECTION_RADIUS = 10.0f;

        public AIBehaviorMgr(WorldServerClient client)
        {
            _client = client;
            _greetedPlayers = new Dictionary<ulong, DateTime>();
        }

        public void Update()
        {
            if (_client == null || _client.player == null) return;
            if (_client.aiChatMgr == null || !_client.aiChatMgr.AIEnabled) return;

            ScanForPlayers();
        }

        private void ScanForPlayers()
        {
            try
            {
                var objects = ObjectMgr.GetInstance().getObjectArray();
                foreach (var obj in objects)
                {
                    // Check if object is a player (Type == 4 usually, verify ObjectType enum)
                    // And not me
                    if (obj.Type == ObjectType.Player && obj.Guid.GetOldGuid() != _client.player.Guid.GetOldGuid())
                    {
                        float dist = Terrain.TerrainMgr.CalculateDistance(_client.player.Position, obj.Position);
                        if (dist <= DETECTION_RADIUS)
                        {
                            HandlePlayerProximity(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIBehavior] Error scanning players: {ex.Message}");
            }
        }

        private void HandlePlayerProximity(WotlkClient.Clients.Object player)
        {
            ulong guid = player.Guid.GetOldGuid();

            // Check Cooldown
            if (_greetedPlayers.ContainsKey(guid))
            {
                if ((DateTime.Now - _greetedPlayers[guid]).TotalMinutes < GREET_COOLDOWN_MINUTES)
                {
                    return; // Too soon
                }
            }

            // Greet
            _greetedPlayers[guid] = DateTime.Now;
            
            string name = player.Name;
            if (string.IsNullOrEmpty(name)) return;

            Console.WriteLine($"[AIBehavior] Detected player {name} at {DETECTION_RADIUS}m. Greeting...");
            
            // Generate Greeting
            string greeting = _client.aiChatMgr.GetGreeting(name);
            if (!string.IsNullOrEmpty(greeting))
            {
                _client.SendChatMsg(ChatMsg.Say, Languages.Common, greeting, ""); // Say messages don't need target
            }
        }
    }
}

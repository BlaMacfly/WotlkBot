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
    public class GatherMgr
    {
        private WorldServerClient client;
        private string prefix;
        private bool isRunning = false;
        private Thread loop;

        public bool GatherEnabled { get; set; } = true;
        public float ScanRadius { get; set; } = 40.0f;

        // Keywords to look for
        private List<string> ResourceKeywords = new List<string>()
        {
            "Vein", "Deposit", "Ore", // Mining
            "Peacebloom", "Silverleaf", "Earthroot", "Mageroyal", "Briarthorn", "Herb" // Herbalism
        };

        private DateTime lastGatherTime = DateTime.MinValue;
        private WoWGuid currentTargetNode = null;

        public GatherMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            loop = new Thread(GatherLoop);
            loop.IsBackground = true;
            loop.Start();
            Log.WriteLine(LogType.Success, "GatherMgr started (Professions).", prefix);
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void GatherLoop()
        {
            while (isRunning)
            {
                if (GatherEnabled && client.Connected && client.player != null)
                {
                    try
                    {
                        // Conditions: Not in Combat, Not Moving (unless moving to node), Not dead
                        if (client.combatMgr.AutoCombatEnabled && client.combatMgr.currentTarget != null) 
                        {
                            // In combat, ignore gathering
                            Thread.Sleep(1000);
                            continue;
                        }

                        // Scan for nodes
                        Object bestNode = FindClosestNode();

                        if (bestNode != null)
                        {
                            float dist = Terrain.TerrainMgr.CalculateDistance(client.player.Position, bestNode.Position);
                            
                            if (dist < 5.0f)
                            {
                                // We are close! Interact.
                                if ((DateTime.Now - lastGatherTime).TotalSeconds > 10) // Anti-spam
                                {
                                    Console.WriteLine($"[Gather] Interacting with {bestNode.Name}");
                                    client.movementMgr.Stop();
                                    
                                    // Send interact
                                    PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_GAMEOBJ_USE);
                                    packet.Write(bestNode.Guid.GetOldGuid());
                                    client.Send(packet);

                                    // Emote working
                                    client.SendEmote(EmoteType.WORK); // 
                                    
                                    lastGatherTime = DateTime.Now;
                                    currentTargetNode = null;
                                    Thread.Sleep(3000); // Wait for cast
                                }
                            }
                            else
                            {
                                // Move to it
                                if (currentTargetNode == null || currentTargetNode.GetOldGuid() != bestNode.Guid.GetOldGuid())
                                {
                                    // New node found
                                    client.SendChatMsg(ChatMsg.Say, Languages.Universal, $"J'ai vu {bestNode.Name} ! Je vais le chercher.");
                                    currentTargetNode = bestNode.Guid;
                                }

                                if (!client.movementMgr.isMoving)
                                {
                                    client.movementMgr.Waypoints.Clear();
                                    client.movementMgr.Waypoints.Add(bestNode.Position);
                                    client.movementMgr.Start();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.WriteLine(LogType.Error, "GatherLoop Error: " + ex.Message, prefix);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private Object FindClosestNode()
        {
            var objects = ObjectMgr.GetInstance().GetAllObjects();
            Object best = null;
            float bestDist = ScanRadius;

            foreach (var obj in objects)
            {
                if (obj.Type == ObjectType.GameObject)
                {
                    // Check Name
                    if (obj.Name != null && ResourceKeywords.Any(k => obj.Name.Contains(k)))
                    {
                        float dist = Terrain.TerrainMgr.CalculateDistance(client.player.Position, obj.Position);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = obj;
                        }
                    }
                }
            }
            return best;
        }
    }
}

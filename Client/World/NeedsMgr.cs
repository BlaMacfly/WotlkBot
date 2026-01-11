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
    public class NeedsMgr
    {
        private WorldServerClient client;
        private string prefix;
        private bool isRunning = false;
        private Thread loop;

        // Configuration
        public bool NeedsEnabled { get; set; } = true;
        public int MinFreeSlots { get; set; } = 5;
        public int MinDurabilityPercent { get; set; } = 30;

        // State
        public DateTime lastVendorVisit = DateTime.MinValue;

        public NeedsMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            loop = new Thread(NeedsLoop);
            loop.IsBackground = true;
            loop.Start();
            Log.WriteLine(LogType.Success, "NeedsMgr started (Vendor/Repair).", prefix);
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void NeedsLoop()
        {
            while (isRunning)
            {
                if (NeedsEnabled && client.Connected && client.player != null)
                {
                    try
                    {
                        // 1. Check Status (Mocked for now as Inventory/Durability parsing is complex)
                        // In a real bot, we would iterate Player Fields for durability 
                        // and Bag Objects for slots.
                        
                        bool needsVendor = false;
                        bool needsRepair = false;

                        // Mock: If we haven't visited vendor in 20 minutes, force a check
                        if ((DateTime.Now - lastVendorVisit).TotalMinutes > 20)
                        {
                            needsVendor = true;
                            // Log.WriteLine(LogType.Normal, "[Needs] Time to visit a vendor (Routine check).", prefix);
                        }

                        // Basic check for full bags (if we receive "Inventory Full" errors? Hard to catch without event)
                        
                        // 2. Action
                        if (needsVendor || needsRepair)
                        {
                            // Find nearby Vendor
                            Object vendor = FindClosestVendor();
                            if (vendor != null)
                            {
                                float dist = Terrain.TerrainMgr.CalculateDistance(client.player.Position, vendor.Position);
                                if (dist < 5.0f)
                                {
                                    // Interact
                                    Console.WriteLine($"[Needs] interacting with {vendor.Name} to sell/repair.");
                                    client.movementMgr.Stop();
                                    InteractMsg(vendor.Guid);
                                    
                                    Thread.Sleep(1000);
                                    
                                    // Sell Grey Items (Mock logic: Send Sell packet for specific slots if we knew them)
                                    // For now, just Repair All
                                    RepairAll(vendor.Guid);
                                    
                                    lastVendorVisit = DateTime.Now;
                                }
                                else if (dist < 100.0f) // Only go if close
                                {
                                    // Move to Vendor
                                    if (!client.movementMgr.isMoving)
                                    {
                                        Console.WriteLine($"[Needs] Moving to vendor {vendor.Name}");
                                        client.movementMgr.Waypoints.Clear();
                                        client.movementMgr.Waypoints.Add(vendor.Position);
                                        client.movementMgr.Start();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.WriteLine(LogType.Error, "NeedsLoop Error: " + ex.Message, prefix);
                    }
                }
                Thread.Sleep(5000); // Check every 5s
            }
        }

        private Object FindClosestVendor()
        {
            var objects = ObjectMgr.GetInstance().GetAllObjects();
            Object best = null;
            float bestDist = 9999f;
            
            foreach(var obj in objects)
            {
                if (obj.Type == ObjectType.Unit && IsVendor(obj))
                {
                    float dist = Terrain.TerrainMgr.CalculateDistance(client.player.Position, obj.Position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = obj;
                    }
                }
            }
            return best;
        }

        private bool IsVendor(Object unit)
        {
            // Check NPC Flags
            if (unit.Fields != null && unit.Fields.Length > (int)UpdateFields.UNIT_NPC_FLAGS)
            {
                uint flags = unit.Fields[(int)UpdateFields.UNIT_NPC_FLAGS];
                // 128 = UNIT_NPC_FLAG_VENDOR
                // 4096 = UNIT_NPC_FLAG_REPAIR
                if ((flags & 128) != 0 || (flags & 4096) != 0)
                    return true;
            }
            return false;
        }

        private void InteractMsg(WoWGuid guid)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_GOSSIP_HELLO);
            packet.Write(guid.GetOldGuid());
            client.Send(packet);
        }

        private void RepairAll(WoWGuid npcGuid)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_REPAIR_ITEM);
            packet.Write(npcGuid.GetOldGuid()); // NPC Guid
            packet.Write((ulong)0); // Item Guid (0 for all?) check mangos source usually
            // Actually CMSG_REPAIR_ITEM:
            // NPCHANDLE (8)
            // ITEMGUID (8) -> 0 to repair all ?
            // In some versions, you just send NPC GUID and it repairs all if flag is set?
            // TrinityCore: if itemGuid is 0, repair all.
            packet.Write((ulong)0); 
            client.Send(packet);
            Console.WriteLine("[Needs] Repair request sent.");
        }
    }
}

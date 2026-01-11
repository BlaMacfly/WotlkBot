using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WotlkClient.Constants;
using WotlkClient.Shared;
using WotlkClient.Network;
using WotlkClient.Terrain;

namespace WotlkClient.Clients
{
    public class CombatMgr
    {
        private WorldServerClient client;
        private string prefix;
        private bool isRunning = false;
        private Thread loop;
        private Object player;
        public Object currentTarget = null;
        private Random rnd = new Random();

        // Config
        public bool AutoCombatEnabled { get; set; } = true; // Enabled by default now
        public float AttackRangeMelee { get; set; } = 4.0f;
        public float AttackRangeSpell { get; set; } = 25.0f;

        // Spells & Skills
        private const uint WRATH = 5176;     // Colère (Druid Rank 1)
        private const uint MOONFIRE = 8921;  // Moonfire (Druid Rank 1)
        private const uint HEROIC_STRIKE = 78; // Heroic Strike (Warrior)

        public CombatMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        public void SetPlayer(Object p)
        {
            player = p;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            loop = new Thread(CombatLoop);
            loop.IsBackground = true;
            loop.Start();
            Log.WriteLine(LogType.Success, "CombatMgr started (Self Defense Active).", prefix);
        }

        public void Stop()
        {
            isRunning = false;
            //if (loop != null) loop.Abort(); // Avoid Abort if possible, let flag handle it
        }

        private void CombatLoop()
        {
            while (isRunning)
            {
                try
                {
                    if (AutoCombatEnabled && player != null && client.Connected)
                    {
                        // 1. Find Target if we don't have one
                        if (currentTarget == null || currentTarget.Health <= 0)
                        {
                            FindAggroTarget();
                        }

                        // 2. Fight Target
                        if (currentTarget != null)
                        {
                            if(currentTarget.Health > 0)
                            {
                                ProcessCombat(currentTarget);
                            }
                            else
                            {
                                Console.WriteLine($"[Combat] Target {currentTarget.Name} is dead. Victory!");
                                StopCombat();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                   // Log.WriteLine(LogType.Error, "CombatLoop Error: " + ex.Message, prefix);
                }
                Thread.Sleep(500);
            }
        }

        private void StopCombat()
        {
            currentTarget = null;
            client.MoveStop(player.Position, MovementMgr.MM_GetTime());
            client.movementMgr.Stop(); // Ensure movement stops
            client.SendAttackStop();
        }

        // Detect mobs targeting ME or FollowTarget (Master)
        private void FindAggroTarget()
        {
            var units = ObjectMgr.GetInstance().GetAllObjects();
            
            // Defines who we protect
            List<ulong> protectedGuids = new List<ulong>();
            protectedGuids.Add(player.Guid.GetOldGuid()); // Protect Self

            // Protect Follow Target (Master)
            if (client.movementMgr.FollowTarget != null)
                protectedGuids.Add(client.movementMgr.FollowTarget.Guid.GetOldGuid());

            foreach (var unit in units)
            {
                if (unit.Type == ObjectType.Unit && unit.Health > 0)
                {
                    // Read Target GUID from fields (Index 18/19 for Unit Field Target)
                    if (unit.Fields != null && unit.Fields.Length > 20)
                    {
                        ulong targetGuid = GetGuidFromFields(unit, (int)UpdateFields.UNIT_FIELD_TARGET);
                        
                        // Check if this unit is targeting one of us
                        if (targetGuid != 0 && protectedGuids.Contains(targetGuid))
                        {
                            Console.WriteLine($"[Combat] Aggro detected! {unit.Name} is attacking us/master!");
                            currentTarget = unit;
                            
                            // Target him back
                            client.SetSelection(unit.Guid);
                            return;
                        }
                    }
                }
            }
        }

        public void AttackTarget(Object target)
        {
            if (target == null) return;
            currentTarget = target;
            Console.WriteLine($"[Combat] Manually attacking {target.Name}");
            ProcessCombat(target);
        }

        private void ProcessCombat(Object target)
        {
            // 1. Face Target
            float angle = TerrainMgr.CalculateAngle(player.Position, target.Position);
            if (Math.Abs(player.Position.O - angle) > 0.5f)
            {
                player.Position.O = angle;
                // Client sync handled by movement or separate packet if static
                // Using MoveStop to update facing while standing still
                if (!client.movementMgr.isMoving)
                     client.MoveStop(player.Position, MovementMgr.MM_GetTime());
            }

            // 2. Check Range & Move
            float dist = TerrainMgr.CalculateDistance(player.Position, target.Position);
            
            // Logic: If Spell User, keep distance? For now simple melee logic is safer to ensure loots
            // Actually, let's try to stay at Spell Range if Druid
            float desiredRange = AttackRangeSpell;
            // Simple check: Identify class?
            // For now, let's just go to Melee range to be safe and ensure interaction
            desiredRange = AttackRangeMelee; 

            if (dist > desiredRange)
            {
                // Move closer
                if (!client.movementMgr.isMoving) 
                {
                    client.movementMgr.Waypoints.Clear();
                    client.movementMgr.Waypoints.Add(target.Position);
                    client.movementMgr.Start();
                }
                return; // Moving...
            }
            else
            {
                // In Range, stop moving to Attack
                if (client.movementMgr.isMoving)
                {
                    client.movementMgr.Waypoints.Clear();
                    client.movementMgr.Stop(); // Calls MoveStop
                }
            }

            // 3. Attack / Cast
            SendAttackSwing(target.Guid);

            // Cast Spell (Wrath) every 2s
            // We use a simple timer check or just spam (server will reject if CD)
            // Ideally we need IsCastReady. For now, rely on server.
            client.CastSpell(target.Guid.GetOldGuid(), WRATH);
        }

        public void SendAttackSwing(WoWGuid guid)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_ATTACKSWING);
            packet.Write(guid.GetOldGuid());
            client.Send(packet);
        }

        private ulong GetGuidFromFields(Object obj, int index)
        {
            if (index + 1 >= obj.Fields.Length) return 0;
            uint low = obj.Fields[index];
            uint high = obj.Fields[index + 1];
            return ((ulong)high << 32) | low; // Little Endian? Actually guid is usually bytes.
                                               // UpdateFields are usually raw uints.
                                               // In TrinityCore: SetUInt64Value(index, guid);
                                               // LOW part is at index, HIGH at index+1
            
        }
    }
}

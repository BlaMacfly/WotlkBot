using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;
using System.Resources;
using WotlkClient.Network;
using WotlkClient.Shared;
using WotlkClient.Constants;
using WotlkClient.Terrain;

namespace WotlkClient.Clients
{
    public class MovementMgr
    {
        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        public static extern uint MM_GetTime();

        private System.Timers.Timer aTimer = new System.Timers.Timer();
        Thread loop = null;
        public MovementFlag Flag = new MovementFlag();
        public List<Coordinate> Waypoints = new List<Coordinate>();
        Coordinate oldLocation;
        UInt32 lastUpdateTime;
        TerrainMgr terrainMgr;
        string prefix;
        WorldServerClient worldServerClient;

        private Object player;
        
        // Formations
        // X = Forward/Back (Positive = Front)
        // Y = Right/Left (Positive = Left?)
        // Z = Up/Down
        public Coordinate FormationOffset { get; set; } = new Coordinate(0, 0, 0); 
        
        public Object FollowTarget { get; set; } = null;
        private float FOLLOW_DISTANCE = 3.0f; // Dynamic based on formation?

        public bool isMoving = false;

        public MovementMgr(WorldServerClient Client, string _prefix)
        {
            worldServerClient = Client;
            terrainMgr = Client.terrainMgr;
            prefix = _prefix;
        }

        public void SetPlayer(Object obj)
        {
            player = obj;
        }

        public void Start()
        {
            try
            {
                Flag.SetMoveFlag(MovementFlags.MOVEMENTFLAG_NONE);
                lastUpdateTime = MM_GetTime();

                loop = new Thread(Loop);
                loop.IsBackground = true;
                loop.Start();
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogType.Error, "Exception Occured", prefix);
                Log.WriteLine(LogType.Error, "Message: {0}", prefix, ex.Message);
                Log.WriteLine(LogType.Error, "Stacktrace: {0}", prefix, ex.StackTrace);
            }
        }

        public void Stop()
        {   
            if (loop != null)
                loop.Abort();
        }

        void Loop()
        {
            while (true)
            {
                try
                {
                    bool shouldMove = false;
                    Object targetObj = null;
                    Coordinate dest = null;
                    float angle = player.Position.O;
                    
                    // Adjust Follow Distance based on formation
                    // If Offset is (0,0,0), distance is standard 3.0f
                    // If Offset is set, we want to reach the specific point, so distance threshold effectively 0 (or small tolerance)
                    float arrivalTolerance = (FormationOffset.X == 0 && FormationOffset.Y == 0) ? 3.0f : 1.0f;

                    // 1. Determine Target/Destination
                    if (FollowTarget != null && FollowTarget.Position != null)
                    {
                        // Calculate Formation Target
                        Coordinate targetPos = FollowTarget.Position;
                        if (FormationOffset.X != 0 || FormationOffset.Y != 0)
                        {
                            // Rotate offset by Target's Orientation
                            float h = targetPos.O;
                            float dx = (float)(FormationOffset.X * Math.Cos(h) - FormationOffset.Y * Math.Sin(h));
                            float dy = (float)(FormationOffset.X * Math.Sin(h) + FormationOffset.Y * Math.Cos(h));
                            
                            targetPos = new Coordinate(targetPos.X + dx, targetPos.Y + dy, targetPos.Z);
                            // We should probably check Z via TerrainMgr but let's assume flat for now or use Target Z
                        }

                        float dist = TerrainMgr.CalculateDistance(player.Position, targetPos);
                        
                        if (dist > arrivalTolerance)
                        {
                            shouldMove = true;
                            targetObj = FollowTarget;
                            angle = TerrainMgr.CalculateAngle(player.Position, targetPos);
                            
                            // If we are very far, maybe face the targetObj directly? 
                            // But usually we face the destination.
                        }
                    }
                    else if (Waypoints.Count != 0)
                    {
                        dest = Waypoints.First();
                        if (dest != null)
                        {
                            float dist = TerrainMgr.CalculateDistance(player.Position, dest);
                            angle = TerrainMgr.CalculateAngle(player.Position, dest);
                            
                            if (dist > 1) 
                                shouldMove = true;
                            else
                                Waypoints.Remove(dest); // Arrived
                        }
                    }

                    // 2. Execute Movement State Machine
                    UInt32 timeNow = MM_GetTime();
                    UInt32 diff = (timeNow - lastUpdateTime);
                    lastUpdateTime = timeNow;

                    if (shouldMove)
                    {
                        bool rotationChanged = Math.Abs(angle - player.Position.O) > 0.25f; 
                        
                        if (rotationChanged)
                            player.Position.O = angle;

                        if (!isMoving)
                        {
                            // Start Moving
                            Flag.SetMoveFlag(MovementFlags.MOVEMENTFLAG_FORWARD);
                            worldServerClient.MoveForward(player.Position, timeNow);
                            isMoving = true;
                        }
                        else if (rotationChanged) 
                        {
                             worldServerClient.MoveForward(player.Position, timeNow);
                        }

                        UpdatePosition(diff);
                    }
                    else
                    {
                        // Should Stop
                        if (isMoving)
                        {
                            Flag.SetMoveFlag(MovementFlags.MOVEMENTFLAG_NONE);
                            worldServerClient.MoveStop(player.Position, timeNow);
                            isMoving = false;
                        }
                        
                        if (FollowTarget == null && Waypoints.Count == 0 && Flag.MoveFlags != 0)
                        {
                            Flag.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogType.Error, "Exception Occured " + ex.Message, prefix);
                }
                Thread.Sleep(50);
            }
        }


        public bool UpdatePosition(UInt32 diff)
        {
            double h; double speed;

            if (player == null)
                return false;

            if (Flag.IsMoveFlagSet(MovementFlags.MOVEMENTFLAG_FORWARD))
            {
                speed = 7.0;
            }
            else
                return false;

            float predictedDX = 0;
            float predictedDY = 0;

            if (oldLocation == null)
                oldLocation = player.Position;


            h = player.Position.O;

            float dt = (float)diff / 1000f;
            float dx = (float)Math.Cos(h) * (float)speed * dt;
            float dy = (float)Math.Sin(h) * (float)speed * dt;

            predictedDX = dx;
            predictedDY = dy;

            Coordinate loc = player.Position;
            float realDX = loc.X - oldLocation.X;
            float realDY = loc.Y - oldLocation.Y;

            float predictDist = (float)Math.Sqrt(predictedDX * predictedDX + predictedDY * predictedDY);
            float realDist = (float)Math.Sqrt(realDX * realDX + realDY * realDY);

            if (predictDist > 0.0)
            {

                Coordinate expected = new Coordinate(loc.X + predictedDX, loc.Y + predictedDY, player.Position.Z, player.Position.O);
                expected = terrainMgr.getZ(expected);
                if (player.Position.Equals(expected))
                    return false;
                player.Position = expected;
                
            }

            oldLocation = loc;
            return true;
        }

        public float CalculateDistance(Coordinate c1)
        {
            return TerrainMgr.CalculateDistance(player.Position, c1);
        }
    }


    public class MovementFlag
    {
        public uint MoveFlags;

        public void Clear()
        {
            MoveFlags = new uint();
        }

        public void SetMoveFlag(MovementFlags flag)
        {
            MoveFlags |= (uint)flag;
        }
        public void UnSetMoveFlag(MovementFlags flag)
        {
            MoveFlags &= ~(uint)flag;
        }
        public bool IsMoveFlagSet(MovementFlags flag)
        {
            return ((MoveFlags & (uint)flag) >= 1) ? true : false;
        }
    }
}

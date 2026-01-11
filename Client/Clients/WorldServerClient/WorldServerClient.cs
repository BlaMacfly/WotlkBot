using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Timers;
using WotlkClient.Constants;
using WotlkClient.Crypt;
using WotlkClient.Network;
using WotlkClient.Shared;
using WotlkClient.Shared;
using WotlkClient.Terrain;
using WotlkClient.AI;

namespace WotlkClient.Clients
{
    public delegate void AuthCompletedCallBack(uint taskResult);
    public delegate void CharLoginCompletedCallBack(uint taskResult);
    public delegate void CharEnumCompletedCallBack(uint taskResult);
    public delegate void InviteCallBack(string inviter);

    public partial class WorldServerClient
    {
        private static object _lockObj = new object();
        AuthCompletedCallBack authCompletedCallBack;
        CharEnumCompletedCallBack charEnumCompletedCallBack;
        CharLoginCompletedCallBack charLoginCompletedCallBack;
        InviteCallBack inviteCallBack;
        private UInt32 packetNumber = 0;
        private UInt32 ServerSeed;
        private UInt32 ClientSeed;
        private Random random = new Random();

        public Socket mSocket = null;

        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        public static extern uint MM_GetTime();

        
        private System.Timers.Timer aTimer = new System.Timers.Timer();
        private System.Timers.Timer uTimer = new System.Timers.Timer();
        private UInt32 Ping_Seq;
        private UInt32 Ping_Req_Time;
        private UInt32 Ping_Res_Time;
        public UInt32 Latency;

        // Connection Info
        readonly string mUsername;
        readonly string mCharname;
        private byte[] mKey;
        public bool Connected;
        string prefix;

        //Packet Handling
        private PacketHandler pHandler;
        private PacketLoop pLoop = null;
        public PacketCrypt mCrypt;
        
        //Managers
        
        public MovementMgr movementMgr = null;
        public CombatMgr combatMgr = null;
        public TerrainMgr terrainMgr = null;
        public HealingMgr healingMgr = null;
        public AIChatMgr aiChatMgr = null;
        public AIBehaviorMgr aiBehaviorMgr = null;
        public PartyMgr partyMgr = null;
        public NeedsMgr needsMgr = null;
        public InitiativeMgr initiativeMgr = null;
        public SocialMgr socialMgr = null;
        public GatherMgr gatherMgr = null;
        public QuestHelperMgr questHelperMgr = null;
        public DuelMgr duelMgr = null;
        public StrategyMgr strategyMgr = null;
        public MemoryMgr memoryMgr = null;
        public VoiceMgr voiceMgr = null;
        
        //
        public Realm realm;
        public Character[] Charlist = new Character[0];

        public Object player = null;

        public WorldServerClient(string user, Realm rl, byte[] key, string charName, AuthCompletedCallBack callback)
        {
            // Set data path to WoW Root or current dir. Avoid using Username as path!
            // Assuming bot is in bin, WoW root is up two levels? Or just hardcode for this user env.
            prefix = @"j:\World of Warcraft 3.3.5a"; 
            
            mUsername = user.ToUpper();
            mCharname = charName;
            terrainMgr = new TerrainMgr(prefix);
            movementMgr = new MovementMgr(this, prefix);
            combatMgr = new CombatMgr(this, prefix);
            healingMgr = new HealingMgr(this, prefix);
            partyMgr = new PartyMgr(this, prefix);
            needsMgr = new NeedsMgr(this, prefix);
            initiativeMgr = new InitiativeMgr(this, prefix);
            socialMgr = new SocialMgr(this, prefix);
            gatherMgr = new GatherMgr(this, prefix);
            questHelperMgr = new QuestHelperMgr(this, prefix);
            duelMgr = new DuelMgr(this, prefix);
            strategyMgr = new StrategyMgr(this, prefix);
            memoryMgr = new MemoryMgr();
            voiceMgr = new VoiceMgr();

            aiChatMgr = new AIChatMgr(prefix);
            aiBehaviorMgr = new AIBehaviorMgr(this);
            realm = rl;
            mKey = key;
            authCompletedCallBack = callback;
        }

        public WorldServerClient()
        {

        }

        public void SetInviteCallback(InviteCallBack callback)
        {
            inviteCallBack = callback;
        }

        public void Logout()
        {
            PacketOut ping = new PacketOut(WorldServerOpCode.CMSG_LOGOUT_REQUEST);
            Send(ping);
        }

        public void Connect()
        {
            string[] address = realm.Address.Split(':');
            byte[] test = new byte[1];
            test[0] = 10;
            mCrypt = new PacketCrypt(test);
            IPAddress WSAddr = Dns.GetHostAddresses(address[0])[0];
            int WSPort = Int32.Parse(address[1]);
            IPEndPoint ep = new IPEndPoint(WSAddr, WSPort);
            
            try
            {
                mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                mSocket.Connect(ep);
                Log.WriteLine(LogType.Success, "Successfully connected to WorldServer at: {0}!", prefix, realm.Address);

            }
            catch (SocketException ex)
            {
                Log.WriteLine(LogType.Error, "Failed to connect to realm: {0}", prefix, ex.Message);
                Disconnect();
                if (charLoginCompletedCallBack != null)
                    charLoginCompletedCallBack(1);
                return;
            }

            byte[] nullA = new byte[24];
            mCrypt = new PacketCrypt(nullA);
            Connected = true;
            pHandler = new PacketHandler(this, prefix);
            pLoop = new PacketLoop(this, mSocket, prefix);
            pLoop.Start();
            pHandler.Initialize();
        }

        void PingLoop()
        {
            aTimer.Elapsed += new ElapsedEventHandler(Ping);
            aTimer.Interval = 1000000;
            aTimer.Enabled = true;

            Ping_Seq = 1;
            Latency = 1;
        }

        void Ping(object source, ElapsedEventArgs e)
        {
            while(!mSocket.Connected)
            {
                aTimer.Enabled = false;
                aTimer.Stop();
                return;
            }

            Ping_Req_Time = MM_GetTime();

            PacketOut ping = new PacketOut(WorldServerOpCode.CMSG_PING);
            ping.Write(Ping_Seq);
            ping.Write(Latency);
            Send(ping);
            Console.WriteLine("pinging");
        }

        public void Send(PacketOut packet)
        {
            lock (_lockObj)
            {
                try
                {
                    if (!Connected)
                        return;
                    Log.WriteLine(LogType.Network, "Sending packet: {0}", prefix, packet.packetId);

                    Byte[] Data = packet.ToArray();

                    int Length = Data.Length;
                    byte[] Packet = new byte[2 + Length];
                    Packet[0] = (byte)(Length >> 8);
                    Packet[1] = (byte)(Length & 0xff);
                    Data.CopyTo(Packet, 2);
                    mCrypt.Encrypt(Packet, 0, 6);
                    packetNumber++;
                    Log.WriteLine(LogType.Packet, "{0}", prefix, packet.ToHex(packetNumber));
                    mSocket.Send(Packet);
                }
                catch (SocketException se)
                {
                    Log.WriteLine(LogType.Error, "Exception Occured in packet {0}", prefix, packetNumber);
                    Log.WriteLine(LogType.Error, "Message: {0}", prefix, se.Message);
                    Log.WriteLine(LogType.Error, "Stacktrace: {0}", prefix, se.StackTrace);
                    HardDisconnect();
                    System.Console.WriteLine("Disconnected from server with " + mUsername);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogType.Error, "Exception Occured", prefix);
                    Log.WriteLine(LogType.Error, "Message: {0}", prefix, ex.Message);
                    Log.WriteLine(LogType.Error, "Stacktrace: {0}", prefix, ex.StackTrace);
                }
            }
        }

        public void StartHeartbeat()
        {
            uTimer.Elapsed += new ElapsedEventHandler(Heartbeat);
            uTimer.Interval = 3000;
            uTimer.Enabled = true;
        }

        public void HandlePacket(PacketIn packet)
        {
            Log.WriteLine(LogType.Packet, "{0}", mUsername, packet.ToHex());
            pHandler.HandlePacket(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_PARTY_MEMBER_STATS)]
        public void HandlePartyMemberStats(PacketIn packet)
        {
            if (partyMgr != null)
                partyMgr.HandlePartyMemberStats(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_LEVELUP_INFO)]
        public void HandleLevelUp(PacketIn packet)
        {
            if (socialMgr != null)
                socialMgr.HandleLevelUp(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_QUESTGIVER_QUEST_DETAILS)]
        public void HandleQuestDetails(PacketIn packet)
        {
            if (questHelperMgr != null)
                questHelperMgr.HandleQuestDetails(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_QUESTUPDATE_ADD_KILL)]
        public void HandleQuestUpdateKill(PacketIn packet)
        {
            if (questHelperMgr != null)
                questHelperMgr.HandleQuestUpdateKill(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_DUEL_REQUESTED)]
        public void HandleDuelRequest(PacketIn packet)
        {
            if (duelMgr != null)
                duelMgr.HandleDuelRequest(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_DUEL_WINNER)]
        public void HandleDuelWinner(PacketIn packet)
        {
            if (duelMgr != null)
                duelMgr.HandleDuelWinner(packet);
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_DUEL_COUNTDOWN)]
        public void HandleDuelCountdown(PacketIn packet)
        {
            if (duelMgr != null)
                duelMgr.HandleDuelCountdown(packet);
        }

        public void Disconnect()
        {
            
        }

        public void HardDisconnect()
        {
            if (mSocket != null && mSocket.Connected)
                mSocket.Close();
            
            if (movementMgr != null)
                movementMgr.Stop();
            if (combatMgr != null)
                combatMgr.Stop();
            if (needsMgr != null)
                needsMgr.Stop();
            if (initiativeMgr != null)
                initiativeMgr.Stop();
            if (socialMgr != null)
                socialMgr.Stop();
            if (gatherMgr != null)
                gatherMgr.Stop();
            if (questHelperMgr != null)
                questHelperMgr.Stop();
            if (duelMgr != null)
                duelMgr.Stop();
            if (strategyMgr != null)
                strategyMgr.Stop();
            if (pLoop != null)
                pLoop.Stop();
            Connected = false;
        }

        ~WorldServerClient()
        {
            HardDisconnect();
        }

        public void SetSelection(WoWGuid guid)
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_SET_SELECTION);
            packet.Write(guid.GetOldGuid());
            Send(packet);
        }



        public void SendAttackStop()
        {
            PacketOut packet = new PacketOut(WorldServerOpCode.CMSG_ATTACKSTOP);
            Send(packet);
        }

        void AppendPackedGuid(UInt64 guid, PacketOut stream)
        {
            byte[] packGuid = new byte[9];
            packGuid[0] = 0;
            int size = 1;

            for (byte i = 0; guid != 0; i++)
            {
                if ((guid & 0xFF) != 0)
                {
                    packGuid[0] |= (byte)(1 << i);
                    packGuid[size] = (byte)(guid & 0xFF);
                    size++;
                }
                guid >>= 8;
            }
            stream.Write(packGuid, 0, size);
        }


        UInt64 UnpackGuid(PacketIn stream)
        {
            UInt64 guid = 0;

            byte guidmark = stream.ReadByte();
            byte shift = 0;

            for (int i = 0; i < 8 && stream.Remaining > 0; i++)
            {
                if ((guidmark & (1 << i)) != 0)
                {
                    guid |= ((UInt64)stream.ReadByte()) << shift;
                    shift += 8;
                }
            }
            return guid;
        }
    }
}

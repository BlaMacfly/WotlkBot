using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WotlkClient.Constants;
using WotlkClient.Network;
using WotlkClient.Shared;

namespace WotlkClient.Clients
{
    public class PartyMember
    {
        public ulong Guid;
        public uint Health;
        public uint MaxHealth;
        public uint Level;
        public string Name; // Might need query
    }

    public class PartyMgr
    {
        private WorldServerClient client;
        private string prefix;
        public List<PartyMember> Members = new List<PartyMember>();

        public PartyMgr(WorldServerClient Client, string _prefix)
        {
            client = Client;
            prefix = _prefix;
        }

        [PacketHandlerAtribute(WorldServerOpCode.SMSG_PARTY_MEMBER_STATS)]
        public void HandlePartyMemberStats(PacketIn packet)
        {
            // Format:
            // PackedGuid
            // UInt32 UpdateFlags (if 0x40000000 -> GroupUpdateFlags?)
            // If flag & 0x1 -> Read Health (UInt16/32?)
            // If flag & 0x2 -> Read MaxHealth
            // ...
            // Actually the structure is a bit distinct per version.
            // 3.3.5:
            // PackedGuid (Player)
            // UInt32 status mask
            // Loop while mask != 0
            
            // Wait, standard structure is:
            // Guid (Packed)
            // Mask (UInt32)
            // DEPENDING ON MASK:
            // 0x001: Current Health (Val: UInt32/16?)
            // 0x002: Max Health
            // 0x004: Power
            // 0x008: Max Power
            // 0x010: Level
            // 0x020: Zone
            // 0x040: Position X/Y
            // ...

            try 
            {
                ulong guid = ReadPackedGuid(packet);
                uint mask = packet.ReadUInt32();
                
                PartyMember member = GetMember(guid);
                if (member == null)
                {
                    member = new PartyMember { Guid = guid };
                    Members.Add(member);
                }

                if ((mask & 0x001) != 0) member.Health = packet.ReadUInt32(); // or UInt16? Usually 32 in WotLK
                if ((mask & 0x002) != 0) member.MaxHealth = packet.ReadUInt32();
                if ((mask & 0x004) != 0) 
                {
                    packet.ReadByte(); // Power type
                    packet.ReadUInt16(); // Current Power
                }
                if ((mask & 0x008) != 0) packet.ReadUInt16(); // Max Power
                if ((mask & 0x010) != 0) member.Level = packet.ReadUInt16();
                if ((mask & 0x020) != 0) packet.ReadUInt16(); // Zone
                if ((mask & 0x040) != 0) { packet.ReadUInt16(); packet.ReadUInt16(); } // Pos X, Y

                // Log.WriteLine(LogType.Normal, $"[Party] Update for {guid}: HP {member.Health}/{member.MaxHealth}", prefix);
            }
            catch(Exception ex)
            {
                Log.WriteLine(LogType.Error, "Error parsing PartyStats: " + ex.Message, prefix);
            }
        }

        public PartyMember GetMember(ulong guid)
        {
            return Members.FirstOrDefault(m => m.Guid == guid);
        }

        private ulong ReadPackedGuid(PacketIn packet)
        {
            ulong guid = 0;
            byte mask = packet.ReadByte();
            if (mask == 0) return 0;
            
            for (int i = 0; i < 8; ++i)
            {
                if ((mask & (1 << i)) != 0)
                {
                    byte b = packet.ReadByte();
                    guid |= (ulong)b << (i * 8);
                }
            }
            return guid;
        }
    }
}

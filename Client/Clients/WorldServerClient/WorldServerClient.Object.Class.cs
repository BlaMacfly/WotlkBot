using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WotlkClient.Shared;
using WotlkClient.Constants;

namespace WotlkClient.Clients
{
    public class Object
    {
        public string Name = null;

        public WoWGuid Guid;
        public Coordinate Position = null;
        public ObjectType Type;
        public UInt32[] Fields;
        //public MovementInfo Movement;

        public UInt32 Health
        {
            get
            {
                return Fields[(int)UpdateFields.UNIT_FIELD_HEALTH];
            }
        }

        public UInt32 MaxHealth
        {
            get
            {
                return Fields[(int)UpdateFields.UNIT_FIELD_MAXHEALTH];
            }
        }

        public Race Race
        {
            get
            {
                return (Race)(Fields[(int)UpdateFields.UNIT_FIELD_BYTES_0] & 0xFF);
            }
        }

        public Classname Class
        {
            get
            {
                return (Classname)((Fields[(int)UpdateFields.UNIT_FIELD_BYTES_0] >> 8) & 0xFF);
            }
        }

        public PowerType PowerType
        {
            get
            {
                return (PowerType)((Fields[(int)UpdateFields.UNIT_FIELD_BYTES_0] >> 24) & 0xFF);
            }
        }

        public UInt32 Mana
        {
            get
            {
                return Fields[(int)UpdateFields.UNIT_FIELD_POWER1];
            }
        }

        public UInt32 MaxMana
        {
            get
            {
                return Fields[(int)UpdateFields.UNIT_FIELD_MAXPOWER1];
            }
        }

        public Object(WoWGuid guid)
        {
            this.Guid = guid;
            Fields = new UInt32[2000];
        }

        public void SetPlayer(Character character)
        {
            Name = character.Name;
            Guid = new WoWGuid(character.GUID);
        }

        public void UpdatePlayer(Object obj)
        {
        }

        public void SetField(int x, UInt32 value)
        {
            Fields[x] = value;
        }
    }
}

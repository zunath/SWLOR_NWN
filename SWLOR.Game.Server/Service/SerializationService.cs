﻿using System;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class SerializationService
    {
        public static string Serialize(NWObject @object)
        {
            return NWNXObject.Serialize(@object.Object);
        }
        
        public static NWCreature DeserializeCreature(string base64String, Location location)
        {
            if (location == null) throw new ArgumentException("Invalid target location during creature deserialization.");

            NWCreature creature = NWNXObject.Deserialize(base64String);
            creature.Location = location;

            return creature;
        }

        public static NWItem DeserializeItem(string base64String, NWPlaceable target)
        {
            if (target == null || !target.IsValid)
            {
                throw new ArgumentException("Invalid target placeable during item deserialization.");
            }

            NWItem item = NWNXObject.Deserialize(base64String);
            var result = _.CopyItem(item.Object, target.Object, true);
            item.Destroy();

            return result;
        }

        public static NWItem DeserializeItem(string base64String, Location targetLocation)
        {
            if (targetLocation == null)
            {
                throw new ArgumentException("Invalid target location during item deserialization.");
            }

            NWItem item = NWNXObject.Deserialize(base64String);
            item.Location = targetLocation;
            
            return item;
        }

        public static NWItem DeserializeItem(string base64String, NWCreature target)
        {
            if (target == null || !target.IsValid)
            {
                throw new ArgumentException("Invalid target creature during item deserialization.");
            }

            NWItem item = NWNXObject.Deserialize(base64String);
            var result = _.CopyItem(item.Object, target.Object, true);
            item.Destroy();

            return result;
        }


    }
}

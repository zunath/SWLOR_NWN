﻿using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

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
            if (creature.Object == null) throw new NullReferenceException("Unable to deserialize creature.");
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
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
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
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
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
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
            var result = _.CopyItem(item.Object, target.Object, true);
            item.Destroy();

            return result;
        }


    }
}

using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public class SerializationService : ISerializationService
    {
        
        private readonly INWNXObject _nwnxObject;

        public SerializationService(
            
            INWNXObject nwnxObject)
        {
            
            _nwnxObject = nwnxObject;
        }

        public string Serialize(NWObject @object)
        {
            return _nwnxObject.Serialize(@object.Object);
        }
        
        public NWCreature DeserializeCreature(string base64String, Location location)
        {
            if (location == null) throw new ArgumentException("Invalid target location during creature deserialization.");

            NWCreature creature = _nwnxObject.Deserialize(base64String);
            if (creature.Object == null) throw new NullReferenceException("Unable to deserialize creature.");
            creature.Location = location;

            return creature;
        }

        public NWItem DeserializeItem(string base64String, NWPlaceable target)
        {
            if (target == null || !target.IsValid)
            {
                throw new ArgumentException("Invalid target placeable during item deserialization.");
            }

            NWItem item = _nwnxObject.Deserialize(base64String);
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
            var result = _.CopyItem(item.Object, target.Object, TRUE);
            item.Destroy();

            return result;
        }

        public NWItem DeserializeItem(string base64String, Location targetLocation)
        {
            if (targetLocation == null)
            {
                throw new ArgumentException("Invalid target location during item deserialization.");
            }

            NWItem item = _nwnxObject.Deserialize(base64String);
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
            item.Location = targetLocation;
            
            return item;
        }

        public NWItem DeserializeItem(string base64String, NWCreature target)
        {
            if (target == null || !target.IsValid)
            {
                throw new ArgumentException("Invalid target creature during item deserialization.");
            }

            NWItem item = _nwnxObject.Deserialize(base64String);
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
            var result = _.CopyItem(item.Object, target.Object, TRUE);
            item.Destroy();

            return result;
        }


    }
}

using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class SerializationService : ISerializationService
    {
        private readonly INWScript _;
        private readonly INWNXObject _nwnxObject;

        public SerializationService(
            INWScript script,
            INWNXObject nwnxObject)
        {
            _ = script;
            _nwnxObject = nwnxObject;
        }

        public string Serialize(NWObject @object)
        {
            return _nwnxObject.Serialize(@object.Object);
        }
        
        public NWCreature DeserializeCreature(string base64String, Location location)
        {
            if (location == null) throw new ArgumentException("Invalid target location during creature deserialization.");

            NWCreature creature = NWCreature.Wrap(_nwnxObject.Deserialize(base64String));
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

            NWItem item = NWItem.Wrap(_nwnxObject.Deserialize(base64String));
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
            var result = NWItem.Wrap(_.CopyItem(item.Object, target.Object, NWScript.TRUE));
            item.Destroy();

            return result;
        }

        public NWItem DeserializeItem(string base64String, Location targetLocation)
        {
            if (targetLocation == null)
            {
                throw new ArgumentException("Invalid target location during item deserialization.");
            }

            NWItem item = NWItem.Wrap(_nwnxObject.Deserialize(base64String));
            if (item.Object == null) throw new NullReferenceException("Unable to deserialize item.");
            item.Location = targetLocation;
            
            return item;
        }



    }
}

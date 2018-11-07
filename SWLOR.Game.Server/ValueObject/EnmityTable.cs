using System;
using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class EnmityTable: Dictionary<Guid, Enmity>
    {
        public NWCreature NPCObject { get; }

        public EnmityTable(NWCreature npcObject)
        {
            NPCObject = npcObject;
        }
    }
}

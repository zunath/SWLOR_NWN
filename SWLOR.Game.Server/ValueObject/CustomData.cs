using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class CustomData: Dictionary<string, dynamic>
    {
        public NWObject Owner { get; set; }

        public CustomData()
        {
            
        }

        public CustomData(NWObject owner)
        {
            Owner = owner;
        }
    }
}

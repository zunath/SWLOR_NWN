using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.Effect
{
    public class OnEffectExpired<T>
    {
        public string[] Data { get; set; } 
        public NWObject Creator { get; set; }
        public NWObject AppliedTo { get; set; }

        public OnEffectExpired(string data, NWObject creator)
        {
            Data = data.Split(',');
            Creator = creator;
            AppliedTo = NWGameObject.OBJECT_SELF;
        }
    }
}

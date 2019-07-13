using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnFinishActionItem
    {
        public string ClassName { get; set; }
        public NWPlayer Player { get; set; }
        public NWItem Item { get; set; }
        public NWObject Target { get; set; }
        public NWLocation TargetLocation { get; set; }
        public Vector UserPosition { get; set; }
        public CustomData CustomData { get; set; }

        public OnFinishActionItem(
            string className, 
            NWPlayer player, 
            NWItem item, 
            NWObject target, 
            NWLocation targetLocation, 
            Vector userPosition, 
            CustomData customData)
        {
            ClassName = className;
            Player = player;
            Item = item;
            Target = target;
            TargetLocation = targetLocation;
            UserPosition = userPosition;
            CustomData = customData;
        }
    }
}

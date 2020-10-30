using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Area
{
    public class ExitAreaInstance: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject door = NWScript.OBJECT_SELF;

            if (!door.Area.IsInstance) return;

            NWObject target = NWScript.GetTransitionTarget(door);
            NWPlayer player = NWScript.GetClickingObject();

            NWScript.DelayCommand(6.0f, () =>
            {
                var playerCount = NWModule.Get().Players.Count(x => !Equals(x, player) && Equals(x.Area, door.Area));
                if (playerCount <= 0)
                {
                    AreaService.DestroyAreaInstance(door.Area);
                }
            });

            player.AssignCommand(() =>
            {
                NWScript.ActionJumpToLocation(target.Location);
            });

        }
    }
}

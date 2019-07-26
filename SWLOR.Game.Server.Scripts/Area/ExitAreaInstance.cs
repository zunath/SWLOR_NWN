using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
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
            NWObject door = NWGameObject.OBJECT_SELF;

            if (!door.Area.IsInstance) return;

            NWObject target = _.GetTransitionTarget(door);
            NWPlayer player = _.GetClickingObject();

            _.DelayCommand(6.0f, () =>
            {
                int playerCount = NWModule.Get().Players.Count(x => !Equals(x, player) && Equals(x.Area, door.Area));
                if (playerCount <= 0)
                {
                    AreaService.DestroyAreaInstance(door.Area);
                }
            });

            player.AssignCommand(() =>
            {
                _.ActionJumpToLocation(target.Location);
            });

        }
    }
}

using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnPhysicalAttacked: IRegisteredEvent
    {

        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return false;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return false;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnPhysicalAttacked();
            });

            return true;
        }
    }
}

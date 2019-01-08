using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Creature
{
    internal class OnCombatRoundEnd : IRegisteredEvent
    {
        private readonly IWeatherService _weather;

        public OnCombatRoundEnd(IWeatherService weather)
        {
            _weather = weather;
        }

        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;

            _weather.OnCombatRoundEnd(self);

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return false;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return false;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnCombatRoundEnd();
            });

            return true;
        }
    }
}

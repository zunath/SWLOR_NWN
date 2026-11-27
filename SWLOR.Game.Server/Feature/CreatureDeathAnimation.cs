using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    public static class CreatureDeathAnimation
    {
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnDeath()
        {
            var creature = OBJECT_SELF;
            NamedAnimation.ClearOnDeath(creature);
            UsePerkFeat.ClearQueuedAbility(creature);
            AnimationPlayer.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

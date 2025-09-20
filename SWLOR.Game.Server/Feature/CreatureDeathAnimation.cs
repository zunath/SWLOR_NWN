
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Creature;

namespace SWLOR.Game.Server.Feature
{
    public static class CreatureDeathAnimation
    {
        [ScriptHandler<OnCreatureDeathAfter>]
        public static void OnDeath()
        {
            var creature = OBJECT_SELF;
            AnimationPlayer.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

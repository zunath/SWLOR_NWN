
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Game.Server.Feature
{
    public static class CreatureDeathAnimation
    {
        [ScriptHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnDeath()
        {
            var creature = OBJECT_SELF;
            AnimationPlayer.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

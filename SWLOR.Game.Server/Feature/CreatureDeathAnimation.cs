using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Shared.Core.Event;

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

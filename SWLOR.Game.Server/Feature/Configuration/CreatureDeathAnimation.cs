using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Feature.Configuration
{
    public static class CreatureDeathAnimation
    {
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnDeath()
        {
            var creature = OBJECT_SELF;
            AnimationPlayer.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

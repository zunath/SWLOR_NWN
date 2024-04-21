using SWLOR.Core.Service;
using SWLOR.Core.Service.AnimationService;

namespace SWLOR.Core.Feature
{
    public static class CreatureDeathAnimation
    {
        [NWNEventHandler("crea_death_aft")]
        public static void OnDeath()
        {
            var creature = OBJECT_SELF;
            AnimationPlayer.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

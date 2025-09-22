using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Creature;

namespace SWLOR.Component.Combat.Feature
{
    public class CreatureDeathAnimation
    {
        private readonly IAnimationPlayerService _animationPlayerService;

        public CreatureDeathAnimation(IAnimationPlayerService animationPlayerService)
        {
            _animationPlayerService = animationPlayerService;
        }

        [ScriptHandler<OnCreatureDeathAfter>]
        public void OnDeath()
        {
            var creature = OBJECT_SELF;
            _animationPlayerService.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

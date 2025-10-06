using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Combat.Feature
{
    public class CreatureDeathAnimation
    {
        private readonly IAnimationPlayerService _animationPlayerService;

        public CreatureDeathAnimation(
            IAnimationPlayerService animationPlayerService,
            IEventAggregator eventAggregator)
        {
            _animationPlayerService = animationPlayerService;

            // Subscribe to events
            eventAggregator.Subscribe<OnCreatureDeathAfter>(e => OnDeath());
        }
        public void OnDeath()
        {
            var creature = OBJECT_SELF;
            _animationPlayerService.Play(creature, AnimationEvent.CreatureOnDeath);
        }
    }
}

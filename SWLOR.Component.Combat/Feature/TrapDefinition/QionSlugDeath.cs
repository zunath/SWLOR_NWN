using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Combat.Feature.TrapDefinition
{
    public class SpawnLarvaeOnSlugDeath
    {
        private readonly IMessagingService _messagingService;

        public SpawnLarvaeOnSlugDeath(
            IMessagingService messagingService,
            IEventAggregator eventAggregator)
        {
            _messagingService = messagingService;

            // Subscribe to events
            eventAggregator.Subscribe<OnCreatureDeathBefore>(e => CreatureDeath());
            eventAggregator.Subscribe<OnCreatureSpawnAfter>(e => MessageOnDeath());
        }
        /// <summary>
        /// When this creature dies, he'll spawn more creatures - for example, a large worm exploding into swarms of small bugs.
        /// </summary>
        public void CreatureDeath()
        {
            if (GetTag(OBJECT_SELF) != "qion_hive_slug")
                return;

            var creatureLocation = GetLocation(OBJECT_SELF);

            // Four lines create four creatures, more lines create more creatures
            CreateObject(ObjectType.Creature, "qionhivebugswarm", creatureLocation, true);
            CreateObject(ObjectType.Creature, "qionhivebugswarm", creatureLocation, true);
            CreateObject(ObjectType.Creature, "qionhivebugswarm", creatureLocation, true);
            CreateObject(ObjectType.Creature, "qion_larvae", creatureLocation, true);
        }

        /// <summary>
        /// When the creatures spawn, this will broadcasts an environmental message describing the narrative circumstances of that spawn in.
        /// It has to be ChatChannel.DMTalk - it won't work if it's ChatChannel.PlayerTalk.
        /// </summary>
        public void MessageOnDeath()
        {
            if (GetTag(OBJECT_SELF) != "qion_hive_larvae")
                return;
                
            _messagingService.SendMessageNearbyToPlayers(OBJECT_SELF, "A ravenous larvae that had been clinging onto the Qion Hive Slug dislodges itself upon its host's demise; and with it, clouds of buzzing flesh flies.", 30f);
        }
    }
}

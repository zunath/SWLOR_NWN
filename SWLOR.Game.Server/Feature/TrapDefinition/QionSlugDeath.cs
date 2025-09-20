using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature.TrapDefinition
{
    public class SpawnLarvaeOnSlugDeath
    {
        /// <summary>
        /// When this creature dies, he'll spawn more creatures - for example, a large worm exploding into swarms of small bugs.
        /// </summary>
        [ScriptHandler(ScriptName.OnCreatureDeathBefore)]
        public static void CreatureDeath()
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
        [ScriptHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void MessageOnDeath()
        {
            if (GetTag(OBJECT_SELF) != "qion_hive_larvae")
                return;
                
            Messaging.SendMessageNearbyToPlayers(OBJECT_SELF, "A ravenous larvae that had been clinging onto the Qion Hive Slug dislodges itself upon its host's demise; and with it, clouds of buzzing flesh flies.", 30f);
        }
    }
}

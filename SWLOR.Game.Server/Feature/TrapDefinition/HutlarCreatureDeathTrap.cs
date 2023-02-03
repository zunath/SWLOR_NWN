using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Feature
{
    public class SpawnCreatureOnCreatureDeath
    {
        /// <summary>
        /// When this creature dies, he'll spawn more creatures - for example, a large worm exploding into swarms of small bugs.
        /// </summary>
        [NWNEventHandler("crea_death_bef")]
        public static void CreatureDeath()
        {
            if (GetTag(OBJECT_SELF) != "qion_hive_slug")
                return;

            // Creature that will be created
            string CreateBugSwarm = "qionhivebugswarm";
            string CreateLarvae = "qion_larvae";

            // Where to make the creatures appear
            var lTarget = GetLocation(OBJECT_SELF);

            // Four lines create four creatures, more lines create more creatures
            CreateObject(ObjectType.Creature, CreateBugSwarm, lTarget, true);
            CreateObject(ObjectType.Creature, CreateBugSwarm, lTarget, true);
            CreateObject(ObjectType.Creature, CreateBugSwarm, lTarget, true);
            CreateObject(ObjectType.Creature, CreateLarvae, lTarget, true);
        }
    }
    public class MessageOnCreatureSpawn
    {
        /// <summary>
        /// When the creatures spawn, this will broadcasts an environmental message describing the narrative circumstances of that spawn in.
        /// It has to be ChatChannel.DMTalk - it won't work if it's ChatChannel.PlayerTalk.
        /// </summary>
        [NWNEventHandler("crea_spawn_aft")]
        public static void MessageOnDeath()
        {
            if (GetTag(OBJECT_SELF) != "qion_hive_larvae")
                return;

            uint sender = OBJECT_SELF;
            var range = 30f;
            int nth = 1;

            string MessageOnCreatureSpawn = "A ravenous larvae that had been clinging onto the Qion Hive Slug dislodges itself upon its host's demise; and with it, clouds of buzzing flesh flies.";

            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(sender, nearby) <= range)
            {
                ChatPlugin.SendMessage(Core.NWNX.Enum.ChatChannel.DMTalk, MessageOnCreatureSpawn, sender, nearby);
                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            }
        }
    }
}
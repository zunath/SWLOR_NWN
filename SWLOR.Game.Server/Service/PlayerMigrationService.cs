using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.NWNX;

using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerMigrationService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
        }

        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = DataService.Get<Player>(player.GlobalID);

            // VERSION 2: Background items are no longer plot because item level no longer dictates your skill XP gain.
            if (dbPlayer.VersionNumber < 2) 
            {
                string[] resrefs =
                {
                    "blaster_s",
                    "rifle_s",
                    "powerglove_t",
                    "baton_s",
                    "doubleaxe_z",
                    "kukri_d",
                    "greatsword_s",
                    "scanner_r_h",
                    "harvest_r_h",
                    "man_armor"
                };

                foreach (var resref in resrefs)
                {
                    NWItem item = _.GetItemPossessedBy(player, resref);
                    if (item.IsValid)
                    {
                        item.IsPlot = false;
                    }
                }

                dbPlayer.VersionNumber = 2;
            }

            // VERSION 3: Force feats need to be removed since force powers were reworked.
            if (dbPlayer.VersionNumber < 3)
            {
                // These IDs come from the Feat.2da file.
                NWNXCreature.RemoveFeat(player, 1135); // Force Breach
                NWNXCreature.RemoveFeat(player, 1136); // Force Lightning
                NWNXCreature.RemoveFeat(player, 1137); // Force Heal
                NWNXCreature.RemoveFeat(player, 1138); // Dark Heal
                NWNXCreature.RemoveFeat(player, 1143); // Force Spread
                NWNXCreature.RemoveFeat(player, 1144); // Dark Spread
                NWNXCreature.RemoveFeat(player, 1145); // Force Push
                NWNXCreature.RemoveFeat(player, 1125); // Force Aura
                NWNXCreature.RemoveFeat(player, 1152); // Drain Life
                NWNXCreature.RemoveFeat(player, 1134); // Chainspell

                dbPlayer.VersionNumber = 3;
            }

            // VERSION 4: Give the Uncanny Dodge 1 feat to all characters.
            if (dbPlayer.VersionNumber < 4)
            {
                NWNXCreature.AddFeatByLevel(player, FEAT_UNCANNY_DODGE_1, 1);
                dbPlayer.VersionNumber = 4;
            }

            // VERSION 5: We're doing another Force rework, so remove any force feats the player may have acquired.
            if (dbPlayer.VersionNumber < 5)
            {
                NWNXCreature.RemoveFeat(player, 1135); // Force Breach
                NWNXCreature.RemoveFeat(player, 1136); // Force Lightning
                NWNXCreature.RemoveFeat(player, 1137); // Force Heal I
                NWNXCreature.RemoveFeat(player, 1140); // Absorption Field
                NWNXCreature.RemoveFeat(player, 1143); // Force Spread
                NWNXCreature.RemoveFeat(player, 1145); // Force Push
                NWNXCreature.RemoveFeat(player, 1125); // Force Aura
                NWNXCreature.RemoveFeat(player, 1152); // Drain Life
                NWNXCreature.RemoveFeat(player, 1134); // Chainspell
                NWNXCreature.RemoveFeat(player, 1162); // Force Heal II
                NWNXCreature.RemoveFeat(player, 1163); // Force Heal III
                NWNXCreature.RemoveFeat(player, 1164); // Force Heal IV

                dbPlayer.VersionNumber = 5;
            }

            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }

        

    }
}

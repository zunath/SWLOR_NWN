using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class PlayerMigrationService : IPlayerMigrationService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly INWNXCreature _nwnxCreature;

        public PlayerMigrationService(
            INWScript script,
            IDataService data,
            INWNXCreature nwnxCreature)
        {
            _ = script;
            _data = data;
            _nwnxCreature = nwnxCreature;
        }

        public void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = _data.Get<Player>(player.GlobalID);

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
                _nwnxCreature.RemoveFeat(player, 1135); // Force Breach
                _nwnxCreature.RemoveFeat(player, 1136); // Force Lightning
                _nwnxCreature.RemoveFeat(player, 1137); // Force Heal
                _nwnxCreature.RemoveFeat(player, 1138); // Dark Heal
                _nwnxCreature.RemoveFeat(player, 1143); // Force Spread
                _nwnxCreature.RemoveFeat(player, 1144); // Dark Spread
                _nwnxCreature.RemoveFeat(player, 1145); // Force Push
                _nwnxCreature.RemoveFeat(player, 1125); // Force Aura
                _nwnxCreature.RemoveFeat(player, 1152); // Drain Life
                _nwnxCreature.RemoveFeat(player, 1134); // Chainspell

                dbPlayer.VersionNumber = 3;
            }

            // VERSION 4: Give the Uncanny Dodge 1 feat to all characters.
            if (dbPlayer.VersionNumber < 4)
            {
                _nwnxCreature.AddFeatByLevel(player, FEAT_UNCANNY_DODGE_1, 1);
                dbPlayer.VersionNumber = 4;
            }

            _data.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }

        

    }
}

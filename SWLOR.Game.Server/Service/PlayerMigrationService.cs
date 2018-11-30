using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class PlayerMigrationService : IPlayerMigrationService
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public PlayerMigrationService(
            INWScript script,
            IDataService data)
        {
            _ = script;
            _data = data;
        }

        public void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = _data.Get<Player>(player.GlobalID);

            // Background items are no longer plot because item level no longer dictates your skill XP gain.
            if (dbPlayer.VersionNumber <= 1) 
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

            _data.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }
    }
}

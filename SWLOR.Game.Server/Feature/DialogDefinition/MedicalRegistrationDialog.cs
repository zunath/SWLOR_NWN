using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class MedicalRegistrationDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = "If you die, you will return to the last medical facility you registered at. Would you like to register to this medical facility?";

            page.AddResponse("Register", () =>
            {
                var player = GetPC();
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                var position = GetPosition(player);
                var orientation = GetFacing(player);
                var areaResref = GetResRef(GetArea(player));

                dbPlayer.RespawnAreaResref = areaResref;
                dbPlayer.RespawnLocationOrientation = orientation;
                dbPlayer.RespawnLocationX = position.X;
                dbPlayer.RespawnLocationY = position.Y;
                dbPlayer.RespawnLocationZ = position.Z;

                DB.Set(playerId, dbPlayer);

                FloatingTextStringOnCreature("You will return to this location the next time you die.", player, false);
            });
        }
    }
}

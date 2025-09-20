using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class MedicalRegistrationDialog: DialogBase
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            page.Header = "In the event you suffer a critical injury you will return to your registered medical facility. Would you like to register to this medical facility?";

            page.AddResponse("Register", () =>
            {
                if (!GetIsPC(player) || GetIsDM(player)) return;

                var playerId = GetObjectUUID(player);
                var dbPlayer = _db.Get<Player>(playerId);

                var position = GetPosition(player);
                var orientation = GetFacing(player);
                var areaResref = GetResRef(GetArea(player));

                dbPlayer.RespawnAreaResref = areaResref;
                dbPlayer.RespawnLocationOrientation = orientation;
                dbPlayer.RespawnLocationX = position.X;
                dbPlayer.RespawnLocationY = position.Y;
                dbPlayer.RespawnLocationZ = position.Z;

                _db.Set(dbPlayer);

                FloatingTextStringOnCreature("You will return to this location the next time you die.", player, false);
            });
        }
    }
}

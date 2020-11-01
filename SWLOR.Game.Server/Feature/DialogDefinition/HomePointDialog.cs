using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class HomePointDialog: DialogBase
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
            var player = GetPC();
            page.Header = ColorToken.Green("Home Point") + "\n\n" +
                "You can set your home point to this location. If you should die, you will return to your last home point.";

            page.AddResponse("Set Home Point", () =>
            {
                if (!GetIsPC(player) || GetIsDM(player)) return;

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                var position = GetPosition(OBJECT_SELF);
                var area = GetArea(OBJECT_SELF);
                var areaResref = GetResRef(area);
                var orientation = GetFacing(OBJECT_SELF);

                dbPlayer.RespawnLocationX = position.X;
                dbPlayer.RespawnLocationY = position.Y;
                dbPlayer.RespawnLocationZ = position.Z;
                dbPlayer.RespawnAreaResref = areaResref;
                dbPlayer.RespawnLocationOrientation = orientation;

                FloatingTextStringOnCreature("Home point set!", player, false);

                EndConversation();
            });
        }
    }
}

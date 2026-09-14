using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class MedicalRegistrationDialog: ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithPortrait("p_256x128_medic1")
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            page.Header = "In the event you suffer a critical injury you will return to your registered medical facility. Would you like to register to this medical facility?";

            page.AddResponse("Register", () =>
            {
                if (!GetIsPC(player) || GetIsDM(player)) return;

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

                DB.Set(dbPlayer);

                FloatingTextStringOnCreature("You will return to this location the next time you die.", player, false);
            });
        }
    }
}

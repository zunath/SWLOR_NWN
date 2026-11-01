using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.PropertyService;

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
                var area = GetArea(player);

                // A player city's (or station's) Medical Center is an instance: register it by its property, and keep the
                // last world registration as the fallback for when the building is gone.
                var propertyId = Property.GetPropertyId(area);
                var property = string.IsNullOrWhiteSpace(propertyId) ? null : DB.Get<WorldProperty>(propertyId);
                if (property?.PropertyType == PropertyType.MedicalCenter)
                {
                    // Only a player station's Medical Center takes registrations; planet cities' do not.
                    if (!Station.IsStationInterior(property.Id))
                    {
                        SendMessageToPC(player, "This medical center does not take registrations.");
                        return;
                    }

                    dbPlayer.RespawnPropertyId = property.Id;
                    DB.Set(dbPlayer);
                    FloatingTextStringOnCreature("You will return to this location the next time you die.", player, false);
                    return;
                }

                dbPlayer.RespawnPropertyId = string.Empty;
                dbPlayer.RespawnAreaResref = GetResRef(area);
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

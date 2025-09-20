using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.TaxiService;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class TaxiTerminalDialog: DialogBase
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
            page.Header = BuildHeader();

            var player = GetPC();
            var regionId = GetLocalInt(OBJECT_SELF, "TAXI_REGION_ID");
            var destinations = Taxi.GetDestinationsByRegionId(regionId);
            var destinationId = GetLocalInt(OBJECT_SELF, "TAXI_DESTINATION_ID");
            var destinationType = (TaxiDestinationType) destinationId;

            // DMs or Non-Players: Display all destinations (no registration required)
            if (!GetIsPC(player) || GetIsDM(player))
            {
                foreach (var destination in destinations)
                {
                    page.AddResponse(ColorToken.Green($"{destination.Value.Name}"), () =>
                    {
                        var location = GetLocation(GetWaypointByTag(destination.Value.WaypointTag));
                        AssignCommand(player, () =>
                        {
                            ClearAllActions();
                            ActionJumpToLocation(location);
                        });
                    });
                }
            }
            // Players: Only display destinations the player has unlocked.
            else
            {
                // Player must have the 'Taxi Hailing Device' key item.
                if (!KeyItem.HasKeyItem(player, KeyItemType.TaxiHailingDevice))
                {
                    return;
                }

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                // Register Location option
                if (!dbPlayer.TaxiDestinations.ContainsKey(regionId) ||
                    !dbPlayer.TaxiDestinations[regionId].Contains(destinationType))
                {
                    page.AddResponse(ColorToken.Green("[REGISTER LOCATION]"), () =>
                    {
                        Taxi.RegisterTaxiDestination(player, destinationType);
                    });
                }

                foreach (var destination in destinations)
                {
                    // Skip the destination if this is the current location.
                    if (destination.Key == destinationType)
                        continue;

                    // Player has unlocked this location.
                    if (dbPlayer.TaxiDestinations.ContainsKey(regionId) &&
                        dbPlayer.TaxiDestinations[regionId].Contains(destination.Key))
                    {
                        page.AddResponse(ColorToken.Green(destination.Value.Name + $" [{destination.Value.Price} Credits]"), () =>
                        {
                            if (GetGold(player) < destination.Value.Price)
                            {
                                FloatingTextStringOnCreature( "Insufficient credits!", player, false);
                                return;
                            }

                            var location = GetLocation(GetWaypointByTag(destination.Value.WaypointTag));
                            AssignCommand(player, () =>
                            {
                                ClearAllActions();
                                ActionJumpToLocation(location);
                            });

                            TakeGoldFromCreature(destination.Value.Price, player, true);
                        });
                    }
                    // Player has NOT unlocked this location.
                    else
                    {
                        page.AddResponse(ColorToken.Red(destination.Value.Name + " [UNREGISTERED]"), () =>
                        {
                            SendMessageToPC(player, "This location has not been registered yet.");
                        });
                    }
                }
            }
        }

        private string BuildHeader()
        {
            var player = GetPC();

            if (KeyItem.HasKeyItem(player, KeyItemType.TaxiHailingDevice))
            {
                return $"Your 'Taxi Hailing Device' may be used to summon a taxi to transport you throughout the region. Only destinations you have registered are available for transportation.\n\n" +
                       "Where would you like to go?";
            }
            else
            {
                return $"A 'Taxi Hailing Device' is required to request transportation. Please obtain one and return here.";
            }

        }
    }
}

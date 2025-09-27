using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.Dialog
{
    public class TaxiTerminalDialog: DialogBase
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static readonly ITaxiService _taxiService = ServiceContainer.GetService<ITaxiService>();
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _serviceProvider.GetRequiredService<IKeyItemService>();
        
        private const string MainPageId = "MAIN_PAGE";

        public TaxiTerminalDialog(IServiceProvider serviceProvider, IDialogService dialogService, IDialogBuilder dialogBuilder) : base(dialogService, dialogBuilder)
        {
            _serviceProvider = serviceProvider;
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = BuildHeader();

            var player = GetPC();
            var regionId = GetLocalInt(OBJECT_SELF, "TAXI_REGION_ID");
            var destinations = _taxiService.GetDestinationsByRegionId(regionId);
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
                if (!KeyItemService.HasKeyItem(player, KeyItemType.TaxiHailingDevice))
                {
                    return;
                }

                var playerId = GetObjectUUID(player);
                var dbPlayer = _db.Get<Player>(playerId);

                // Register Location option
                if (!dbPlayer.TaxiDestinations.ContainsKey(regionId) ||
                    !dbPlayer.TaxiDestinations[regionId].Contains(destinationType))
                {
                    page.AddResponse(ColorToken.Green("[REGISTER LOCATION]"), () =>
                    {
                        _taxiService.RegisterTaxiDestination(player, destinationType);
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

            if (KeyItemService.HasKeyItem(player, KeyItemType.TaxiHailingDevice))
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

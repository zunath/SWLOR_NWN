using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// Founding a player station with a Station Core at an anchor site (plan section 25), the space version of
    /// <see cref="PlaceCityHallDialog"/>: the same terms, price and responsibilities as a planet city.
    /// </summary>
    public class PlaceStationCoreDialog: ConversationMenuDefinition
    {
        public const string ItemVariable = "PROPERTY_STATION_CORE_ITEM";
        public const string AnchorVariable = "PROPERTY_STATION_CORE_ANCHOR";

        private class Model
        {
            public bool IsConfirming { get; set; }
            public uint Item { get; set; }
            public string AnchorId { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            var player = Player;
            var model = Data<Model>();
            model.Item = GetLocalObject(player, ItemVariable);
            model.AnchorId = GetLocalString(player, AnchorVariable);

            DeleteLocalObject(player, ItemVariable);
            DeleteLocalString(player, AnchorVariable);
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var anchor = StationRules.GetAnchor(model.AnchorId);
            if (anchor == null)
            {
                page.Header = "There is no station anchor site here.";
                return;
            }

            page.Header = $"Deploying the Station Core will found a player station at this anchor site in {anchor.SystemName}. Please review the following details about the station.\n\n" +
                          $"Initial Price: {StationTuning.InitialPrice} credits\n" +
                          $"Price Per Day: {StationTuning.PricePerDay} credits\n" +
                          $"Structure Limit: {StationTuning.StructureLimit}\n" +
                          $"Building Limit: {StationTuning.BuildingLimit}\n" +
                          $"Shuttle and Trade Exchange: {Planet.GetPlanetByType(anchor.HomePlanet).Name}\n\n" +
                          "The Core builds the station deck with the Station Core, which serves as its City Hall, and its first Starport. Dock at the station through its docking point to go aboard.\n\n" +
                          "WARNING: Founding this station will make you its mayor. Every three weeks (real world time) an election will be held to vote for the new mayor. You will be required to participate or you will lose your position as Mayor.\n\n" +
                          "Additionally, you will be required to pay upkeep on all structures placed. Failure to do so will result in decay of the structures. Failing to pay upkeep for four weeks (real world time) will result in the loss of the station.\n\n" +
                          $"A minimum of {Property.GetCitizensRequiredForNextCityLevel(1)} citizens must be registered in your station within {StationTuning.GraceHours} hours or it will be removed upon the next reset. If you ever fall below this amount you will have {StationTuning.GraceHours} hours from the time of the next server reset to rectify it. Failure to do so will result in the loss of your station, structures, items, etc.\n\n" +
                          "Credits paid cannot be refunded. Are you sure you want to found a station here?";

            if (!model.IsConfirming)
            {
                page.AddResponse("Deploy Station Core", () =>
                {
                    model.IsConfirming = true;
                });
                return;
            }

            page.AddResponse("CONFIRM DEPLOY STATION CORE", () =>
            {
                var area = GetArea(player);
                if (!GetIsObjectValid(model.Item) || GetItemPossessor(model.Item) != player)
                {
                    FloatingTextStringOnCreature("The Station Core is no longer in your inventory.", player, false);
                    return;
                }

                // Check everything again: another pilot may have founded a station here, or the player may have left.
                var request = Station.BuildFoundingRequest(player, area, Vector3(anchor.X, anchor.Y, 0f));
                var refusal = GetResRef(area) != anchor.AreaResref
                    ? StationFoundingRefusal.NoAnchorInRange
                    : StationRules.GetFoundingRefusal(request);
                if (refusal != StationFoundingRefusal.None)
                {
                    FloatingTextStringOnCreature(StationRules.DescribeFoundingRefusal(refusal), player, false);
                    return;
                }

                AssignCommand(player, () => TakeGoldFromCreature(StationTuning.InitialPrice, player, true));
                Property.CreateStation(player, area, model.Item, anchor);

                Close();
            });
        }
    }
}

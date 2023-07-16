using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PlaceCityHallDialog: DialogBase
    {
        private class Model
        {
            public bool IsConfirming { get; set; }

            public uint Item { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            model.Item = GetLocalObject(player, "PROPERTY_CITY_HALL_ITEM");
            model.X = GetLocalFloat(player, "PROPERTY_CITY_HALL_X");
            model.Y = GetLocalFloat(player, "PROPERTY_CITY_HALL_Y");
            model.Z = GetLocalFloat(player, "PROPERTY_CITY_HALL_Z");

            DeleteLocalFloat(player, "PROPERTY_CITY_HALL_X");
            DeleteLocalFloat(player, "PROPERTY_CITY_HALL_Y");
            DeleteLocalFloat(player, "PROPERTY_CITY_HALL_Z");
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var area = GetArea(player);
            var propertyId = Property.GetPropertyId(area);
            var model = GetDataModel<Model>();

            if (!string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
            {
                page.Header = "You are already a citizen of another city. Revoke your citizenship first and try again.";
            }
            else if (!string.IsNullOrWhiteSpace(propertyId))
            {
                page.Header = "This area has already been settled.";
            }
            else
            {
                var city = Property.GetLayoutByType(PropertyLayoutType.City);
                page.Header = "Placing a City Hall structure will claim the area to be used as a city. Please review the following details about this land.\n\n" +
                              $"{ColorToken.Green("Initial Price:")} {city.InitialPrice} credits\n" +
                              $"{ColorToken.Green("Price Per Day:")} {city.PricePerDay} credits\n" +
                              $"{ColorToken.Green("Structure Limit:")} {city.StructureLimit}\n" +
                              $"{ColorToken.Green("Building Limit:")} {city.BuildingLimit}\n\n" +
                              $"{ColorToken.Red("WARNING:")} Placing this city hall will make you mayor of this land. Every three weeks (real world time) an election will be held to vote for the new mayor. You will be required to participate or you will lose your position as Mayor.\n\n" +
                              "Additionally, you will be required to pay upkeep on all structures placed. Failure to do so will result in decay of the structures. Failing to pay upkeep for four weeks (real world time) will result in the loss of the city.\n\n" +
                              $"A minimum of {Property.GetCitizensRequiredForNextCityLevel(1)} citizens must be registered in your city within 18 hours or it will be removed upon the next reset. If you ever fall below this amount you will have 18 hours from the time of the next server reset to rectify it. Failure to do so will result in the loss of your city, structures, items, etc.\n\n" +
                              "Credits paid cannot be refunded. Are you sure you want to claim this area and establish a city?";

                if (model.IsConfirming)
                {
                    page.AddResponse("CONFIRM PLACE CITY HALL", () =>
                    {
                        // Ensure they have enough money.
                        var gold = GetGold(player);
                        if (gold < city.InitialPrice)
                        {
                            FloatingTextStringOnCreature("Insufficient credits!", player, false);
                            return;
                        }

                        // Ensure someone else didn't claim the area before them.
                        propertyId = Property.GetPropertyId(area);
                        if (!string.IsNullOrWhiteSpace(propertyId))
                        {
                            FloatingTextStringOnCreature("This area has already been claimed.", player, false);
                            return;
                        }

                        if (Perk.GetPerkLevel(player, PerkType.CityManagement) < 1)
                        {
                            FloatingTextStringOnCreature("The City Management I perk is required to establish a city.", player, false);
                            return;
                        }

                        AssignCommand(player, () => TakeGoldFromCreature(city.InitialPrice, player, true));

                        var position = Vector3(model.X, model.Y, model.Z);
                        var location = Location(area, position, 0.0f);

                        Property.CreateCity(player, area, model.Item, location);

                        EndConversation();
                    });
                }
                else
                {
                    page.AddResponse("Place City Hall", () =>
                    {
                        model.IsConfirming = true;
                    });
                }
            }
        }
    }
}

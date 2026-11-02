using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// A corral's menu (Player City Revamp, "Farming: Livestock"): who stands in it, placing a young or crated animal from
    /// your inventory, and, through <see cref="FarmhandDialog"/>'s pages, the corral's farmhand. Animals are tended by talking to
    /// them (<see cref="LivestockDialog"/>).
    /// </summary>
    public class CorralDialog : FarmhandDialog
    {
        private const string CorralPageId = "CORRAL_PAGE";
        private const string PlacePageId = "PLACE_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new FarmModel())
                .AddInitializationAction(Initialize)
                .AddPage(CorralPageId, CorralPageInit)
                .AddPage(PlacePageId, PlacePageInit);
            AddFarmhandPages(builder);
            return builder.Build();
        }

        private void CorralPageInit(ConversationMenuPage page)
        {
            var corral = Corrals.GetCorral(Data<FarmModel>().TargetId);
            if (corral == null)
            {
                page.Header = "This corral is no longer here.";
                return;
            }

            var now = DateTime.UtcNow;
            var animals = Corrals.GetAnimalsInCorral(corral.Id);
            var city = DB.Get<Entity.WorldProperty>(corral.ParentPropertyId);
            var cityLevel = city == null ? 1 : CityOfficers.GetCityLevel(city);
            var lines = new List<string>
            {
                $"Corral ({animals.Count} / {LivestockRules.AnimalsPerCorral} animals)",
                $"Your CD key has {Corrals.CountPlaced(GetPCPublicCDKey(Player), GetObjectUUID(Player))} / {LivestockRules.MaxAnimalsPerCdKey} animals placed.",
            };

            if (!LivestockRules.IsCorralOpen(Corrals.GetCorralIndex(corral), cityLevel))
                lines.Add("This corral is over the city's limit of " + LivestockRules.GetMaxCorrals(cityLevel) + " corrals, so it takes no new animals. The animals in it stay.");

            foreach (var animal in animals)
            {
                var definition = Corrals.GetDefinition(animal.Species);
                var owner = animal.OwnerPlayerId == GetObjectUUID(Player)
                    ? "yours"
                    : PlayerName.GetPlainDisplayNameByPlayerId(Player, animal.OwnerPlayerId, DB.Get<Entity.Player>(animal.OwnerPlayerId)?.Name);
                lines.Add($"- {definition?.Name ?? animal.Species.ToString()}, {LivestockRules.DescribeStage(LivestockRules.GetStage(animal, now)).ToLowerInvariant()} ({owner})");
            }

            lines.Add("Talk to an animal to feed it, collect from it, butcher it or crate it.");
            page.Header = string.Join("\n", lines);

            page.AddResponse("Place an animal", () => GoToPage(PlacePageId));
            page.AddResponse("Farmhand", () => GoToPage(FarmhandPageId));
        }

        private void PlacePageInit(ConversationMenuPage page)
        {
            var corralId = Data<FarmModel>().TargetId;
            var animals = new List<uint>();
            for (var item = GetFirstItemInInventory(Player); GetIsObjectValid(item); item = GetNextItemInInventory(Player))
            {
                var resref = GetResRef(item);
                if (resref == LivestockRules.CrateResref || Corrals.GetDefinitionByYoungResref(resref) != null)
                    animals.Add(item);
            }

            if (animals.Count == 0)
            {
                page.Header = "You carry no young or crated animal. The beast trader sells young animals.";
                return;
            }

            page.Header = "Place which animal? Crop Management sets the highest tier you can raise, and a CD key can have " +
                          LivestockRules.MaxAnimalsPerCdKey + " animals placed.";
            foreach (var animalItem in animals)
            {
                var item = animalItem;
                var label = GetResRef(item) == LivestockRules.CrateResref ? GetName(item) : $"Young {GetName(item)}";
                page.AddResponse(label, () =>
                {
                    var result = Corrals.Place(Player, corralId, item);
                    SendMessageToPC(Player, result.Succeeded ? ColorToken.Green(result.Message) : ColorToken.Red(result.Message));
                    GoToPage(CorralPageId, false);
                });
            }
        }
    }
}

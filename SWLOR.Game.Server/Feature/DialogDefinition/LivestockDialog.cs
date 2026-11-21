using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The conversation on a placed animal (Player City Revamp, "Farming: Livestock"): its stage and timers, and for its owner
    /// Feed (a crop picker), Collect, Butcher and Crate. <see cref="Corrals"/> holds the rules and the records.
    /// </summary>
    public class LivestockDialog : ConversationMenuDefinition
    {
        private class Model
        {
            public string AnimalId { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string FeedPageId = "FEED_PAGE";
        private const string ButcherPageId = "BUTCHER_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(() => Data<Model>().AnimalId = GetLocalString(Owner, Corrals.AnimalIdVariable))
                .AddPage(MainPageId, MainPageInit)
                .AddPage(FeedPageId, FeedPageInit)
                .AddPage(ButcherPageId, ButcherPageInit);

            return builder.Build();
        }

        private bool TryGetAnimal(ConversationMenuPage page, out Livestock animal, out LivestockDefinition definition)
        {
            animal = Corrals.LoadAnimal(Data<Model>().AnimalId, DateTime.UtcNow);
            definition = animal == null ? null : Corrals.GetDefinition(animal.Species);
            if (animal != null && definition != null)
                return true;

            page.Header = "This animal is gone.";
            return false;
        }

        private bool IsOwner(Livestock animal) => animal.OwnerPlayerId == GetObjectUUID(Player);

        private void Report(LivestockResult result)
        {
            SendMessageToPC(Player, result.Succeeded ? ColorToken.Green(result.Message) : ColorToken.Red(result.Message));
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            if (!TryGetAnimal(page, out var animal, out var definition))
                return;

            var now = DateTime.UtcNow;
            var owner = IsOwner(animal)
                ? "you"
                : PlayerName.GetPlainDisplayNameByPlayerId(Player, animal.OwnerPlayerId, DB.Get<Player>(animal.OwnerPlayerId)?.Name);

            var lines = new[]
            {
                $"{definition.Name} (tier {definition.Tier})",
                $"Owner: {owner}",
                $"Stage: {LivestockRules.DescribeStage(LivestockRules.GetStage(animal, now))}, fed {animal.Feedings} {(animal.Feedings == 1 ? "time" : "times")}",
                LivestockRules.DescribeGrowth(animal, now),
                LivestockRules.DescribeHunger(animal, now),
                DescribeFeeding(animal, now),
                DescribeProducts(animal, now),
            };
            page.Header = string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));

            if (!IsOwner(animal))
                return;

            page.AddResponse("Feed", () => GoToPage(FeedPageId));
            page.AddResponse("Collect", () => Act(Corrals.Collect(Player, animal.Id)));
            page.AddResponse("Butcher", () => GoToPage(ButcherPageId));
            page.AddResponse("Crate", () =>
            {
                var result = Corrals.Crate(Player, animal.Id);
                Report(result);
                if (result.Succeeded)
                    Close();
                else
                    GoToPage(MainPageId, false);
            });
        }

        private void Act(LivestockResult result)
        {
            Report(result);
            GoToPage(MainPageId, false);
        }

        private static string DescribeFeeding(Livestock animal, DateTime now)
        {
            return LivestockRules.CanFeedAt(animal, now)
                ? $"It can be fed now ({LivestockRules.FeedCrops} crops of its tier or lower)."
                : $"It can be fed again in {FarmingRules.FormatDuration(LivestockRules.GetNextFeedUtc(animal)!.Value - now)}.";
        }

        private static string DescribeProducts(Livestock animal, DateTime now)
        {
            if (!LivestockRules.IsAdult(animal, now))
                return string.Empty;
            if (LivestockRules.HasPenalty(animal, now))
                return $"Predators raided the corrals: no products for {FarmingRules.FormatDuration(animal.NoProductsUntilUtc!.Value - now)}.";
            if (!LivestockRules.IsFedRecently(animal, now))
                return "It gives nothing unless it was fed in the last 24 hours.";
            if (LivestockRules.GetNextCollectUtc(animal) is { } next && now < next)
                return $"It can be collected from again in {FarmingRules.FormatDuration(next - now)}.";
            return "It is ready to be collected from.";
        }

        private void FeedPageInit(ConversationMenuPage page)
        {
            if (!TryGetAnimal(page, out var animal, out var definition))
                return;

            var crops = Farming.GetAllCrops()
                .Where(crop => LivestockRules.CanEat(definition.Tier, crop.Tier))
                .Select(crop => (Crop: crop, Count: FarmItems.Count(Player, crop.HarvestResref)))
                .Where(entry => entry.Count >= LivestockRules.FeedCrops)
                .OrderBy(entry => entry.Crop.Tier)
                .ThenBy(entry => entry.Crop.Name)
                .ToList();

            if (crops.Count == 0)
            {
                page.Header = $"You carry no crop of tier {definition.Tier} or lower, {LivestockRules.FeedCrops} at a time, to feed it.";
                return;
            }

            page.Header = $"Feed the {definition.Name}. A feeding takes {LivestockRules.FeedCrops} crops of its tier or lower. Which crop?";
            foreach (var (crop, count) in crops)
            {
                var type = crop.Type;
                page.AddResponse($"{crop.Name} (tier {crop.Tier}) x{count}", () =>
                {
                    Report(Corrals.Feed(Player, animal.Id, type));
                    GoToPage(MainPageId, false);
                });
            }
        }

        private void ButcherPageInit(ConversationMenuPage page)
        {
            if (!TryGetAnimal(page, out var animal, out var definition))
                return;

            var items = LivestockRules.GetButcherItems(definition, Perk.GetPlayerEffectivePerkLevel(Player, PerkType.BountifulHarvest));
            page.Header = $"Butcher the {definition.Name}? It is gone for good, and you get " +
                          string.Join(", ", items.Select(item => $"{item.Count} {Cache.GetItemNameByResref(item.Resref) ?? item.Resref}")) + ".";
            page.AddResponse("Butcher it", () =>
            {
                var result = Corrals.Butcher(Player, animal.Id);
                Report(result);
                if (result.Succeeded)
                    Close();
                else
                    GoToPage(MainPageId, false);
            });
            page.AddResponse("Keep it", () => GoToPage(MainPageId, false));
        }
    }
}

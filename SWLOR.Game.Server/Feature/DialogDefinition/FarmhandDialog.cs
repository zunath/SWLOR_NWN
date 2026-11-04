using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// A farmhand's post (Player City Revamp, "Farmhands"): assign one of your industrial droids to a house of yours or to a
    /// corral, load its feed hopper, empty its output hopper and recall it. In a house it tends your crops; at a corral it
    /// feeds and collects from your animals. <see cref="CorralDialog"/> adds the farmhand pages to a corral's menu.
    /// </summary>
    public class FarmhandDialog : ConversationMenuDefinition
    {
        protected class FarmModel
        {
            public string TargetId { get; set; }
        }

        protected const string FarmhandPageId = "FARMHAND_PAGE";
        private const string PickPageId = "FARMHAND_PICK_PAGE";
        private const string FeedPageId = "FARMHAND_FEED_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new FarmModel())
                .AddInitializationAction(Initialize);
            AddFarmhandPages(builder);
            return builder.Build();
        }

        protected void AddFarmhandPages(ConversationMenuBuilder builder)
        {
            builder
                .AddPage(FarmhandPageId, FarmhandPageInit)
                .AddPage(PickPageId, PickPageInit)
                .AddPage(FeedPageId, FeedPageInit);
        }

        /// <summary>
        /// The post is the corral the menu was opened on, or else the house the player stands in.
        /// </summary>
        protected void Initialize()
        {
            var fromOwner = GetIsObjectValid(Owner) && GetObjectType(Owner) == ObjectType.Placeable ? Property.GetPropertyId(Owner) : string.Empty;
            var fromArea = Property.GetPropertyId(GetArea(Player));
            Data<FarmModel>().TargetId = Farmhands.GetTarget(fromOwner) != null ? fromOwner
                : Farmhands.GetTarget(fromArea) != null ? fromArea
                : string.Empty;
        }

        private void Report(FarmhandRefusal refusal)
        {
            if (refusal != FarmhandRefusal.None)
                SendMessageToPC(Player, ColorToken.Red(FarmhandRules.Describe(refusal)));
        }

        private void FarmhandPageInit(ConversationMenuPage page)
        {
            var targetId = Data<FarmModel>().TargetId;
            var target = Farmhands.GetTarget(targetId);
            if (target == null)
            {
                page.Header = "Stand inside one of your houses, or beside a corral, to manage a farmhand.";
                return;
            }

            Farmhands.CatchUp(targetId);
            var farmhand = Farmhands.Get(targetId);
            var isHouse = Farmhands.GetTargetType(target) == FarmhandTargetType.House;
            var place = isHouse ? $"your house {target.CustomName}" : "this corral";

            if (farmhand == null)
            {
                page.Header = $"No droid works {place}.\n\n" +
                              (isHouse
                                  ? "A farmhand tends your crops in this house's garden plots the moment each is ready, even while you are away."
                                  : "A farmhand feeds and collects from your animals in this corral while you are away. You load its feed hopper, and what it collects waits in its output hopper.") +
                              "\nThe droid's tier sets the highest crop and animal tier it handles.";
                if (Farmhands.HasAccess(Player, target))
                    page.AddResponse("Assign one of your droids", () => GoToPage(PickPageId));
                return;
            }

            var isOwner = farmhand.OwnerPlayerId == GetObjectUUID(Player);
            var header = $"{farmhand.DroidName} works {place}.\nTier: {farmhand.DroidTier} (it handles crops and animals of tier 1 to {farmhand.DroidTier})\n";
            if (isHouse)
            {
                header += "It tends its owner's crops here as they become ready. Tending adds 1 to the harvest and earns no XP. It never harvests, so a crop still withers 72 hours after it ripens.";
            }
            else
            {
                header += $"Feed hopper: {FarmhandRules.CountFeed(farmhand.FeedHopper)} / {FarmhandRules.HopperCapacity} crops{DescribeFeed(farmhand)}\n" +
                          $"Output hopper: {FarmhandRules.CountOutput(farmhand.OutputHopper)} / {FarmhandRules.HopperCapacity} items{DescribeOutput(farmhand)}\n" +
                          "It feeds each animal of its owner's as soon as the animal may eat while the feed lasts, and collects while the output hopper has room.";
            }

            page.Header = header;

            if (isOwner && !isHouse)
            {
                page.AddResponse("Load feed", () => GoToPage(FeedPageId));
                page.AddResponse("Take the feed back", () =>
                {
                    Report(Farmhands.TakeFeedBack(Player, targetId));
                    GoToPage(FarmhandPageId, false);
                });
                page.AddResponse("Empty the output hopper", () =>
                {
                    Report(Farmhands.EmptyOutput(Player, targetId));
                    GoToPage(FarmhandPageId, false);
                });
            }

            if (isOwner || Farmhands.CanManage(Player, target))
            {
                page.AddResponse($"Recall {farmhand.DroidName}", () =>
                {
                    Report(Farmhands.RecallDroid(Player, targetId));
                    GoToPage(FarmhandPageId, false);
                });
            }
        }

        private static string DescribeFeed(Entity.Farmhand farmhand)
        {
            var crops = farmhand.FeedHopper.Where(pair => pair.Value > 0)
                .Select(pair => $"{pair.Value} {Farming.GetCrop(pair.Key)?.Name ?? pair.Key.ToString()}")
                .ToList();
            return crops.Count == 0 ? string.Empty : $" ({string.Join(", ", crops)})";
        }

        private static string DescribeOutput(Entity.Farmhand farmhand)
        {
            var items = farmhand.OutputHopper.Where(pair => pair.Value > 0)
                .Select(pair => $"{pair.Value} {Cache.GetItemNameByResref(pair.Key) ?? pair.Key}")
                .ToList();
            return items.Count == 0 ? string.Empty : $" ({string.Join(", ", items)})";
        }

        private void PickPageInit(ConversationMenuPage page)
        {
            var targetId = Data<FarmModel>().TargetId;
            var controllers = Space.GetPostableDroids(Player);
            if (controllers.Count == 0)
            {
                page.Header = "You carry no droid you can assign. A droid that follows you must be dismissed first.";
                return;
            }

            page.Header = "Choose a droid to assign. Its controller stays here until you recall it.";
            foreach (var controller in controllers)
            {
                var chosen = controller;
                page.AddResponse($"{Droid.GetDroidName(controller)} (tier {Farmhands.GetDroidTier(controller)})", () =>
                {
                    var refusal = Farmhands.AssignDroid(Player, targetId, chosen);
                    Report(refusal);
                    GoToPage(FarmhandPageId, false);
                });
            }
        }

        private void FeedPageInit(ConversationMenuPage page)
        {
            var targetId = Data<FarmModel>().TargetId;
            var crops = Farming.GetAllCrops()
                .Select(crop => (Crop: crop, Count: FarmItems.Count(Player, crop.HarvestResref)))
                .Where(entry => entry.Count > 0)
                .OrderBy(entry => entry.Crop.Tier)
                .ThenBy(entry => entry.Crop.Name)
                .ToList();

            if (crops.Count == 0)
            {
                page.Header = "You carry no crops to load. Each feeding takes " + LivestockRules.FeedCrops + " crops of the animal's tier or lower.";
                return;
            }

            page.Header = $"Load which crop? The hopper holds {FarmhandRules.HopperCapacity}. The droid uses the lowest tier first.";
            foreach (var (crop, count) in crops)
            {
                var type = crop.Type;
                page.AddResponse($"{crop.Name} (tier {crop.Tier}) x{count}", () =>
                {
                    var refusal = Farmhands.LoadFeed(Player, targetId, type, out var loaded);
                    if (refusal == FarmhandRefusal.None)
                        SendMessageToPC(Player, ColorToken.Green($"You loaded {loaded} {crop.Name} into the feed hopper."));
                    else
                        Report(refusal);
                    GoToPage(FarmhandPageId, false);
                });
            }
        }
    }
}

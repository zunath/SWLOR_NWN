using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// A Fermentation Vat's menu (Player City Revamp, "Farming for other crafts"): the batch in it and the colors it has copied,
    /// starting a batch (type, color, then the crop) and collecting a finished one. A batch is a 2 day job, so a menu that picks the
    /// enzyme and takes the ingredients fits it better than a craft window. <see cref="FermentationVats"/> holds the rules and records.
    /// </summary>
    public class FermentationVatDialog : ConversationMenuDefinition
    {
        private class Model
        {
            public string VatId { get; set; }
            public EnzymeType Type { get; set; }
            public EnzymeColorType Color { get; set; }
        }

        private const string MainPageId = "VAT_PAGE";
        private const string TypePageId = "TYPE_PAGE";
        private const string ColorPageId = "COLOR_PAGE";
        private const string CropPageId = "CROP_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(() => Data<Model>().VatId = Property.GetPropertyId(Owner))
                .AddPage(MainPageId, MainPageInit)
                .AddPage(TypePageId, TypePageInit)
                .AddPage(ColorPageId, ColorPageInit)
                .AddPage(CropPageId, CropPageInit);

            return builder.Build();
        }

        private void Report(FermentationResult result)
        {
            SendMessageToPC(Player, result.Succeeded ? ColorToken.Green(result.Message) : ColorToken.Red(result.Message));
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var vatId = Data<Model>().VatId;
            if (FermentationVats.GetVatStructure(vatId) == null)
            {
                page.Header = "This fermentation vat is no longer here.";
                return;
            }

            var now = DateTime.UtcNow;
            var job = FermentationVats.GetJob(vatId);
            var isOwner = job != null && job.OwnerPlayerId == GetObjectUUID(Player);
            var lines = new List<string>
            {
                FermentationVats.VatName,
                EnzymeCultureRules.DescribeBatch(job, now, isOwner),
                string.Empty,
                "Colors this vat has copied:"
            };
            lines.AddRange(EnzymeCultureRules.DescribeStarters(FermentationVats.GetRecord(vatId)?.Starters).Select(line => "- " + line));
            lines.Add(string.Empty);
            lines.Add($"A batch takes {EnzymeCultureRules.CropsPerBatch} crops of one kind, {EnzymeCultureRules.ManurePerBatch} manure and " +
                      $"{EnzymeCultureRules.WaterPerBatch} Distilled Water, needs Agriculture {EnzymeCultureRules.RequiredAgricultureRank}, and makes one enzyme in " +
                      $"{FarmingRules.FormatDuration(TimeSpan.FromHours(EnzymeCultureRules.Hours))}. It rolls like a hunted enzyme, but only up to " +
                      $"{EnzymeCultureRules.PrimaryCeiling / 10.0:0.0}% on its main stats and {EnzymeCultureRules.SecondaryCeiling / 10.0:0.0}% on the rest.");
            lines.Add("The first batch of each enzyme uses up one hunted enzyme of that type and color as its starter, and the vat copies it from then on. " +
                      "An enzyme a vat made cannot start another vat. A CD key runs " + EnzymeCultureRules.MaxRunningPerCdKey + " batch at a time.");
            page.Header = string.Join("\n", lines);

            if (job == null)
                page.AddResponse("Start a batch", () => GoToPage(TypePageId));
            else if (isOwner && EnzymeCultureRules.IsComplete(job, now))
            {
                page.AddResponse("Collect the batch", () =>
                {
                    Report(FermentationVats.Collect(Player, vatId));
                    GoToPage(MainPageId, false);
                });
            }
        }

        private void TypePageInit(ConversationMenuPage page)
        {
            page.Header = "Which enzyme?";
            foreach (var enzymeType in EnzymeCultureRules.Types)
            {
                var type = enzymeType;
                page.AddResponse(type.ToString(), () =>
                {
                    Data<Model>().Type = type;
                    GoToPage(ColorPageId);
                });
            }
        }

        private void ColorPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            var starters = FermentationVats.GetRecord(model.VatId)?.Starters;
            page.Header = $"Which color of {model.Type}? A color the vat has not copied needs one hunted {model.Type} of that color as its starter.";

            foreach (var enzymeColor in EnzymeCultureRules.GetColors(model.Type))
            {
                var color = enzymeColor;
                var copied = EnzymeCultureRules.HasStarter(starters, model.Type, color);
                var hasStarter = GetIsObjectValid(FermentationVats.FindStarter(Player, model.Type, color));
                var note = copied ? "copied" : hasStarter ? "starter in your pack" : "needs a starter";
                page.AddResponse($"{color} ({note})", () =>
                {
                    model.Color = color;
                    GoToPage(CropPageId);
                });
            }
        }

        private void CropPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            var crops = FermentationVats.GetUsableCrops(Player);
            if (crops.Count == 0)
            {
                page.Header = $"You carry no crop, {EnzymeCultureRules.CropsPerBatch} at a time, to use. A batch of {EnzymeCultureRules.GetName(model.Type, model.Color)} needs " +
                              $"{EnzymeCultureRules.CropsPerBatch} of one crop, {EnzymeCultureRules.ManurePerBatch} manure and {EnzymeCultureRules.WaterPerBatch} Distilled Water.";
                return;
            }

            page.Header = $"A batch of {EnzymeCultureRules.GetName(model.Type, model.Color)} uses {EnzymeCultureRules.CropsPerBatch} crops of one kind, plus the lowest tier of manure you carry and " +
                          $"{EnzymeCultureRules.WaterPerBatch} Distilled Water. Which crop?";
            foreach (var (crop, count) in crops)
            {
                var type = crop.Type;
                page.AddResponse($"{crop.Name} (tier {crop.Tier}) x{count}", () =>
                {
                    Report(FermentationVats.Start(Player, model.VatId, model.Type, model.Color, type));
                    GoToPage(MainPageId, false);
                });
            }
        }
    }
}

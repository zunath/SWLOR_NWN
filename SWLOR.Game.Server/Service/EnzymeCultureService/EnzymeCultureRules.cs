using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.EnzymeCultureService
{
    /// <summary>
    /// The enzyme culture rules of the Player City Revamp ("Farming for other crafts"), with no database or engine calls. A
    /// Fermentation Vat, placed in a house in a city, turns 2 crops, 1 manure and Distilled Water into one enzyme of the chosen
    /// type and color over 2 days. It can make all 15 enzymes (Hydrolase, Isomerase and Lyase in their five colors each), needs
    /// Agriculture 20, and rolls like a hunted enzyme from the lowest brackets only (<see cref="EnzymeRolls"/>). A vat copies only
    /// the colors it has been shown: the first batch of each type and color uses up one hunted enzyme of that kind as its
    /// starter, and the vat remembers it. Each CD key runs one batch at a time. The <c>FermentationVats</c> service reads and writes
    /// the records and the inventory and asks these rules what is allowed.
    /// </summary>
    public static class EnzymeCultureRules
    {
        /// <summary>The Agriculture rank a culture needs: a level 20 recipe.</summary>
        public const int RequiredAgricultureRank = 20;

        /// <summary>The Agriculture level the culture pays its XP at.</summary>
        public const int RecipeLevel = RequiredAgricultureRank;

        /// <summary>Hours from starting a batch to its enzyme: 2 days.</summary>
        public const int Hours = 48;

        /// <summary>The crops one batch uses, all of one type.</summary>
        public const int CropsPerBatch = 2;

        /// <summary>The manure one batch uses, of any tier.</summary>
        public const int ManurePerBatch = 1;

        /// <summary>The Distilled Water one batch uses.</summary>
        public const int WaterPerBatch = 1;

        /// <summary>The item resref of Distilled Water.</summary>
        public const string WaterResref = "distilled_water";

        /// <summary>The batches one CD key can have running at a time, across its characters and vats.</summary>
        public const int MaxRunningPerCdKey = 1;

        /// <summary>The highest primary bracket (the third: 2.1% to 3.0%) a cultured enzyme rolls.</summary>
        public const int MaxPrimaryBracket = 2;

        /// <summary>The highest secondary bracket (the second: 1.1% to 2.0%) a cultured enzyme rolls.</summary>
        public const int MaxSecondaryBracket = 1;

        /// <summary>No cultured primary stat is above 3.0%, in tenths of a percent.</summary>
        public const int PrimaryCeiling = 30;

        /// <summary>No cultured secondary stat is above 2.0%, in tenths of a percent.</summary>
        public const int SecondaryCeiling = 20;

        private static readonly Dictionary<EnzymeType, EnzymeColorType[]> Colors = new()
        {
            [EnzymeType.Hydrolase] = new[] { EnzymeColorType.Blue, EnzymeColorType.Orange, EnzymeColorType.Red, EnzymeColorType.Purple, EnzymeColorType.White },
            [EnzymeType.Isomerase] = new[] { EnzymeColorType.Orange, EnzymeColorType.Blue, EnzymeColorType.Green, EnzymeColorType.Red, EnzymeColorType.Yellow },
            [EnzymeType.Lyase] = new[] { EnzymeColorType.Green, EnzymeColorType.Blue, EnzymeColorType.Orange, EnzymeColorType.Red, EnzymeColorType.Black },
        };

        /// <summary>The three enzyme types, in the order the vat lists them.</summary>
        public static IReadOnlyList<EnzymeType> Types { get; } = new[] { EnzymeType.Hydrolase, EnzymeType.Isomerase, EnzymeType.Lyase };

        /// <summary>The five colors a type comes in.</summary>
        public static IReadOnlyList<EnzymeColorType> GetColors(EnzymeType type) =>
            Colors.TryGetValue(type, out var colors) ? colors : Array.Empty<EnzymeColorType>();

        /// <summary>The 15 enzymes a vat can make.</summary>
        public static IReadOnlyList<(EnzymeType Type, EnzymeColorType Color)> GetAll() =>
            Types.SelectMany(type => GetColors(type).Select(color => (type, color))).ToList();

        public static bool IsEnzyme(EnzymeType type, EnzymeColorType color) => GetColors(type).Contains(color);

        /// <summary>The enzyme's item resref: <c>hydrolase_blue</c>.</summary>
        public static string GetResref(EnzymeType type, EnzymeColorType color) =>
            $"{type.ToString().ToLowerInvariant()}_{color.ToString().ToLowerInvariant()}";

        /// <summary>How the vat names an enzyme: "Hydrolase - Blue".</summary>
        public static string GetName(EnzymeType type, EnzymeColorType color) => $"{type} - {color}";

        // Starters

        /// <summary>Whether the vat has copied this color already, from the resrefs it remembers.</summary>
        public static bool HasStarter(IEnumerable<string> starters, EnzymeType type, EnzymeColorType color) =>
            starters != null && starters.Contains(GetResref(type, color), StringComparer.OrdinalIgnoreCase);

        /// <summary>The starters a vat remembers once it has used one of this color: the same list when it knew it, else with it added.</summary>
        public static List<string> WithStarter(IEnumerable<string> starters, EnzymeType type, EnzymeColorType color)
        {
            var list = starters?.ToList() ?? new List<string>();
            if (!HasStarter(list, type, color))
                list.Add(GetResref(type, color));
            return list;
        }

        /// <summary>
        /// Whether an item can be the starter of a batch: a hunted enzyme of exactly the type and color. An enzyme a vat made is
        /// marked, and cannot start another vat, so finding a color still takes hunting.
        /// </summary>
        public static bool CanBeStarter(string itemResref, bool isCultured, EnzymeType type, EnzymeColorType color) =>
            !isCultured && string.Equals(itemResref, GetResref(type, color), StringComparison.OrdinalIgnoreCase);

        // The memory travels with the vat

        /// <summary>The character that joins the remembered resrefs in the text kept on the structure item.</summary>
        public const char StarterSeparator = ',';

        /// <summary>
        /// The text a picked up vat keeps on its structure item: the remembered enzymes, as resrefs in the vat's own order (type, then
        /// the colors as the loot tables list them) joined by <see cref="StarterSeparator"/>. Anything that is not one of the 15
        /// enzymes is left out, and so is a repeat. Empty for a vat that remembers nothing.
        /// </summary>
        public static string EncodeStarters(IEnumerable<string> starters)
        {
            var known = starters?.ToList() ?? new List<string>();
            return string.Join(StarterSeparator, GetAll()
                .Select(enzyme => GetResref(enzyme.Type, enzyme.Color))
                .Where(resref => known.Contains(resref, StringComparer.OrdinalIgnoreCase)));
        }

        /// <summary>The remembered enzymes in the text a structure item keeps, in the vat's own order. Unknown entries and repeats are dropped.</summary>
        public static List<string> DecodeStarters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var named = text.Split(StarterSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return GetAll()
                .Select(enzyme => GetResref(enzyme.Type, enzyme.Color))
                .Where(resref => named.Contains(resref, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// What a vat being picked up takes with it: everything it remembers, except the color of a batch that is still running on
        /// a starter. That starter goes back to its owner with the rest of the batch's ingredients, so the vat must not also keep the
        /// color, or one hunted enzyme would teach two vats. A batch that had finished is done, and its color stays.
        /// </summary>
        public static List<string> GetCarriedStarters(IEnumerable<string> starters, FermentationJob job, DateTime now)
        {
            var carried = starters?.ToList() ?? new List<string>();
            if (IsRunning(job, now) && !string.IsNullOrEmpty(job.StarterData))
                carried.RemoveAll(resref => string.Equals(resref, GetResref(job.Type, job.Color), StringComparison.OrdinalIgnoreCase));
            return carried;
        }

        // Timing

        public static DateTime GetCompleteUtc(DateTime startedUtc) => startedUtc.AddHours(Hours);

        /// <summary>A batch is done once its time has come, whether or not the server ran in the meantime.</summary>
        public static bool IsComplete(FermentationJob job, DateTime now) => job != null && now >= job.CompleteUtc;

        public static bool IsRunning(FermentationJob job, DateTime now) => job != null && now < job.CompleteUtc;

        public static TimeSpan GetTimeLeft(FermentationJob job, DateTime now) =>
            job == null || now >= job.CompleteUtc ? TimeSpan.Zero : job.CompleteUtc - now;

        /// <summary>The batches still running among a CD key's jobs. A finished batch waiting to be collected is not running.</summary>
        public static int CountRunning(IEnumerable<FermentationJob> jobs, DateTime now) =>
            jobs?.Count(job => IsRunning(job, now)) ?? 0;

        // Inputs

        /// <summary>
        /// The manure the batch uses: the lowest tier the character carries (tier 1 to 5), or an empty string when they carry none.
        /// </summary>
        public static string PickManure(Func<string, int> carried)
        {
            for (var tier = 1; tier <= FarmingRules.Tiers; tier++)
            {
                var resref = LivestockRules.GetManureResref(tier);
                if (carried(resref) >= ManurePerBatch)
                    return resref;
            }

            return string.Empty;
        }

        /// <summary>Whether a crop can go in a batch: any of the 26 crops, never a woodlot sapling.</summary>
        public static bool IsCultureCrop(CropDefinition crop) => crop is { IsSapling: false };

        // Checks

        /// <summary>Why a character cannot start a batch, or <see cref="FermentationRefusal.None"/>. Checked in this order.</summary>
        /// <param name="vatJob">The batch already in the vat, or null.</param>
        /// <param name="cdKeyJobs">Every batch the character's CD key has in any vat.</param>
        /// <param name="agricultureRank">The character's Agriculture rank.</param>
        /// <param name="cropIsUsable">Whether the crop picked is one a culture can use (<see cref="IsCultureCrop"/>).</param>
        /// <param name="cropsCarried">How many of the crop the character carries.</param>
        /// <param name="manureCarried">Whether the character carries any manure.</param>
        /// <param name="waterCarried">How many Distilled Water the character carries.</param>
        /// <param name="starters">The enzymes the vat has copied already.</param>
        /// <param name="hasStarterItem">Whether the character carries a hunted enzyme of the type and color.</param>
        public static FermentationRefusal CheckStart(
            bool isPlayer,
            bool vatExists,
            bool hasAccess,
            FermentationJob vatJob,
            IEnumerable<FermentationJob> cdKeyJobs,
            DateTime now,
            EnzymeType type,
            EnzymeColorType color,
            int agricultureRank,
            bool cropIsUsable,
            int cropsCarried,
            bool manureCarried,
            int waterCarried,
            IEnumerable<string> starters,
            bool hasStarterItem)
        {
            if (!isPlayer)
                return FermentationRefusal.NotAPlayer;
            if (!vatExists)
                return FermentationRefusal.NoVat;
            if (!hasAccess)
                return FermentationRefusal.NoAccess;
            if (!IsEnzyme(type, color))
                return FermentationRefusal.UnknownEnzyme;
            if (vatJob != null)
                return FermentationRefusal.VatBusy;
            if (CountRunning(cdKeyJobs, now) >= MaxRunningPerCdKey)
                return FermentationRefusal.CdKeyBusy;
            if (agricultureRank < RequiredAgricultureRank)
                return FermentationRefusal.SkillTooLow;
            if (!cropIsUsable)
                return FermentationRefusal.NotACrop;
            if (cropsCarried < CropsPerBatch)
                return FermentationRefusal.NoCrops;
            if (!manureCarried)
                return FermentationRefusal.NoManure;
            if (waterCarried < WaterPerBatch)
                return FermentationRefusal.NoWater;
            if (!HasStarter(starters, type, color) && !hasStarterItem)
                return FermentationRefusal.NoStarter;
            return FermentationRefusal.None;
        }

        /// <summary>Why a character cannot collect the vat's batch, or <see cref="FermentationRefusal.None"/>.</summary>
        public static FermentationRefusal CheckCollect(bool isPlayer, bool vatExists, FermentationJob job, string playerId, DateTime now)
        {
            if (!isPlayer)
                return FermentationRefusal.NotAPlayer;
            if (!vatExists)
                return FermentationRefusal.NoVat;
            if (job == null)
                return FermentationRefusal.NoBatch;
            if (!string.Equals(job.OwnerPlayerId, playerId, StringComparison.Ordinal))
                return FermentationRefusal.NotTheOwner;
            if (!IsComplete(job, now))
                return FermentationRefusal.NotDone;
            return FermentationRefusal.None;
        }

        // Pay and recovery

        /// <summary>
        /// The Agriculture XP a finished batch pays: what a level 20 Agriculture recipe pays at the character's rank, through the
        /// same path crafts use (<c>CraftMinigameXp.BaseXp</c>), so a character who has outgrown level 20 gets less, as with any recipe.
        /// </summary>
        public static int GetXp(int agricultureRank) =>
            CraftMinigameXp.BaseXp(new RecipeDetail { Skill = SkillType.Agriculture, Level = RecipeLevel }, agricultureRank);

        /// <summary>What becomes of a batch when its vat goes away: a finished one is delivered, a running one is returned.</summary>
        public static FermentationRecovery GetRecovery(FermentationJob job, DateTime now) =>
            IsComplete(job, now) ? FermentationRecovery.DeliverEnzyme : FermentationRecovery.ReturnInputs;

        // Text

        /// <summary>The message a player reads for a refusal.</summary>
        public static string Describe(FermentationRefusal refusal)
        {
            return refusal switch
            {
                FermentationRefusal.None => string.Empty,
                FermentationRefusal.NotAPlayer => "Only characters can use a fermentation vat.",
                FermentationRefusal.NoVat => "This fermentation vat is no longer here.",
                FermentationRefusal.NoAccess => "You do not have permission to tend gardens in this house, so you cannot use its vat.",
                FermentationRefusal.UnknownEnzyme => "That is not an enzyme a vat can make.",
                FermentationRefusal.VatBusy => "This vat already holds a batch. Collect it first.",
                FermentationRefusal.CdKeyBusy => $"You already have a batch fermenting. A CD key runs {MaxRunningPerCdKey} at a time.",
                FermentationRefusal.SkillTooLow => $"Agriculture must be level {RequiredAgricultureRank} to culture enzymes.",
                FermentationRefusal.NotACrop => "That is not a crop a culture can use.",
                FermentationRefusal.NoCrops => $"A batch needs {CropsPerBatch} of the same crop, and you carry fewer.",
                FermentationRefusal.NoManure => "A batch needs 1 manure, and you carry none.",
                FermentationRefusal.NoWater => "A batch needs 1 Distilled Water, and you carry none.",
                FermentationRefusal.NoStarter => "This vat has not copied that enzyme yet. The first batch of each type and color needs 1 hunted enzyme of it as a starter, and you carry none.",
                FermentationRefusal.NoBatch => "This vat holds no batch.",
                FermentationRefusal.NotTheOwner => "That batch belongs to another character.",
                FermentationRefusal.NotDone => "The batch is not finished yet.",
                _ => string.Empty
            };
        }

        public static string DescribeStarted(EnzymeType type, EnzymeColorType color, bool usedStarter) =>
            $"You started a batch of {GetName(type, color)}. It will be ready in {FarmingRules.FormatDuration(TimeSpan.FromHours(Hours))}." +
            (usedStarter ? $" The vat used your hunted enzyme as its starter and will copy {GetName(type, color)} from now on." : string.Empty);

        public static string DescribeCollected(EnzymeType type, EnzymeColorType color, bool inOverflow) =>
            $"The batch is ready: you collected a cultured {GetName(type, color)} enzyme." +
            (inOverflow ? " It did not fit in your inventory and is in your overflow storage. Use /overflow to claim it." : string.Empty);

        /// <summary>What the vat's page says about its batch, to the character looking at it.</summary>
        public static string DescribeBatch(FermentationJob job, DateTime now, bool viewerIsOwner)
        {
            if (job == null)
                return "The vat is empty.";

            var name = GetName(job.Type, job.Color);
            if (!IsComplete(job, now))
                return $"A batch of {name} is fermenting. It will be ready in {FarmingRules.FormatDuration(GetTimeLeft(job, now))}.";

            return viewerIsOwner
                ? $"Your batch of {name} is ready to collect."
                : $"A batch of {name} is ready, waiting for its owner to collect it.";
        }

        /// <summary>The colors the vat has copied, a line for each type: "Hydrolase: Blue, Red".</summary>
        public static IReadOnlyList<string> DescribeStarters(IEnumerable<string> starters)
        {
            var known = starters?.ToList() ?? new List<string>();
            return Types
                .Select(type =>
                {
                    var colors = GetColors(type).Where(color => HasStarter(known, type, color)).Select(color => color.ToString()).ToList();
                    return $"{type}: {(colors.Count == 0 ? "none" : string.Join(", ", colors))}";
                })
                .ToList();
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class GatheringPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.EfficientStrikes,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Prospector, Gathering 5 and 25. Prospecting stats only; a Strike and a Heavy Strike take less time; no feat.",
                },
                new()
                {
                    Perk = PerkType.BulkRefining,
                    MaxLevel = 3,
                    Prices = new[] { 1, 3, 3 },
                    Notes = "Prospector, Gathering 10, 30 and 45. Each power core refines more items; read by the refinery, not a Prospecting session.",
                },
                new()
                {
                    Perk = PerkType.RichVeins,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    Notes = "Prospector, Gathering 15 and 40. The Extract of a rich seam takes less time.",
                },
                new()
                {
                    Perk = PerkType.StripMiningStyle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Prospector style, Gathering 20. Picked in the window; carried by StripMiningStatusEffect. Refund removes an unowned style.",
                },
                new()
                {
                    Perk = PerkType.CoreSample,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Prospector, Gathering 35. The Prospecting window shows the seams a node still holds.",
                },
                new()
                {
                    Perk = PerkType.MotherLode,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Prospector capstone, Gathering 50. Automatic trait: the first Strike of each node takes half the time.",
                },
                new()
                {
                    Perk = PerkType.FineTrowel,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Treasure Seeker, Gathering 5 and 40. A Dig Deeper find weights the site's rare entries more.",
                },
                new()
                {
                    Perk = PerkType.SharpChisel,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Treasure Seeker, Gathering 10 and 30. A Heavy Strike shatters an unsurveyed gem pocket less often.",
                },
                new()
                {
                    Perk = PerkType.SteadyDig,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Treasure Seeker, Gathering 15. Dig Deeper risks ruining a spot less.",
                },
                new()
                {
                    Perk = PerkType.CarefulHandsStyle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Treasure Seeker style, Gathering 20. Picked in the window; carried by CarefulHandsStatusEffect. Refund removes an unowned style.",
                },
                new()
                {
                    Perk = PerkType.KeenEye,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Treasure Seeker, Gathering 25. A Search finds more often on every spot.",
                },
                new()
                {
                    Perk = PerkType.DeepScan,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Treasure Seeker, Gathering 35. A Survey reveals one more seam and shows gem pockets first.",
                },
                new()
                {
                    Perk = PerkType.RichPickings,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Treasure Seeker, Gathering 45. A Search find weights the site's rare entries more.",
                },
                new()
                {
                    Perk = PerkType.Jackpot,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Treasure Seeker capstone, Gathering 50. Automatic trait: the first Dig Deeper find of each board weights the rare entries most.",
                },
                new()
                {
                    Perk = PerkType.Prospector,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.ProspectorTrait },
                },
                new()
                {
                    Perk = PerkType.DeepCore,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.DeepCoreTrait },
                },
                new()
                {
                    Perk = PerkType.RapidExtraction,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.RapidExtractionTrait },
                },
                new()
                {
                    Perk = PerkType.GasSiphoning,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.GasSiphoningTrait },
                },
                new()
                {
                    Perk = PerkType.SalvageExpert,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.SalvageExpertTrait },
                },
                new()
                {
                    Perk = PerkType.TraitHunter,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.TraitHunterTrait },
                },
            };
        }
    }
}

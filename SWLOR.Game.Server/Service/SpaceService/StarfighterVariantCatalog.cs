using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class StarfighterVariantCatalog
    {
        private record BaseVariant(string Name, int AppearanceId, PropertyLayoutType Layout);

        public record StarfighterVariantDefinition(
            int Sequence,
            int Tier,
            string Name,
            string DisplayName,
            string DeedTag,
            string DeedResref,
            int AppearanceId,
            PropertyLayoutType Layout,
            RecipeType RecipeType,
            int PerkLevel,
            int HighPowerNodes,
            int LowPowerNodes,
            int MaxHull,
            int MaxCapacitor,
            int MaxShield,
            int ShieldRechargeRate,
            int RecipeLevel,
            int EnhancementSlots,
            string RefinedComponentResref,
            int RefinedComponentQuantity,
            string MaterialComponentResref,
            int MaterialComponentQuantity,
            string FiberComponentResref,
            int FiberComponentQuantity,
            string ElectronicsComponentResref,
            int ElectronicsComponentQuantity);

        private static readonly IReadOnlyList<BaseVariant> _baseVariants = new List<BaseVariant>
        {
            new("Neutral Striker", 10057, PropertyLayoutType.Striker),
            new("Armored Transport", 10218, PropertyLayoutType.Consular),
            new("Bretonia", 10197, PropertyLayoutType.Saber),
            new("Civilian Elite Fighter", 10192, PropertyLayoutType.Fighter),
            new("Civilian Fighter", 10191, PropertyLayoutType.Fighter),
            new("Corsair Mk2", 10194, PropertyLayoutType.Mule),
            new("Corsair", 10193, PropertyLayoutType.Mule),
            new("Cutlass", 10161, PropertyLayoutType.Fighter),
            new("S-100 Stinger Starfighter", 10159, PropertyLayoutType.Fighter),
            new("Hutt Bomber", 10106, PropertyLayoutType.Fighter),
            new("Hutt Fighter", 10105, PropertyLayoutType.Fighter),
            new("Hutt Gunship", 10107, PropertyLayoutType.Merchant),
            new("Invader", 10158, PropertyLayoutType.Fighter),
            new("Hunter", 10222, PropertyLayoutType.Fighter),
            new("Jedi Transport", 10119, PropertyLayoutType.Falchion),
            new("Kusari Mk2", 10205, PropertyLayoutType.Fighter),
            new("Kusari", 10204, PropertyLayoutType.Fighter),
            new("Kusari Freighter", 10206, PropertyLayoutType.Panther),
            new("Liberty Mk2", 10200, PropertyLayoutType.Hound),
            new("Liberty", 10199, PropertyLayoutType.Hound),
            new("Bretonia Freighter", 10197, PropertyLayoutType.Consular),
            new("Mandalorian Brute Patrol Ship", 10156, PropertyLayoutType.Throne),
            new("Davaab-type Starfighter", 10155, PropertyLayoutType.Fighter),
            new("Teroch-type Gunship", 10153, PropertyLayoutType.Fighter),
            new("Neutral Barracuda", 10113, PropertyLayoutType.Fighter),
            new("Civilian BW Fighter", 10112, PropertyLayoutType.Fighter),
            new("Civilian Condor", 10055, PropertyLayoutType.Condor),
            new("Civilian Freighter", 10054, PropertyLayoutType.LightFreighter1),
            new("ST-07 Assault ship", 10056, PropertyLayoutType.Hound),
            new("Neutral Quartermaster transport", 10132, PropertyLayoutType.Throne),
            new("Starflier", 10114, PropertyLayoutType.Fighter),
            new("YV-929 Hauler", 10151, PropertyLayoutType.Throne),
            new("Onderon Ruping Bomber", 10237, PropertyLayoutType.Mule),
            new("Onderon Type81a Fighter", 10236, PropertyLayoutType.Fighter),
            new("Order Fighter", 10195, PropertyLayoutType.Fighter),
            new("Phoebos", 10104, PropertyLayoutType.Fighter),
            new("Pirate Fighter", 10183, PropertyLayoutType.Striker),
            new("Pirate Freighter", 10184, PropertyLayoutType.Hound),
            new("Assault Transport", 10150, PropertyLayoutType.Merchant),
            new("Military Bomber MK 1", 10058, PropertyLayoutType.Condor),
            new("Military Bomber MK 2", 10059, PropertyLayoutType.Condor),
            new("Military Bomber MK 3", 10060, PropertyLayoutType.Condor),
            new("S-250 Chela Starfighter", 10149, PropertyLayoutType.Fighter),
            new("Military Gunship Large", 10148, PropertyLayoutType.Striker),
            new("Military Gunship MK 1", 10061, PropertyLayoutType.Hound),
            new("Military Gunship MK 2", 10062, PropertyLayoutType.Hound),
            new("Military Gunship MK 3", 10063, PropertyLayoutType.Hound),
            new("Infiltrator MK 1", 10064, PropertyLayoutType.Fighter),
            new("Infiltrator MK 2", 10065, PropertyLayoutType.Fighter),
            new("Infiltrator MK 3", 10066, PropertyLayoutType.Fighter),
            new("Twin Infiltrator", 10067, PropertyLayoutType.LightEscort1),
            new("Advanced Scout MK 1", 10068, PropertyLayoutType.Fighter),
            new("Advanced Scout MK 2", 10069, PropertyLayoutType.Fighter),
            new("Star Saber XC-01", 10070, PropertyLayoutType.Fighter),
            new("Advanced Striker MK 1", 10071, PropertyLayoutType.Striker),
            new("Advanced Striker MK 2", 10072, PropertyLayoutType.Striker),
            new("Advanced Striker MK 3", 10073, PropertyLayoutType.Striker),
            new("Rheinland Mk 2", 10211, PropertyLayoutType.Merchant),
            new("Rheinland", 10210, PropertyLayoutType.Merchant),
            new("Rheinland Freighter", 10212, PropertyLayoutType.LightFreighter1),
            new("Advanced Bomber", 10077, PropertyLayoutType.Striker),
            new("Twin Bomber", 10241, PropertyLayoutType.Fighter),
            new("Advanced Bomber MK 2", 10078, PropertyLayoutType.Condor),
            new("Advanced Bomber MK 3", 10079, PropertyLayoutType.Condor),
            new("Advanced Gunboat", 10100, PropertyLayoutType.Mule),
            new("Advanced Gunship MK 1", 10080, PropertyLayoutType.Condor),
            new("Advanced Gunship MK 2", 10081, PropertyLayoutType.Condor),
            new("Advanced Gunship MK 3", 10082, PropertyLayoutType.Condor),
            new("Infiltrator MK 1", 10083, PropertyLayoutType.Striker),
            new("Infiltrator MK 2", 10084, PropertyLayoutType.Striker),
            new("Infiltrator MK 3", 10085, PropertyLayoutType.Striker),
            new("Advanced Scout MK 1", 10086, PropertyLayoutType.LightEscort1),
            new("Advanced Scout MK 2", 10087, PropertyLayoutType.LightEscort1),
            new("Advanced Scout MK 3", 10088, PropertyLayoutType.LightEscort1),
            new("Advanced Striker Mk 1", 10089, PropertyLayoutType.LightEscort1),
            new("Advanced Striker Mk 2", 10090, PropertyLayoutType.LightEscort1),
            new("Advanced Striker Mk 3", 10091, PropertyLayoutType.LightEscort1),
            new("KT-400 Light Freighter", 10240, PropertyLayoutType.Saber),
            new("Trandoshan Transport", 10147, PropertyLayoutType.Consular),
            new("XS Freighter", 10146, PropertyLayoutType.LightFreighter1),
            new("Y8 Miner Ship", 10152, PropertyLayoutType.LightFreighter1),
            new("Zoomer Fighter", 10144, PropertyLayoutType.Fighter),
            new("Legion Fighter", 10259, PropertyLayoutType.Fighter),
        };

        private static readonly IReadOnlyList<StarfighterVariantDefinition> _variants = Build();
        private static readonly Dictionary<RecipeType, StarfighterVariantDefinition> _byRecipeType = _variants.ToDictionary(x => x.RecipeType, x => x);

        public static IReadOnlyList<StarfighterVariantDefinition> GetTier(int tier)
        {
            return _variants.Where(x => x.Tier == tier).ToList();
        }

        public static bool TryGetByRecipeType(RecipeType recipeType, out StarfighterVariantDefinition definition)
        {
            return _byRecipeType.TryGetValue(recipeType, out definition);
        }

        public static string GetRecipeDisplayName(RecipeType recipeType, string fallbackName)
        {
            return TryGetByRecipeType(recipeType, out var definition)
                ? definition.DisplayName
                : fallbackName;
        }

        private static List<StarfighterVariantDefinition> Build()
        {
            var results = new List<StarfighterVariantDefinition>();

            var totalCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var variant in _baseVariants)
            {
                if (!totalCounts.ContainsKey(variant.Name))
                    totalCounts[variant.Name] = 0;
                totalCounts[variant.Name]++;
            }

            var seenByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _baseVariants.Count; i++)
            {
                var sequence = i + 1;
                var baseVariant = _baseVariants[i];
                var profileA = sequence % 2 == 1;

                if (!seenByName.ContainsKey(baseVariant.Name))
                    seenByName[baseVariant.Name] = 0;
                seenByName[baseVariant.Name]++;
                var occurrence = seenByName[baseVariant.Name];
                var needsVariantSuffix = totalCounts[baseVariant.Name] > 1;
                var variantSuffix = needsVariantSuffix ? $" - Variant {occurrence}" : string.Empty;

                results.Add(CreateTierDefinition(sequence, 1, baseVariant, profileA, variantSuffix, occurrence, needsVariantSuffix));
                results.Add(CreateTierDefinition(sequence, 2, baseVariant, profileA, variantSuffix, occurrence, needsVariantSuffix));
                results.Add(CreateTierDefinition(sequence, 3, baseVariant, profileA, variantSuffix, occurrence, needsVariantSuffix));
                results.Add(CreateTierDefinition(sequence, 4, baseVariant, profileA, variantSuffix, occurrence, needsVariantSuffix));
                results.Add(CreateTierDefinition(sequence, 5, baseVariant, profileA, variantSuffix, occurrence, needsVariantSuffix));
            }

            return results;
        }

        private static StarfighterVariantDefinition CreateTierDefinition(
            int sequence,
            int tier,
            BaseVariant baseVariant,
            bool profileA,
            string variantSuffix,
            int occurrence,
            bool needsVariantSuffix)
        {
            var displayName = $"{baseVariant.Name}{variantSuffix} (Tier {tier})";
            var deedTag = $"sdeed_sv{sequence:000}_t{tier}";
            var deedResref = deedTag;

            var recipeType = (RecipeType)(tier switch
            {
                1 => (int)RecipeType.StarfighterVariantTier1Start + (sequence - 1),
                2 => (int)RecipeType.StarfighterVariantTier2Start + (sequence - 1),
                3 => (int)RecipeType.StarfighterVariantTier3Start + (sequence - 1),
                4 => (int)RecipeType.StarfighterVariantTier4Start + (sequence - 1),
                5 => (int)RecipeType.StarfighterVariantTier5Start + (sequence - 1),
                _ => (int)RecipeType.StarfighterVariantTier1Start + (sequence - 1)
            });

            if (tier == 1)
            {
                return profileA
                    ? new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        1, 2, 3, 50, 40, 50, 4, 5, 1,
                        "ref_tilarium", 2, "aluminum", 1, "fiberp_ruined", 1, "elec_ruined", 1)
                    : new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        1, 3, 2, 40, 60, 40, 4, 10, 1,
                        "ref_tilarium", 5, "aluminum", 3, "fiberp_ruined", 3, "elec_ruined", 3);
            }

            if (tier == 2)
            {
                return profileA
                    ? new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        2, 4, 5, 100, 80, 100, 6, 15, 1,
                        "ref_currian", 2, "steel", 1, "fiberp_flawed", 1, "elec_flawed", 1)
                    : new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        2, 5, 4, 80, 120, 80, 6, 20, 1,
                        "ref_currian", 5, "steel", 3, "fiberp_flawed", 3, "elec_flawed", 3);
            }

            if (tier == 3)
            {
                return profileA
                    ? new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        3, 5, 6, 150, 120, 150, 8, 25, 2,
                        "ref_idailia", 2, "obsidian", 1, "fiberp_good", 1, "elec_good", 1)
                    : new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        3, 6, 5, 120, 180, 120, 8, 30, 2,
                        "ref_idailia", 5, "obsidian", 3, "fiberp_good", 3, "elec_good", 3);
            }

            if (tier == 4)
            {
                return profileA
                    ? new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        4, 5, 6, 200, 160, 200, 10, 35, 2,
                        "ref_barinium", 2, "crystal", 1, "fiberp_imperfect", 1, "elec_imperfect", 1)
                    : new StarfighterVariantDefinition(
                        sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                        4, 6, 5, 160, 240, 160, 10, 40, 2,
                        "ref_barinium", 5, "crystal", 3, "fiberp_imperfect", 3, "elec_imperfect", 3);
            }

            // Tier 5 intentionally uses only Throne/Consular profile templates.
            return profileA
                ? new StarfighterVariantDefinition(
                    sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                    5, 6, 7, 250, 200, 250, 12, 45, 2,
                    "ref_gostian", 2, "diamond", 1, "fiberp_high", 1, "elec_high", 1)
                : new StarfighterVariantDefinition(
                    sequence, tier, baseVariant.Name, displayName, deedTag, deedResref, baseVariant.AppearanceId, baseVariant.Layout, recipeType,
                    5, 7, 6, 200, 300, 200, 12, 50, 2,
                    "ref_gostian", 5, "diamond", 3, "fiberp_high", 3, "elec_high", 3);
        }

    }
}

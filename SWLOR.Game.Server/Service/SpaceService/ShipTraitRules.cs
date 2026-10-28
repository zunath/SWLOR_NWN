using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The component trait rules (plan section 14) as pure functions: what a module's traits do to its numbers, how
    /// items store them, and what a salvage cycle yields as salvaged components.
    /// </summary>
    public static class ShipTraitRules
    {
        /// <summary>The item local that stores an item's traits as comma-separated numbers.</summary>
        public const string TraitsVariable = "SHIP_COMPONENT_TRAITS";

        private static readonly Dictionary<ShipComponentTraitType, ShipComponentTrait> _traits =
            RevampComponentCatalog.Traits.ToDictionary(x => x.Type);

        private static readonly Dictionary<string, ShipComponentItem> _components =
            RevampComponentCatalog.Items.ToDictionary(x => x.Resref);

        private static readonly Dictionary<RecipeType, ShipComponentRecipe> _recipes =
            RevampComponentCatalog.Recipes.ToDictionary(x => x.RecipeType);

        public static ShipComponentTrait Get(ShipComponentTraitType type) =>
            _traits.TryGetValue(type, out var trait) ? trait : null;

        /// <summary>Whether the resref is a component item.</summary>
        public static bool IsComponent(string resref) => resref != null && _components.ContainsKey(resref);

        /// <summary>The family of a component item, or Invalid when the resref is not one.</summary>
        public static ShipComponentFamily FamilyOf(string resref) =>
            resref != null && _components.TryGetValue(resref, out var item) ? item.Family : ShipComponentFamily.Invalid;

        /// <summary>The component item of a family at a tier (clamped to 1-5).</summary>
        public static string ComponentResref(ShipComponentFamily family, int tier)
        {
            var clamped = Math.Clamp(tier, 1, 5);
            return RevampComponentCatalog.Items.Single(x => x.Family == family && x.Tier == clamped).Resref;
        }

        /// <summary>The trait a component recipe stamps on what it makes: its crafted variant, or None.</summary>
        public static ShipComponentTraitType VariantTrait(RecipeType recipe) =>
            _recipes.TryGetValue(recipe, out var row) ? row.Trait : ShipComponentTraitType.None;

        // Effects

        /// <summary>The summed value of one stat over the traits.</summary>
        public static int Sum(IEnumerable<ShipComponentTraitType> traits, ShipTraitStat stat)
        {
            var total = 0;
            foreach (var type in traits ?? Enumerable.Empty<ShipComponentTraitType>())
            {
                var trait = Get(type);
                if (trait == null) continue;
                total += trait.Effects.Where(x => x.Stat == stat).Sum(x => x.Value);
            }

            return total;
        }

        /// <summary>A value changed by a percent, rounded half away from zero.</summary>
        public static int Scale(int value, int percent) =>
            percent == 0 ? value : (int)Math.Round(value * (100 + percent) / 100.0, MidpointRounding.AwayFromZero);

        private static int ScaleKeepingOne(int value, int percent) =>
            value > 0 ? Math.Max(1, Scale(value, percent)) : Scale(value, percent);

        /// <summary>
        /// The module step as its traits make it: a copy with the percent stats applied, or the step itself when the
        /// module has no traits. Evasion, Speed, Yield and RareFind are not module numbers and are read separately.
        /// </summary>
        public static ShipModuleStep Apply(ShipModuleStep step, IReadOnlyCollection<ShipComponentTraitType> traits)
        {
            if (step == null || traits == null || traits.Count == 0)
                return step;

            int P(ShipTraitStat stat) => Sum(traits, stat);
            var damage = P(ShipTraitStat.Damage);
            var accuracy = P(ShipTraitStat.Accuracy);
            var shield = P(ShipTraitStat.Shield);
            var capacitor = P(ShipTraitStat.Capacitor);
            var resistance = P(ShipTraitStat.Resistance);

            var s = step.Clone();
            s.Dmg = ScaleKeepingOne(step.Dmg, damage);
            s.DamageThermal = Scale(step.DamageThermal, damage);
            s.DamageEM = Scale(step.DamageEM, damage);
            s.DamageExplosive = Scale(step.DamageExplosive, damage);
            s.DamageAll = Scale(step.DamageAll, damage);
            s.Accuracy = Scale(step.Accuracy, accuracy);
            s.ModuleAccuracy = Scale(step.ModuleAccuracy, accuracy);
            s.AuraAccuracy = Scale(step.AuraAccuracy, accuracy);
            s.CapacitorCost = CapacitorCost(step.CapacitorCost, traits);
            s.Hull = Scale(step.Hull, P(ShipTraitStat.Hull));
            s.Shield = Scale(step.Shield, shield);
            s.AuraShieldPercent = Scale(step.AuraShieldPercent, shield);
            s.Repair = ScaleKeepingOne(step.Repair, P(ShipTraitStat.Repair));
            s.MaxCapacitor = Scale(step.MaxCapacitor, capacitor);
            s.CapacitorRestore = Scale(step.CapacitorRestore, capacitor);
            s.CapacitorRegen = Math.Round(step.CapacitorRegen * (100 + P(ShipTraitStat.CapacitorRegen)) / 100.0, 3);
            s.ModuleDefense = Scale(step.ModuleDefense, P(ShipTraitStat.Defense));
            s.ResistAll = Scale(step.ResistAll, resistance);
            s.ResistThermal = Scale(step.ResistThermal, resistance);
            s.ResistEM = Scale(step.ResistEM, resistance);
            s.ResistExplosive = Scale(step.ResistExplosive, resistance);
            return s;
        }

        /// <summary>A module's capacitor use per cycle after its traits, never below 0.</summary>
        public static int CapacitorCost(int cost, IEnumerable<ShipComponentTraitType> traits) =>
            Math.Max(0, Scale(cost, Sum(traits, ShipTraitStat.CapacitorCost)));

        /// <summary>A module's Module Bonus after its Yield traits, never below 0.</summary>
        public static int ModuleBonus(int moduleBonus, IEnumerable<ShipComponentTraitType> traits) =>
            Math.Max(0, moduleBonus + Sum(traits, ShipTraitStat.Yield));

        // Hull traits

        /// <summary>Whether a hull applies the stat: the module numbers (capacitor use, repair, Yield, rare find) it does not.</summary>
        public static bool IsHullStat(ShipTraitStat stat) => RevampComponentCatalog.HullTraitStats.Contains(stat);

        /// <summary>
        /// What a hull's traits add to one stat: <see cref="RevampComponentCatalog.HullTraitSharePercent"/> of the summed
        /// value, rounded half away from zero; 0 for a stat a hull does not apply.
        /// </summary>
        public static int HullSum(IEnumerable<ShipComponentTraitType> traits, ShipTraitStat stat)
        {
            if (!IsHullStat(stat))
                return 0;

            var share = Sum(traits, stat) * RevampComponentCatalog.HullTraitSharePercent / 100.0;
            return (int)Math.Round(share, MidpointRounding.AwayFromZero);
        }

        /// <summary>One trait's effects on a hull as players read them, for example "+3% hull, +3% resistances, -1% speed".</summary>
        public static string DescribeHullEffects(ShipComponentTrait trait)
        {
            var parts = new List<string>();
            foreach (var effect in trait.Effects)
            {
                var value = HullSum(new[] { trait.Type }, effect.Stat);
                if (value == 0)
                    continue;

                var unit = effect.Stat is ShipTraitStat.Evasion ? string.Empty : "%";
                parts.Add($"{(value > 0 ? "+" : string.Empty)}{value}{unit} {HullStatText(effect.Stat)}");
            }

            return parts.Count == 0 ? "no effect on a hull" : string.Join(", ", parts);
        }

        private static string HullStatText(ShipTraitStat stat) => stat switch
        {
            ShipTraitStat.Damage => "weapon damage",
            ShipTraitStat.Accuracy => "Accuracy",
            ShipTraitStat.Hull => "max hull",
            ShipTraitStat.Shield => "max shield",
            ShipTraitStat.Capacitor => "max capacitor",
            ShipTraitStat.CapacitorRegen => "capacitor regen",
            ShipTraitStat.Defense => "Defense",
            ShipTraitStat.Resistance => "resistances",
            ShipTraitStat.Evasion => "Evasion",
            ShipTraitStat.Speed => "speed",
            _ => stat.ToString(),
        };

        /// <summary>One examine line per hull trait, with what it does on the hull.</summary>
        public static List<string> DescribeHullLines(IEnumerable<ShipComponentTraitType> traits) =>
            (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Select(Get).Where(x => x != null)
                .Select(x => $"{x.Name}: {DescribeHullEffects(x)}")
                .ToList();

        // Item storage

        /// <summary>Reads the stored trait numbers; unknown numbers are dropped.</summary>
        public static List<ShipComponentTraitType> Parse(string stored)
        {
            var traits = new List<ShipComponentTraitType>();
            if (string.IsNullOrWhiteSpace(stored))
                return traits;

            foreach (var part in stored.Split(','))
            {
                if (int.TryParse(part.Trim(), out var id) && _traits.ContainsKey((ShipComponentTraitType)id))
                    traits.Add((ShipComponentTraitType)id);
            }

            return traits;
        }

        public static string Format(IEnumerable<ShipComponentTraitType> traits) =>
            string.Join(",", (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Where(x => x != ShipComponentTraitType.None).Select(x => ((int)x).ToString()));

        /// <summary>The item's name with its traits, for example "Laser Cannon Mk III [Focused]".</summary>
        public static string DecoratedName(string baseName, IEnumerable<ShipComponentTraitType> traits)
        {
            var names = (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Select(Get).Where(x => x != null).Select(x => x.Name).ToList();
            return names.Count == 0 ? baseName : $"{baseName} [{string.Join(", ", names)}]";
        }

        /// <summary>
        /// A fitted module's tooltip in Ship Management: its name with its traits, then one line per trait, for example
        /// "Laser Cannon Mk III [Focused]" and "Focused: +6% Accuracy, -3% DMG".
        /// </summary>
        public static string ModuleTooltip(string name, IEnumerable<ShipComponentTraitType> traits)
        {
            var lines = DescribeLines(traits);
            return lines.Count == 0
                ? name
                : DecoratedName(name, traits) + "\n" + string.Join("\n", lines);
        }

        /// <summary>Ship Management's hull trait line: "Hull traits: Reinforced, Hutt-rigged", or "Hull traits: none".</summary>
        public static string HullTraitsText(IEnumerable<ShipComponentTraitType> traits)
        {
            var names = (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Select(Get).Where(x => x != null).Select(x => x.Name).ToList();
            return names.Count == 0 ? "Hull traits: none" : $"Hull traits: {string.Join(", ", names)}";
        }

        /// <summary>The Ship HUD's Hull Traits value: the hull's trait names, or "None" (W11k).</summary>
        public static string HullTraitNames(IEnumerable<ShipComponentTraitType> traits)
        {
            var names = (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Select(Get).Where(x => x != null).Select(x => x.Name).ToList();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }

        /// <summary>Ship Management's hull trait tooltip: what each trait does on the hull, one per line.</summary>
        public static string HullTraitsTooltip(IEnumerable<ShipComponentTraitType> traits)
        {
            var lines = DescribeHullLines(traits);
            return lines.Count == 0
                ? "A hull built from traited components inherits their traits at half value."
                : string.Join("\n", lines);
        }

        /// <summary>One examine line per trait, for example "Focused: +6% Accuracy, -3% DMG".</summary>
        public static List<string> DescribeLines(IEnumerable<ShipComponentTraitType> traits) =>
            (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Select(Get).Where(x => x != null)
                .Select(x => $"{x.Name}{(x.Kind == ShipTraitKind.Rare ? " (rare)" : string.Empty)}: {x.Description}")
                .ToList();

        // Salvage

        /// <summary>The chance per salvage cycle (0-100) that a wreck also yields one salvaged component.</summary>
        public static int SalvagedComponentPercent(WreckDifficulty difficulty) =>
            RevampComponentCatalog.SalvagedComponentPercent.TryGetValue(difficulty, out var percent) ? percent : 0;

        /// <summary>The chance (0-100) a salvaged component's trait is rare: the wreck's chance plus Trait Hunter's points.</summary>
        public static int RareTraitPercent(WreckDifficulty difficulty, int traitHunterBonus)
        {
            var basePercent = RevampComponentCatalog.RareTraitPercent.TryGetValue(difficulty, out var percent) ? percent : 0;
            return Math.Clamp(basePercent + Math.Max(0, traitHunterBonus), 0, 100);
        }

        /// <summary>The traits of one kind a family's components can carry.</summary>
        public static List<ShipComponentTrait> Pool(ShipComponentFamily family, ShipTraitKind kind) =>
            RevampComponentCatalog.Traits.Where(x => x.Kind == kind && x.Families.Contains(family)).ToList();

        /// <summary>
        /// Rolls one salvage cycle's salvaged component. <paramref name="next"/> returns a number from 0 to its
        /// argument minus 1. Returns null when the cycle yields none. The component is of the wreck's tier (tier 6
        /// wrecks give tier 5) and a random salvage family, and always carries a trait: a rare one at
        /// <see cref="RareTraitPercent"/>, otherwise one of the family's common traits.
        /// </summary>
        public static (string Resref, ShipComponentTraitType Trait)? RollSalvagedComponent(
            int wreckTier,
            WreckDifficulty difficulty,
            int traitHunterBonus,
            Func<int, int> next)
        {
            if (next(100) >= SalvagedComponentPercent(difficulty))
                return null;

            var families = RevampComponentCatalog.SalvageFamilies;
            var family = families[next(families.Count)];
            var rare = next(100) < RareTraitPercent(difficulty, traitHunterBonus);
            var pool = Pool(family, rare ? ShipTraitKind.Rare : ShipTraitKind.Common);
            if (pool.Count == 0)
                pool = Pool(family, ShipTraitKind.Common);

            return (ComponentResref(family, wreckTier), pool[next(pool.Count)].Type);
        }
    }
}

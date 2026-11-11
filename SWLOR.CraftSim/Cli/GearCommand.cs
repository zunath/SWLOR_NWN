using SWLOR.CraftSim.Data;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>The <c>gear</c> command: prints the gear levels derived from the real crafting gear.</summary>
    public static class GearCommand
    {
        public static int Run(string[] args, TextWriter output)
        {
            var options = CommandLineOptions.Parse(args, new[] { "--repo" });
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var recipes = RecipeCatalog.Load();
            var gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);

            foreach (var skill in CraftingGearCatalog.GearSkills)
            {
                output.WriteLine($"{skill}: {gear.TierCount(skill)} tiers");
                for (var tier = 1; tier <= gear.TierCount(skill); tier++)
                {
                    var set = gear.Tier(skill, tier);
                    output.WriteLine($"  tier {tier}: Control {set.Control}, Craftsmanship {set.Craftsmanship}, CP {set.CpBonus}  [{string.Join(", ", set.Items.Select(i => i.Resref))}]");
                }

                output.WriteLine("  levels: " + string.Join("; ", gear.Levels(skill)));
            }

            foreach (var note in gear.Notes)
                output.WriteLine($"note: {note}");

            return 0;
        }
    }
}

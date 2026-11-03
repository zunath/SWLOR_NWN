using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    public static class GatheringBaseline
    {
        // A fixed node level and rank: the item counts do not depend on either.
        private const int ProbeNodeLevel = 3;
        private const int ProbeRank = 20;

        public static GatheringBaselineResults Run(GatheringBaselineOptions options)
        {
            var harvest = new List<HarvestRow>();
            foreach (var might in options.Attributes.Modifiers)
            {
                double harvests = 0, items = 0;
                for (var trial = 0; trial < options.Trials; trial++)
                {
                    var rng = SimRandom.ForRun(options.Seed, 1, might + 100, trial);
                    var node = HarvestModel.HarvestNode(rng, ProbeNodeLevel, ProbeRank, might);
                    harvests += node.Harvests;
                    items += node.Items;
                }

                harvest.Add(new HarvestRow(
                    might,
                    HarvestModel.BonusItemChance(might),
                    HarvestModel.ExpectedHarvestsPerNode,
                    HarvestModel.ExpectedItemsPerNode(might),
                    harvests / options.Trials,
                    items / options.Trials));
            }

            var scavenge = new List<ScavengeRow>();
            for (var hardLook = 0; hardLook <= ScavengeModel.MaximumHardLook; hardLook++)
            {
                foreach (var perception in options.Attributes.Modifiers)
                {
                    double attempts = 0, successes = 0;
                    for (var trial = 0; trial < options.Trials; trial++)
                    {
                        var rng = SimRandom.ForRun(options.Seed, 2, hardLook, perception + 100, trial);
                        var site = ScavengeModel.SearchSite(rng, ProbeNodeLevel, ProbeRank, hardLook, perception);
                        attempts += site.Attempts;
                        successes += site.Successes;
                    }

                    scavenge.Add(new ScavengeRow(
                        hardLook,
                        perception,
                        ScavengeModel.SecondAttemptChance(hardLook, perception),
                        ScavengeModel.ExpectedAttemptsPerSite(hardLook, perception),
                        ScavengeModel.ExpectedSuccessesPerSite(hardLook, perception),
                        attempts / options.Trials,
                        successes / options.Trials));
                }
            }

            return new GatheringBaselineResults { Trials = options.Trials, Seed = options.Seed, Harvest = harvest, Scavenge = scavenge };
        }
    }
}

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// The timings and play choices the progression-pace model uses. Seconds per action and the harvester, refinery and scavenge
    /// delays come from the game server; the per-craft overhead, node travel time and hand-in time are unmeasured estimates that
    /// the client evidence replaces.
    /// </summary>
    public sealed record ProgressionAssumptions
    {
        /// <summary>Highest rank any crafting skill reaches; the model plays ranks 0 up to this.</summary>
        public const int MaximumRank = 50;

        /// <summary>RP XP a character earns per hour with no materials: 1,500 base XP every 30 minutes (RoleplayXP.cs:16-18, 90).</summary>
        public const int RoleplayXpPerHour = 3000;

        /// <summary>Seconds per action in the old craft window (the legacy baseline's estimate, <c>GateThresholds.LegacySecondsPerAction</c>).</summary>
        public double SecondsPerAction { get; init; } = 6.0;

        /// <summary>
        /// Seconds per craft besides its actions: open the station, pick the recipe, commit the components and collect the item.
        /// An unmeasured estimate; the gates' own overhead defaults to 0.
        /// </summary>
        public double OverheadSecondsPerCraft { get; init; } = 30.0;

        /// <summary>
        /// Seconds from finishing one resource node to starting the next (walking, finding a free node). An unmeasured estimate.
        /// Respawn waits and contention for nodes are not modelled, so this is a lower bound on real gathering.
        /// </summary>
        public double NodeTravelSeconds { get; init; } = 30.0;

        /// <summary>Seconds per harvester use: the item's <c>Delay(5f)</c> (HarvesterItemDefinition.cs).</summary>
        public double SecondsPerHarvest { get; init; } = 5.0;

        /// <summary>Seconds of search animation per scavenge attempt (ScavengePoint.cs).</summary>
        public double SecondsPerScavengeAttempt { get; init; } = 2.0;

        /// <summary>Seconds one refining operation takes (RefineryViewModel.cs <c>RefiningDelaySeconds</c>).</summary>
        public double RefineSecondsPerBatch { get; init; } = 6.0;

        /// <summary>
        /// Units one refining operation handles: a full stack, the 99 that baseitems.2da allows for the material base items (526, 536, 538).
        /// The refinery takes several stacks per operation, so this is the cautious reading.
        /// </summary>
        public int RefineBatchUnits { get; init; } = 99;

        /// <summary>The Might modifier of the gatherer: each point adds a 25% chance of a free second item per harvest.</summary>
        public int MightModifier { get; init; }

        /// <summary>
        /// The gatherer's Gathering tier, which sets the Hard Look level (the chance of a second search at a scavenge site). The progression
        /// model sets it to the tier of the crafter's own rank, as if their Gathering rank followed their crafting rank; null uses the site's level.
        /// </summary>
        public int? GatheringTier { get; init; }

        /// <summary>The Perception modifier of the scavenger.</summary>
        public int PerceptionModifier { get; init; }

        /// <summary>
        /// Seconds a component unit costs when no node supplies it (leather and electronics from enemies or crates, meat, crafted parts,
        /// vendor goods). Zero leaves those units out of the hours and counts them separately.
        /// </summary>
        public double UnmodelledUnitSeconds { get; init; }

        /// <summary>When true the crafter picks the cheapest quarter of the level's recipes by gathering time instead of rotating through all of them.</summary>
        public bool CheapestQuartileRecipes { get; init; }

        /// <summary>
        /// When true the crafter rotates through the level's distinct recipes, so the first successful craft of each one pays the 20% first-craft bonus.
        /// </summary>
        public bool VarietyPlay { get; init; }

        /// <summary>
        /// A crafting-only multiplier on the XP crafts and guild hand-ins pay (a proposal, not today's game). The XP chart's cutoff is not
        /// moved: a craft at least five levels below the rank still pays nothing.
        /// </summary>
        public double CraftXpMultiplier { get; init; } = 1.0;

        /// <summary>
        /// A crafting-only ceiling on the XP any one rank costs (a proposal, not today's game); null leaves the table as it is. It
        /// flattens the late ranks, which cost up to 16,000 XP each, without changing what a craft pays.
        /// </summary>
        public int? RequiredXpCap { get; init; }

        /// <summary>
        /// Hours of play between server starts, which is how often the guild boards are redrawn. A catch-up whose board holds no task it can use
        /// waits for the next start, and the crafts made meanwhile cannot be handed in. An unmeasured estimate.
        /// </summary>
        public double PlayHoursPerServerStart { get; init; } = 4.0;

        /// <summary>Seconds one guild hand-in costs besides the craft: accepting the task and giving the report. An unmeasured estimate.</summary>
        public double HandInSeconds { get; init; } = 10.0;

        /// <summary>
        /// Seconds from one fishing bite to the next: cast, wait and reel. An unmeasured estimate; the plan's fishing XP cohort measures it
        /// in the client. Every bite pays XP whether or not the fish is landed.
        /// </summary>
        public double FishingSecondsPerBite { get; init; } = 30.0;

        /// <summary>The fisher's Willpower score; each point adds 2 percentage points to the catch chance (Fishing.cs). 10 is a modifier of 0.</summary>
        public int WillpowerScore { get; init; } = 10;

        /// <summary>Trials in a research experiment (<c>ExperimentRules.Trials</c>).</summary>
        public int ResearchTrials { get; init; } = SWLOR.Game.Server.Service.CraftService.Minigame.Experiment.ExperimentRules.Trials;

        /// <summary>Seconds one experiment trial takes, as an action in the craft window does.</summary>
        public double ResearchSecondsPerTrial { get; init; } = 6.0;

        /// <summary>Seconds at the research terminal to start a job (choose the recipe, confirm the cost) and again to collect it. An unmeasured estimate.</summary>
        public double ResearchVisitSeconds { get; init; } = 30.0;
    }
}

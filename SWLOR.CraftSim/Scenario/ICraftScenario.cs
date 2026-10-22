using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario
{
    /// <summary>
    /// What a crafting minigame gives the simulator so every balance gate can drive it: a way to build one of its engines for a
    /// recipe, rank, gear tier and style, and the inputs the gates compare against. The gates, the adaptive player and the
    /// fixed-rotation solver are generic over <see cref="ICraftMinigameEngine"/>; a scenario never writes its own solver.
    /// <para>
    /// To add one, implement this interface in a public class with a public parameterless constructor (the <c>gates</c> command
    /// finds it by reflection and runs it by <see cref="Name"/>). Everything but the first five members has a default.
    /// Members are called from several threads at once, so a scenario keeps no mutable state.
    /// </para>
    /// </summary>
    public interface ICraftScenario
    {
        /// <summary>The short name the command line takes, such as <c>forge</c>.</summary>
        string Name { get; }

        /// <summary>The name in the report, such as "Plasma Forge".</summary>
        string Title { get; }

        /// <summary>The crafting skill whose recipes the scenario plays.</summary>
        SkillType Skill { get; }

        CraftMinigameType Minigame { get; }

        /// <summary>
        /// The player's seconds per action in this minigame's window, including reading the previews: the input that turns actions
        /// into minutes for the XP-rate gate. It is an estimate until the client evidence measures it.
        /// </summary>
        double SecondsPerAction { get; }

        /// <summary>
        /// A new engine for one attempt: the recipe's parameters (<see cref="CraftEngineRequest.Recipe"/>), the crafter's rank, the
        /// gear worn (Control powers quality, Craftsmanship progress, CP bonus the action budget) and the style, built on
        /// <see cref="CraftEngineRequest.Random"/>. It must return an attempt that has not started, with no perks unless a style says so.
        /// </summary>
        ICraftMinigameEngine CreateEngine(CraftEngineRequest request);

        /// <summary>
        /// What the minigame multiplies a craft's base XP by, as its route declares it in the game. 1 by default; a minigame whose XP per
        /// hour fell short of the old window's raises it, and the XP-rate gate then plays the new loop with it.
        /// </summary>
        double XpMultiplier => 1.0;

        /// <summary>
        /// The multiplier on the base XP of a craft of a recipe of the given level: <see cref="XpMultiplier"/> by default. A minigame whose
        /// need differs by level (the old window's crafts are short at low levels and long at high ones) declares a multiplier per level
        /// band in the game's <c>CraftMinigameXp</c> and returns it here.
        /// </summary>
        double XpMultiplierFor(int recipeLevel) => XpMultiplier;

        /// <summary>Which recipes the gates play and at which ranks. By default every recipe of <see cref="Skill"/>, at its own level up to rank 50.</summary>
        ScenarioCohort Cohort => ScenarioCohort.ForSkill(Skill);

        /// <summary>The actions per attempt the gates expect. By default an item craft at level: 10-20.</summary>
        LengthTarget LengthTarget => LengthTarget.ItemCraft;

        /// <summary>The alternative styles to compare. None by default, which makes the style-balance gate not applicable.</summary>
        IReadOnlyList<CraftStylePair> StylePairs => Array.Empty<CraftStylePair>();

        /// <summary>How the adaptive player searches: depth, samples per outcome, node budget and leaf heuristic.</summary>
        AdaptivePolicyOptions AdaptiveOptions => AdaptivePolicyOptions.Default;

        /// <summary>How hard the fixed-rotation solver searches, for the gate that asks whether one rotation always wins.</summary>
        FixedRotationSolverOptions SolverOptions => FixedRotationSolverOptions.Default;

        /// <summary>
        /// Whether <see cref="CraftEngineRequest.Enhancements"/> reaches the engine, for gate 7 (enhancement parity). By default it does not,
        /// and gate 7 then does not run. Set <see cref="EnhancementSupport.Modeled"/> once <see cref="CreateEngine"/> applies the request's
        /// total progress penalty through the engine's own enhancement handling, or <see cref="EnhancementSupport.NoSlots"/> for a minigame
        /// whose recipes have no enhancement slots.
        /// </summary>
        EnhancementSupport Enhancements => EnhancementSupport.NotModeled;

        /// <summary>What this scenario's results do not cover, added to the report's own limitations.</summary>
        IReadOnlyList<string> Limitations => Array.Empty<string>();
    }
}

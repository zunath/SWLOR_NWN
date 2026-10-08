using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftActionType
    {
        BasicSynthesis,
        RapidSynthesis,
        CarefulSynthesis,
        BasicTouch,
        StandardTouch,
        PreciseTouch,
        MastersMend,
        SteadyHand,
        MuscleMemory,
        Veneration,
        WasteNot
    }

    public sealed record CraftActionDetail(
        CraftActionType Type, string Name, int RequiredSkillRank, int CPCost,
        int DurabilityCost, int SuccessChance, int BaseProgress = 0, int BaseQuality = 0)
    {
        public static IReadOnlyList<CraftActionDetail> LegacyActions { get; } = Array.AsReadOnly(new[]
        {
            new CraftActionDetail(CraftActionType.BasicSynthesis, "Basic Synthesis", 0, 0, 10, 90, BaseProgress: 10),
            new CraftActionDetail(CraftActionType.RapidSynthesis, "Rapid Synthesis", 10, 6, 10, 75, BaseProgress: 30),
            new CraftActionDetail(CraftActionType.CarefulSynthesis, "Careful Synthesis", 30, 10, 10, 50, BaseProgress: 80),
            new CraftActionDetail(CraftActionType.BasicTouch, "Basic Touch", 5, 3, 10, 90, BaseQuality: 10),
            new CraftActionDetail(CraftActionType.StandardTouch, "Standard Touch", 15, 6, 10, 75, BaseQuality: 30),
            new CraftActionDetail(CraftActionType.PreciseTouch, "Precise Touch", 35, 10, 10, 50, BaseQuality: 80),
            new CraftActionDetail(CraftActionType.MastersMend, "Master's Mend", 10, 10, 0, 100),
            new CraftActionDetail(CraftActionType.SteadyHand, "Steady Hand", 20, 12, 0, 100),
            new CraftActionDetail(CraftActionType.MuscleMemory, "Muscle Memory", 40, 12, 0, 100),
            new CraftActionDetail(CraftActionType.Veneration, "Veneration", 25, 8, 10, 100),
            new CraftActionDetail(CraftActionType.WasteNot, "Waste Not", 8, 4, 0, 100)
        });

        // BaseProgress and BaseQuality are percentages of the common stat-driven gain in the new rules.
        public static IReadOnlyList<CraftActionDetail> Actions { get; } = Array.AsReadOnly(new[]
        {
            new CraftActionDetail(CraftActionType.BasicSynthesis, "Basic Synthesis", 0, 0, 10, 100, BaseProgress: 100),
            new CraftActionDetail(CraftActionType.RapidSynthesis, "Rapid Synthesis", 10, 6, 15, 75, BaseProgress: 180),
            new CraftActionDetail(CraftActionType.CarefulSynthesis, "Careful Synthesis", 30, 10, 5, 100, BaseProgress: 120),
            new CraftActionDetail(CraftActionType.BasicTouch, "Basic Touch", 5, 3, 10, 100, BaseQuality: 100),
            new CraftActionDetail(CraftActionType.StandardTouch, "Standard Touch", 15, 7, 10, 100, BaseQuality: 150),
            new CraftActionDetail(CraftActionType.PreciseTouch, "Precise Touch", 35, 10, 5, 100, BaseQuality: 125),
            new CraftActionDetail(CraftActionType.MastersMend, "Master's Mend", 10, 10, 0, 100),
            new CraftActionDetail(CraftActionType.SteadyHand, "Steady Hand", 20, 5, 0, 100),
            new CraftActionDetail(CraftActionType.MuscleMemory, "Muscle Memory", 40, 6, 0, 100),
            new CraftActionDetail(CraftActionType.Veneration, "Veneration", 25, 3, 0, 100),
            new CraftActionDetail(CraftActionType.WasteNot, "Waste Not", 8, 4, 0, 100)
        });

        public static CraftActionDetail Get(CraftActionType type) => System.Linq.Enumerable.First(Actions, action => action.Type == type);

        public static CraftActionDetail GetLegacy(CraftActionType type)
        {
            foreach (var action in LegacyActions)
                if (action.Type == type)
                    return action;
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}

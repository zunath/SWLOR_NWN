using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftRuleEffect
    {
        RestoreWorkDurability, FailedProgressPercent, QualityPercent, NextTouchQualityPercent,
        RestoreSwitchCP, ForecastLength, RefundCPPercent, MendDurability, WasteNotWindow,
        ReduceSynthesisCP, PreventDurabilityFailure, TouchProgressPercent, PreserveMendCondition,
        NextBasicTouchDiscount, MaximumQualityProgress, NextRapidDurabilityDiscount,
        SynthesisQualityPercent, ReplaceFailedConditions
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CraftRuleAttribute : Attribute
    {
        public CraftRuleEffect Effect { get; }
        public int Limit { get; }
        public CraftWorkKind Work { get; set; }
        public CraftWorkKind PreviousWork { get; set; }
        public CraftCondition Condition { get; set; } = CraftCondition.Normal;
        public CraftActionType Action { get; set; } = (CraftActionType)(-1);
        public CraftTechnique Technique { get; set; }
        public bool CategorySwitch { get; set; }
        public bool Failure { get; set; }
        public bool Paid { get; set; }

        public CraftRuleAttribute(CraftRuleEffect effect, int limit = 0) { Effect = effect; Limit = limit; }

        public bool Matches(CraftSession session, CraftActionDetail action)
        {
            var work = action.BaseProgress > 0 ? CraftWorkKind.Synthesis : action.BaseQuality > 0 ? CraftWorkKind.Touch : CraftWorkKind.None;
            return (Work == CraftWorkKind.None || work == Work) &&
                   (Action == (CraftActionType)(-1) || action.Type == Action) &&
                   (Condition == CraftCondition.Normal || session.CurrentCondition == Condition) &&
                   (PreviousWork == CraftWorkKind.None || session.LastWorkSucceeded && session.LastWork == PreviousWork) &&
                   (!CategorySwitch || session.LastWorkSucceeded && work != CraftWorkKind.None && session.LastWork != work) &&
                   (!Paid || action.CPCost > 0) && (Technique == CraftTechnique.None || (session.Technique & Technique) == Technique);
        }

        public static IReadOnlyList<(StatType Stat, CraftRuleAttribute Rule)> All { get; } =
            Array.AsReadOnly(Enum.GetValues<StatType>().Select(stat => (Stat: stat,
                Rule: typeof(StatType).GetField(stat.ToString())?.GetCustomAttribute<CraftRuleAttribute>()))
                .Where(entry => entry.Rule != null).ToArray());
    }
}

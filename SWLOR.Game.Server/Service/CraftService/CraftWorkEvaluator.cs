using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>Condition-based work rules. Preview and resolution share the same pure transition.</summary>
    public static class CraftWorkEvaluator
    {
        private sealed record Transition(CraftSession Next, int CPSpent, int DurabilitySpent, int Progress,
            int Quality, int DurabilityRestored, int CPRestored, bool Protected, string Effects);

        private static CraftWorkKind Kind(CraftActionDetail action) => action.BaseProgress > 0 ? CraftWorkKind.Synthesis
            : action.BaseQuality > 0 ? CraftWorkKind.Touch : CraftWorkKind.None;
        private static int FloorGain(double amount) => Math.Max(0, (int)Math.Floor(amount));
        private static int BonusGain(int gain, int percent) => gain > 0 && percent > 0 ? Math.Max(1, FloorGain(gain * percent / 100d)) : 0;
        private static int Spend(int baseCost, int reduction) => Math.Max(0, (int)Math.Ceiling(baseCost * (100 - Math.Clamp(reduction, 0, 50)) / 100d));
        private static int Count(IReadOnlyDictionary<StatType, int> counts, StatType stat) => counts.TryGetValue(stat, out var count) ? count : 0;
        private static CraftBuffType? Support(CraftActionType action) => action switch
        {
            CraftActionType.SteadyHand => CraftBuffType.SteadyHand,
            CraftActionType.MuscleMemory => CraftBuffType.MuscleMemory,
            CraftActionType.Veneration => CraftBuffType.Veneration,
            CraftActionType.WasteNot => CraftBuffType.WasteNot,
            _ => null
        };
        private static int UseLimit(CraftBuffType buff) => buff == CraftBuffType.SteadyHand || buff == CraftBuffType.MuscleMemory ? 2 : 1;
        private static bool Consumes(CraftBuffType buff, CraftActionDetail action) => buff switch
        {
            CraftBuffType.SteadyHand => action.Type == CraftActionType.RapidSynthesis,
            CraftBuffType.MuscleMemory or CraftBuffType.TouchQuality => Kind(action) == CraftWorkKind.Touch,
            CraftBuffType.Veneration => Kind(action) == CraftWorkKind.Synthesis && action.CPCost > 0,
            CraftBuffType.WasteNot => Kind(action) != CraftWorkKind.None,
            CraftBuffType.BasicTouchDiscount => action.Type == CraftActionType.BasicTouch,
            CraftBuffType.RapidDurabilityDiscount => action.Type == CraftActionType.RapidSynthesis,
            _ => false
        };

        public static CraftActionPreview Preview(CraftSession session, CraftActionType type)
        {
            var action = CraftActionDetail.Get(type);
            var success = Calculate(session, action, true);
            var failure = Calculate(session, action, false);
            var support = Support(type);
            var reason = session.Status != CraftSessionStatus.Active ? "This crafting session has ended."
                : session.SkillRank < action.RequiredSkillRank ? $"Requires skill rank {action.RequiredSkillRank}."
                : session.CP < success.CPSpent ? "Not enough CP."
                : support.HasValue && session.Buff(support.Value) != null ? "This preparation is still active."
                : support.HasValue && session.Used(support.Value) >= UseLimit(support.Value) ? "No uses of this preparation remain."
                : type == CraftActionType.MastersMend && session.Durability == session.MaxDurability ? "Durability is already full."
                : string.Empty;
            var chance = type == CraftActionType.RapidSynthesis && session.Buff(CraftBuffType.SteadyHand) != null ? 100 : action.SuccessChance;
            return new CraftActionPreview(action, reason.Length == 0, reason, success.CPSpent, success.DurabilitySpent,
                success.DurabilityRestored, success.Progress, success.Quality, chance,
                success.Next.Status == CraftSessionStatus.Succeeded, success.Next.Status == CraftSessionStatus.Failed,
                chance < 100 && failure.Next.Status == CraftSessionStatus.Failed)
            {
                RulesVersion = 2, FinishesOnFailure = chance < 100 && failure.Next.Status == CraftSessionStatus.Succeeded,
                CPRestored = success.CPRestored, ProgressOnFailure = failure.Progress,
                DurabilityOnFailure = failure.Next.Durability, FailureProtection = success.Protected || failure.Protected,
                EffectDescription = success.Effects + (failure.Effects.Length > 0 ? $"\nOn failed work: {failure.Effects}" : string.Empty)
            };
        }

        public static CraftActionOutcome Resolve(CraftSession session, CraftActionRequest request, Func<int> rollD100)
        {
            var preview = Preview(session, request.Action);
            if (request.SessionId != session.Id || request.ExpectedActionCount != session.ActionCount)
                return new CraftActionOutcome(session, preview, false, false, "This action belongs to an earlier crafting state.");
            if (!preview.IsAvailable) return new CraftActionOutcome(session, preview, false, false, preview.UnavailableReason);
            var roll = rollD100();
            if (roll < 1 || roll > 100) throw new ArgumentOutOfRangeException(nameof(rollD100));
            var succeeded = roll <= preview.SuccessChance;
            var next = Calculate(session, preview.Action, succeeded).Next;
            return new CraftActionOutcome(next, preview, true, succeeded, string.Empty);
        }

        public static int UnconditionedProgress(CraftSession session)
        {
            var delta = session.SkillRank - session.RecipeLevel;
            return FloorGain((10 + (session.SkillRank >= 20 ? 21 : 0) + session.Craftsmanship * 0.65d) * Math.Max(0, 1 + 0.05d * delta));
        }
        public static int UnconditionedQuality(CraftSession session)
        {
            var delta = session.SkillRank - session.RecipeLevel;
            return FloorGain((10 + (session.SkillRank >= 40 ? 70 : 0) + session.Control * 0.75d) * (delta < 0 ? Math.Max(0, 1 + 0.05d * delta) : 1));
        }

        private static Transition Calculate(CraftSession session, CraftActionDetail action, bool succeeded)
        {
            var kind = Kind(action);
            var rules = CraftRuleAttribute.All.Where(entry => session.CraftingStat(entry.Stat) > 0 && entry.Rule.Matches(session, action) &&
                (entry.Rule.Limit == 0 || session.TriggerCount(entry.Stat) < entry.Rule.Limit)).ToArray();
            var counts = new Dictionary<StatType, int>(session.TriggerCounts);
            var buffs = new Dictionary<CraftBuffType, CraftBuff>();
            var uses = new Dictionary<CraftBuffType, int>(session.SupportUses);
            var effects = new List<string>();
            void Trigger(StatType stat) { counts[stat] = Count(counts, stat) + 1; }
            void Grant(CraftBuffType buff, int magnitude) { buffs[buff] = new CraftBuff(1, 2, magnitude); }
            // Every old preparation ages; the activating action never ages the preparation it creates.
            foreach (var (type, buff) in session.Buffs)
            {
                var charges = buff.Charges - (Consumes(type, action) ? 1 : 0);
                if (charges > 0 && buff.ActionsRemaining > 1) buffs[type] = buff with { Charges = charges, ActionsRemaining = buff.ActionsRemaining - 1 };
            }

            var cpBase = action.CPCost;
            var durabilityBase = action.DurabilityCost;
            if (action.Type == CraftActionType.BasicTouch && session.Buff(CraftBuffType.BasicTouchDiscount) is { } touchDiscount)
                cpBase = Math.Max(1, cpBase - touchDiscount.Magnitude);
            if (action.Type == CraftActionType.RapidSynthesis && session.Buff(CraftBuffType.RapidDurabilityDiscount) is { } rapidDiscount)
                durabilityBase = Math.Max(0, durabilityBase - rapidDiscount.Magnitude);
            foreach (var (stat, rule) in rules.Where(entry => entry.Rule.Effect == CraftRuleEffect.ReduceSynthesisCP))
            {
                cpBase = Math.Max(1, cpBase - session.CraftingStat(stat)); Trigger(stat);
            }
            if (session.Profile == CraftProfile.Sturdy && kind == CraftWorkKind.Synthesis) durabilityBase = (int)Math.Ceiling(durabilityBase * 0.75);
            if (session.Profile == CraftProfile.Delicate && action.Type == CraftActionType.RapidSynthesis) durabilityBase += 5;
            var cpDiscount = session.CurrentCondition == CraftCondition.Economical ? 25 : 0;
            if (kind == CraftWorkKind.Synthesis && action.CPCost > 0 && session.Buff(CraftBuffType.Veneration) != null) cpDiscount += 50;
            var durabilityDiscount = kind != CraftWorkKind.None && session.CurrentCondition == CraftCondition.Reinforced ? 50 : 0;
            if (kind != CraftWorkKind.None && session.Buff(CraftBuffType.WasteNot) != null) durabilityDiscount += 50;
            var cpSpent = Spend(cpBase, cpDiscount);
            var durabilitySpent = Spend(durabilityBase, durabilityDiscount);
            var progressFactor = action.BaseProgress / 100d;
            var qualityFactor = action.BaseQuality / 100d;
            if (session.CurrentCondition == CraftCondition.Workable) progressFactor *= 1.25;
            if (session.CurrentCondition == CraftCondition.Fine) qualityFactor *= 1.5;
            if (session.Profile == CraftProfile.Sturdy) qualityFactor *= 0.9;
            if (session.Profile == CraftProfile.Delicate) qualityFactor *= 1.1;
            if (session.Profile == CraftProfile.Calibrated && session.LastWorkSucceeded && kind != CraftWorkKind.None && kind != session.LastWork)
            { progressFactor *= 1.2; qualityFactor *= 1.2; }
            if (action.Type == CraftActionType.PreciseTouch && session.CurrentCondition == CraftCondition.Fine) qualityFactor *= 1.15;
            if (kind == CraftWorkKind.Touch)
            {
                if (session.Buff(CraftBuffType.MuscleMemory) is { } muscle) qualityFactor *= 1 + muscle.Magnitude / 100d;
                if (session.Buff(CraftBuffType.TouchQuality) is { } finish) qualityFactor *= 1 + finish.Magnitude / 100d;
            }
            foreach (var (stat, rule) in rules.Where(entry => entry.Rule.Effect == CraftRuleEffect.QualityPercent))
            {
                qualityFactor *= 1 + session.CraftingStat(stat) / 100d;
                if (succeeded && session.Quality < session.MaxQuality) Trigger(stat);
            }
            var successProgress = FloorGain(UnconditionedProgress(session) * progressFactor);
            var progress = succeeded ? successProgress : 0;
            var quality = succeeded ? FloorGain(UnconditionedQuality(session) * qualityFactor) : 0;
            var restoration = succeeded && action.Type == CraftActionType.MastersMend ? 30 : 0;
            var cpRestoration = 0;
            var hold = false;
            var replace = false;
            var protection = false;
            // Stat metadata selects triggers; generic outcomes own arithmetic and resource caps.
            foreach (var (stat, rule) in rules)
            {
                var value = session.CraftingStat(stat);
                if (rule.Failure == succeeded) continue;
                var triggered = false;
                switch (rule.Effect)
                {
                    case CraftRuleEffect.RestoreWorkDurability:
                        if (kind != CraftWorkKind.None && succeeded) { restoration += Math.Min(value, durabilitySpent); triggered = durabilitySpent > 0; }
                        break;
                    case CraftRuleEffect.FailedProgressPercent:
                        progress += BonusGain(Math.Min(successProgress, session.MaxProgress - session.Progress), value); triggered = true; break;
                    case CraftRuleEffect.NextTouchQualityPercent:
                        Grant(CraftBuffType.TouchQuality, value); effects.Add($"Next touch: +{value}% quality within 2 actions."); triggered = true; break;
                    case CraftRuleEffect.RestoreSwitchCP:
                        cpRestoration += value; triggered = true; break;
                    case CraftRuleEffect.RefundCPPercent:
                        if (kind != CraftWorkKind.None && cpSpent > 0) { cpRestoration += Math.Min(6, cpSpent * value / 100); triggered = true; } break;
                    case CraftRuleEffect.MendDurability:
                        restoration += value; triggered = true; break;
                    case CraftRuleEffect.TouchProgressPercent:
                        if (quality > 0 && session.Quality < session.MaxQuality) { progress += BonusGain(UnconditionedProgress(session), value); triggered = true; } break;
                    case CraftRuleEffect.PreserveMendCondition:
                        if (session.TriggerCount(stat) < value && Math.Min(restoration, session.MaxDurability - session.Durability) >= 10)
                        { hold = true; effects.Add("Preserves the current condition for the next action."); triggered = true; } break;
                    case CraftRuleEffect.NextBasicTouchDiscount:
                        Grant(CraftBuffType.BasicTouchDiscount, value); effects.Add($"Next Basic Touch: {value} less CP within 2 actions."); triggered = true; break;
                    case CraftRuleEffect.MaximumQualityProgress:
                        if (quality > 0 && session.Quality < session.MaxQuality && session.Quality + quality >= session.MaxQuality)
                        { progress += UnconditionedProgress(session); triggered = true; } break;
                    case CraftRuleEffect.NextRapidDurabilityDiscount:
                        Grant(CraftBuffType.RapidDurabilityDiscount, value); effects.Add($"Next Rapid Synthesis: {value} less durability within 2 actions."); triggered = true; break;
                    case CraftRuleEffect.SynthesisQualityPercent:
                        quality += BonusGain(UnconditionedQuality(session), value); triggered = true; break;
                    case CraftRuleEffect.ReplaceFailedConditions:
                        replace = true; break;
                }
                if (triggered) Trigger(stat);
            }
            if (succeeded && Support(action.Type) is { } support)
            {
                var extra = support == CraftBuffType.WasteNot ? session.CraftingStat(StatType.CraftingWasteNotWindowBonus) : 0;
                var charges = support == CraftBuffType.Veneration || support == CraftBuffType.WasteNot ? 2 + extra : 1;
                var duration = support == CraftBuffType.Veneration || support == CraftBuffType.WasteNot ? 3 + extra : 2;
                buffs[support] = new CraftBuff(charges, duration, support == CraftBuffType.MuscleMemory ? 100 : 0);
                uses[support] = session.Used(support) + 1;
                effects.Add($"Preparation: {charges} charge(s), expires after {duration} accepted actions. {UseLimit(support) - uses[support]} use(s) remain.");
            }
            progress = Math.Clamp(progress, 0, Math.Max(0, session.MaxProgress - session.Progress));
            quality = Math.Clamp(quality, 0, Math.Max(0, session.MaxQuality - session.Quality));
            restoration = Math.Clamp(restoration, 0, session.MaxDurability - Math.Max(0, session.Durability - durabilitySpent));
            cpRestoration = Math.Clamp(cpRestoration, 0, session.MaxCP - Math.Max(0, session.CP - cpSpent));
            var durability = Math.Max(0, session.Durability - durabilitySpent) + restoration;
            var status = session.Progress + progress >= session.MaxProgress ? CraftSessionStatus.Succeeded
                : durability <= 0 ? CraftSessionStatus.Failed : CraftSessionStatus.Active;
            if (status == CraftSessionStatus.Failed && kind != CraftWorkKind.None)
            {
                foreach (var (stat, rule) in rules.Where(entry => entry.Rule.Effect == CraftRuleEffect.PreventDurabilityFailure))
                { durability = 1; protection = true; status = CraftSessionStatus.Active; Trigger(stat); break; }
            }
            var index = session.ConditionIndex + (hold ? 0 : 1);
            var conditions = session.Conditions;
            if (replace && status == CraftSessionStatus.Active)
            {
                var replacement = conditions.ToArray();
                if (index + 1 < replacement.Length)
                {
                    replacement[index] = CraftCondition.Reinforced; replacement[index + 1] = CraftCondition.Economical;
                    conditions = Array.AsReadOnly(replacement);
                    foreach (var (stat, rule) in rules.Where(entry => entry.Rule.Effect == CraftRuleEffect.ReplaceFailedConditions)) Trigger(stat);
                    effects.Add("Next conditions become Reinforced, then Economical.");
                }
            }
            var next = session with
            {
                CP = Math.Max(0, session.CP - cpSpent) + cpRestoration, Durability = durability,
                Progress = session.Progress + progress, Quality = session.Quality + quality,
                ActionCount = session.ActionCount + 1, Status = status, Buffs = buffs, SupportUses = uses,
                TriggerCounts = counts, Conditions = conditions, ConditionIndex = index,
                LastWork = kind != CraftWorkKind.None ? kind : session.LastWork,
                LastWorkSucceeded = kind != CraftWorkKind.None ? succeeded : session.LastWorkSucceeded
            };
            return new Transition(next, cpSpent, durabilitySpent, progress, quality, restoration, cpRestoration, protection, string.Join("\n", effects));
        }
    }
}

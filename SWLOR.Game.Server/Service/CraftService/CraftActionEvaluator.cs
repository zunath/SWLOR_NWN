using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public sealed record CraftActionPreview(
        CraftActionDetail Action, bool IsAvailable, string UnavailableReason,
        int CPCost, int DurabilityCost, int DurabilityRestored,
        int ProgressGain, int QualityGain, int SuccessChance,
        bool FinishesOnSuccess, bool FailsOnSuccess, bool FailsOnFailure)
    {
        public bool FinishesOnFailure { get; init; }
        public int CPRestored { get; init; }
        public int ProgressOnFailure { get; init; }
        public int DurabilityOnFailure { get; init; }
        public bool FailureProtection { get; init; }
        public string EffectDescription { get; init; }
        public int RulesVersion { get; init; } = 1;
        public string ButtonText => $"{Action.Name} [{CPCost}]";

        public string Description
        {
            get
            {
                var lines = new List<string>
                {
                    $"CP: {CPCost}. Durability spent: {DurabilityCost}. Success: {SuccessChance}%.",
                    $"On success: +{ProgressGain} progress, +{QualityGain} quality, +{DurabilityRestored} durability."
                };
                if (RulesVersion == 2)
                {
                    lines.Add($"On success: +{CPRestored} CP. On failure: +{ProgressOnFailure} progress.");
                    if (FailureProtection) lines.Add("Once per craft, durability exhaustion leaves 1 durability when work would otherwise fail.");
                    if (!string.IsNullOrEmpty(EffectDescription)) lines.Add(EffectDescription);
                }
                if (RulesVersion == 1) switch (Action.Type)
                {
                    case CraftActionType.SteadyHand:
                        lines.Add("Guarantees the next synthesis. Consumed on success; does not expire."); break;
                    case CraftActionType.MuscleMemory:
                        lines.Add("Guarantees the next touch. Consumed on success; does not expire."); break;
                    case CraftActionType.Veneration:
                        lines.Add("Halves CP cost of the next four paid synthesis attempts. Other actions do not consume charges."); break;
                    case CraftActionType.WasteNot:
                        lines.Add("Halves durability spent by the next four durability-spending actions, including Veneration."); break;
                }
                if (FinishesOnSuccess)
                    lines.Add(SuccessChance == 100 ? "Finishes the item." : "Finishes the item on success.");
                if (FinishesOnFailure) lines.Add("Even failed work grants enough progress to finish the item.");
                if (FailsOnSuccess)
                    lines.Add("Even on success, this action exhausts durability and fails the craft.");
                if (FailsOnFailure)
                    lines.Add(SuccessChance == 100 ? "" : "A failed action exhausts durability and fails the craft.");
                if (!IsAvailable)
                    lines.Add(UnavailableReason);
                return string.Join("\n", lines);
            }
        }
    }

    public sealed record CraftActionOutcome(
        CraftSession Session, CraftActionPreview Preview, bool Accepted, bool Succeeded, string Reason);

    /// <summary>Legacy manual rules with no engine, inventory, persistence, or GUI dependencies.</summary>
    public static class CraftActionEvaluator
    {
        public static CraftActionPreview Preview(CraftSession session, CraftActionType type)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (session.RulesVersion == 2) return CraftWorkEvaluator.Preview(session, type);
            var action = CraftActionDetail.GetLegacy(type);
            var cp = action.CPCost;
            if (action.BaseProgress > 0 && cp > 0 && session.VenerationCharges > 0)
                cp /= 2;
            var durability = session.WasteNotCharges > 0 ? action.DurabilityCost / 2 : action.DurabilityCost;
            var chance = action.BaseProgress > 0 && session.SteadyHandActive ||
                         action.BaseQuality > 0 && session.MuscleMemoryActive ? 100 : action.SuccessChance;
            var delta = session.SkillRank - session.RecipeLevel;
            var progress = action.BaseProgress > 0
                ? (int)((action.BaseProgress + (session.SkillRank >= 20 ? 21 : 0) + session.Craftsmanship * 0.65f) * (1 + 0.05f * delta))
                : 0;
            var quality = action.BaseQuality > 0
                ? (int)((action.BaseQuality + (session.SkillRank >= 40 ? 115 : 0) + session.Control * 0.75f) * (delta < 0 ? 1 + 0.05f * delta : 1))
                : 0;
            progress = Math.Min(progress, session.MaxProgress - session.Progress);
            quality = Math.Min(quality, session.MaxQuality - session.Quality);
            var restored = type == CraftActionType.MastersMend ? Math.Min(30, session.MaxDurability - session.Durability) : 0;
            var finishes = session.Progress + progress >= session.MaxProgress;
            var exhausted = session.Durability - durability <= 0;
            var reason = session.Status != CraftSessionStatus.Active ? "This crafting session has ended."
                : session.SkillRank < action.RequiredSkillRank ? $"Requires skill rank {action.RequiredSkillRank}."
                : session.CP < cp ? "Not enough CP." : string.Empty;
            return new CraftActionPreview(action, reason.Length == 0, reason, cp, durability, restored,
                progress, quality, chance, finishes, exhausted && !finishes && restored == 0, exhausted && chance < 100);
        }

        public static CraftActionOutcome Resolve(CraftSession session, CraftActionRequest request, Func<int> rollD100)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (rollD100 == null) throw new ArgumentNullException(nameof(rollD100));
            if (session.RulesVersion == 2) return CraftWorkEvaluator.Resolve(session, request, rollD100);
            var preview = Preview(session, request.Action);
            if (request.SessionId != session.Id || request.ExpectedActionCount != session.ActionCount)
                return new CraftActionOutcome(session, preview, false, false, "This action belongs to an earlier crafting state.");
            if (!preview.IsAvailable)
                return new CraftActionOutcome(session, preview, false, false, preview.UnavailableReason);
            var roll = rollD100();
            if (roll < 1 || roll > 100) throw new ArgumentOutOfRangeException(nameof(rollD100), "A D100 roll must be between 1 and 100.");
            var succeeded = roll <= preview.SuccessChance;
            var progress = session.Progress + (succeeded ? preview.ProgressGain : 0);
            var quality = session.Quality + (succeeded ? preview.QualityGain : 0);
            var durability = Math.Max(0, session.Durability - preview.DurabilityCost) + (succeeded ? preview.DurabilityRestored : 0);
            var veneration = session.VenerationCharges;
            var wasteNot = session.WasteNotCharges;
            if (preview.Action.BaseProgress > 0 && preview.Action.CPCost > 0 && veneration > 0) veneration--;
            if (preview.Action.DurabilityCost > 0 && wasteNot > 0) wasteNot--;
            var next = session with
            {
                CP = session.CP - preview.CPCost, Progress = progress, Quality = quality, Durability = durability,
                SteadyHandActive = succeeded && preview.Action.BaseProgress > 0 ? false : session.SteadyHandActive,
                MuscleMemoryActive = succeeded && preview.Action.BaseQuality > 0 ? false : session.MuscleMemoryActive,
                VenerationCharges = succeeded && request.Action == CraftActionType.Veneration ? 4 : veneration,
                WasteNotCharges = succeeded && request.Action == CraftActionType.WasteNot ? 4 : wasteNot,
                ActionCount = session.ActionCount + 1,
                Status = progress >= session.MaxProgress ? CraftSessionStatus.Succeeded
                    : durability <= 0 ? CraftSessionStatus.Failed : CraftSessionStatus.Active
            };
            if (succeeded && request.Action == CraftActionType.SteadyHand) next = next with { SteadyHandActive = true };
            if (succeeded && request.Action == CraftActionType.MuscleMemory) next = next with { MuscleMemoryActive = true };
            return new CraftActionOutcome(next, preview, true, succeeded, string.Empty);
        }
    }
}

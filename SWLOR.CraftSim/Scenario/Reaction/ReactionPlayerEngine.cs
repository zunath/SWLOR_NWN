using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

namespace SWLOR.CraftSim.Scenario.Reaction
{
    /// <summary>
    /// A Reaction engine as a prudent crafter plays it. Dampen can be taken away altogether (to measure what it is worth). A prudent
    /// crafter also uses Dampen only as the cheap repair it is: it does not spend the CP its next Stabilize needs (Stabilize is the
    /// bigger repair), and it does not Dampen while the Stability it restores is not missing. The generic adaptive player values
    /// neither, so left alone it spends the CP of its one Stabilize on Dampen, or stalls on Dampen at full Stability, which says more
    /// about the player than about Dampen.
    /// </summary>
    internal sealed class ReactionPlayerEngine : ICraftMinigameEngine
    {
        private const string Dampen = ReactionEngine.Dampen;
        private const string KeepsStabilizeCp = "Keeps the CP Stabilize needs.";
        private const string WouldWasteRestore = "Would waste the Stability it restores.";

        private readonly ReactionEngine _inner;
        private readonly bool _offersDampen;
        private readonly bool _prudent;
        private readonly IReadOnlyList<MinigameActionInfo> _actions;

        public ReactionPlayerEngine(ReactionEngine inner, bool offersDampen, bool prudent)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _offersDampen = offersDampen;
            _prudent = prudent;
            _actions = offersDampen ? inner.Actions : inner.Actions.Where(action => action.Id != Dampen).ToList();
        }

        /// <summary>The Reaction engine being played, as the game builds it.</summary>
        internal ReactionEngine Inner => _inner;

        public CraftMinigameType Type => _inner.Type;

        public MinigameStatus Status => _inner.Status;

        public MinigameMeters Meters => _inner.Meters;

        public int Turn => _inner.Turn;

        public MinigameOpportunity NextOpportunity => _inner.NextOpportunity;

        public IReadOnlyList<MinigameActionInfo> Actions => _actions;

        public IReadOnlyList<MinigameMeterView> DescribeMeters() => _inner.DescribeMeters();

        public MinigameActionPreview Preview(string actionId)
        {
            if (actionId == Dampen && !_offersDampen)
                throw new ArgumentException("Dampen is not offered.", nameof(actionId));

            var preview = _inner.Preview(actionId);
            if (actionId != Dampen || !preview.Enabled)
                return preview;

            var refusal = Refusal(preview.CpCost);
            return refusal == null ? preview : preview with { Enabled = false, DisabledReason = refusal };
        }

        public IReadOnlyList<MinigameActionPreview> PreviewAll() => _actions.Select(action => Preview(action.Id)).ToList();

        public MinigameActionResult Perform(string actionId)
        {
            if (actionId == Dampen)
            {
                var refusal = _offersDampen ? Refusal(_inner.Preview(Dampen).CpCost) : "Dampen is not offered.";
                if (refusal != null)
                    return new MinigameActionResult(actionId, false, refusal, null, Meters, Meters, DescribeMeters(), Status);
            }

            return _inner.Perform(actionId);
        }

        public ICraftMinigameEngine Fork(IMinigameRandom random) =>
            new ReactionPlayerEngine((ReactionEngine)_inner.Fork(random), _offersDampen, _prudent);

        /// <summary>Why a prudent crafter would not Dampen now, or null.</summary>
        private string Refusal(int dampenCp)
        {
            if (!_prudent)
                return null;

            var settings = _inner.Settings;
            var cp = _inner.Meters.Cp.Value;
            if (settings.StabilizeUnlocked && cp >= settings.StabilizeCp && cp - dampenCp < settings.StabilizeCp)
                return KeepsStabilizeCp;

            var integrity = _inner.Meters.Integrity;
            return integrity.Max - integrity.Value < ReactionRules.DampenRestore ? WouldWasteRestore : null;
        }
    }
}

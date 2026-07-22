using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
{
    /// <summary>
    /// Declares the expected in-engine behavior of one ability feat. The shared
    /// AbilityBehaviorExecutor turns each case into a live activation with assertions,
    /// so covering a new ability means adding one of these to the tree's
    /// IAbilityBehaviorSource rather than writing a bespoke test.
    /// </summary>
    public class AbilityBehaviorCase
    {
        public FeatType Feat { get; set; }

        /// <summary>
        /// Who the ability is activated on. Hostile abilities need HostileCreature so
        /// CanUseAbility's enemy check passes.
        /// </summary>
        public AbilityTargetKind Target { get; set; } = AbilityTargetKind.Self;

        /// <summary>
        /// Optional item resref equipped into the caster's right hand before activation,
        /// for abilities whose validation requires a specific weapon family.
        /// </summary>
        public string EquipMainHandResref { get; set; }

        /// <summary>
        /// Status effect classes expected on the ACTIVATOR after impact.
        /// </summary>
        public Type[] ExpectedActivatorStatusEffects { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Status effect classes expected on the TARGET after impact.
        /// </summary>
        public Type[] ExpectedTargetStatusEffects { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Temporary stat adjustments expected on the ACTIVATOR after impact, expressed as a
        /// delta from the value immediately before activation.
        /// </summary>
        public Dictionary<StatType, int> ExpectedActivatorStatAdjustments { get; set; } = new();

        /// <summary>
        /// Expect the target's hit points to drop below their pre-activation value.
        /// </summary>
        public bool ExpectsTargetDamage { get; set; }

        /// <summary>
        /// Expect the caster's FP pool to decrease.
        /// </summary>
        public bool ExpectsFPCost { get; set; }

        /// <summary>
        /// Expect the caster's Stamina pool to decrease.
        /// </summary>
        public bool ExpectsSTMCost { get; set; }

        /// <summary>
        /// Expect the ability's recast group to be on cooldown after activation.
        /// </summary>
        public bool ExpectsRecast { get; set; }

        /// <summary>
        /// Free-text context for a reviewer: why assertions are relaxed, quirks observed
        /// in the definition, etc. Not used by the executor.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Non-null marks the case as not engine-executable yet (e.g. requires ship/space
        /// context or an unavailable item blueprint). The executor records it as skipped;
        /// the coverage ratchet still counts it as declared. Burn these down over time.
        /// </summary>
        public string SkipReason { get; set; }
    }
}

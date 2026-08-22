using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
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
        /// Temporary stat adjustments expected on the TARGET after impact, expressed as a
        /// delta from the value immediately before activation.
        /// </summary>
        public Dictionary<StatType, int> ExpectedTargetStatAdjustments { get; set; } = new();

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
        /// The ability's impact restores part of its own cost (e.g. RestoreStaminaOnHit,
        /// restore-on-crit riders), so the exact post-deduction pool is unobservable - the
        /// refund lands in the same window as the deduction and may be conditional. Cost
        /// assertions fall back to requiring a NET dip below the pre-activation snapshot.
        /// Only set this when the definition demonstrably refunds; name the rider in Notes.
        /// </summary>
        public bool ImpactRefundsCosts { get; set; }

        /// <summary>
        /// Expect the (dead) target to be alive again after impact - for revival abilities.
        /// Usually paired with <see cref="TargetStartsDead"/>.
        /// </summary>
        public bool ExpectsTargetRevived { get; set; }

        /// <summary>
        /// Expect a temporary-hit-point effect on the ACTIVATOR after impact - for shield-style
        /// abilities that grant raw EffectTemporaryHitpoints rather than a status effect.
        /// </summary>
        public bool ExpectsActivatorTemporaryHP { get; set; }

        /// <summary>
        /// Expect the number of temporary-hit-point effects on the TARGET to increase after
        /// impact. Comparing the effect count makes this safe for both friendly targets and
        /// hostile fixtures that already carry the executor's damage buffer.
        /// </summary>
        public bool ExpectsTargetTemporaryHP { get; set; }

        /// <summary>
        /// Expect the ACTIVATOR's hit points to rise above their pre-activation value. The
        /// executor wounds the caster before activation so the heal is observable.
        /// </summary>
        public bool ExpectsActivatorHealing { get; set; }

        /// <summary>
        /// Expect a distinct friendly TARGET's hit points to rise above their pre-activation
        /// value. The executor wounds the target and suppresses its natural NPC regeneration
        /// before activation so only the tested impact can satisfy the assertion.
        /// </summary>
        public bool ExpectsTargetHealing { get; set; }

        /// <summary>
        /// Minimum hit points the revived target must have after impact. Use this to distinguish
        /// a bare resurrection from ranks which promise meaningful post-revival healing.
        /// Requires <see cref="ExpectsTargetRevived"/>.
        /// </summary>
        public int MinimumTargetHitPointsAfterRevive { get; set; }

        /// <summary>
        /// Healing percentage promised after revival. The executor derives the exact minimum
        /// from the target's maximum HP and the caster's Willpower scaling, then requires that
        /// full heal on top of the native resurrection's 1 HP.
        /// Requires <see cref="ExpectsTargetRevived"/>.
        /// </summary>
        public float? ExpectedTargetHealingPercentAfterRevive { get; set; }

        /// <summary>
        /// Distance at which a distinct target is spawned. The default preserves the close-range
        /// fixture used by most abilities; movement abilities can start farther away.
        /// </summary>
        public float TargetDistanceMeters { get; set; } = 1.5f;

        /// <summary>
        /// Maximum allowed caster-to-target distance after impact. This makes leap/intercept
        /// movement observable instead of passing on damage or status alone.
        /// </summary>
        public float? MaximumActivatorDistanceToTargetAfterImpact { get; set; }

        /// <summary>
        /// Perk levels seeded onto the caster before activation, for abilities whose impact
        /// scales off or requires OTHER perks (e.g. Leadership orders that emit the aura the
        /// caster has trained). NPCs default every perk to max level when unset, so this is
        /// only needed to pin a specific level or make a stat-gated branch deterministic.
        /// </summary>
        public Dictionary<PerkType, int> SetupNPCPerkLevels { get; set; } = new();

        /// <summary>
        /// When true, the spawned target is killed before activation - for revival abilities
        /// that require a dead friendly target.
        /// </summary>
        public bool TargetStartsDead { get; set; }

        /// <summary>
        /// When true, the spawned target joins the caster's party before activation (via the
        /// real associate-add pipeline) - for abilities whose validation requires a party
        /// member rather than just a same-faction ally.
        /// </summary>
        public bool TargetJoinsCasterParty { get; set; }

        /// <summary>
        /// Optional status effect applied to the target before activation, for abilities whose
        /// validation requires pre-existing target state (e.g. Tag In requires a Guarded ally).
        /// A factory because some status effects only have parameterized constructors.
        /// </summary>
        public Func<Service.StatusEffectService.IStatusEffect> TargetSetupStatusEffectFactory { get; set; }

        /// <summary>
        /// Status effect classes applied to the target before activation. Prefer this for
        /// parameterless effects; use <see cref="TargetSetupStatusEffectFactory"/> when custom
        /// constructor state is required.
        /// </summary>
        public Type[] TargetSetupStatusEffects { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Pre-applied target status effect classes which must be absent after impact. Every
        /// entry must also appear in <see cref="TargetSetupStatusEffects"/>.
        /// </summary>
        public Type[] ExpectedRemovedTargetStatusEffects { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Documents why a definition-declared resource cost cannot be observed by this case.
        /// This is intentionally separate from Notes so cost coverage cannot disappear silently.
        /// </summary>
        public string CostAssertionWaiverReason { get; set; }

        /// <summary>
        /// Documents why an executable impact has no observable outcome assertion. Use only
        /// where the harness lacks a safe observation seam (for example, enmity-only impacts).
        /// </summary>
        public string OutcomeAssertionWaiverReason { get; set; }

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

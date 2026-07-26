using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for FeatType.MaulingBite through FeatType.WillFracture,
    /// alphabetically the second half of the NPC innate-ability tree
    /// (SWLOR.Game.Server/Feature/AbilityDefinition/NPC). See <see cref="NPCAbilityBehaviorsPart1"/>
    /// for the shared conventions of this tree (no CustomValidation, no weapon/FP gating,
    /// InnateAbility/NPCSignatureAbility factories, unconditional status+damage on a landed hit).
    /// </summary>
    public class NPCAbilityBehaviorsPart2 : IAbilityBehaviorSource
    {
        [EngineTest("NPC ability behaviors (part 2)", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new NPCAbilityBehaviorsPart2().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // MaulingBiteAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.MaulingBite,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // MercilessAngleAbilityDefinition - NPCSignatureAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.MercilessAngle,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HemorrhageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // MindSpikeAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.MindSpike,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(TerrifiedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // NullShockAbilityDefinition - InnateAbility.BuildArea, target-anchored sphere.
                new()
                {
                    Feat = FeatType.NullShock,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceSuppressionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // OpeningCutAbilityDefinition - NPCSignatureAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.OpeningCut,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // OverloadShotAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.OverloadShot,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PackHarrierAbilityDefinition - NPCSignatureAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.PackHarrier,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HobbleStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PermafrostRuptureAbilityDefinition - InnateAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.PermafrostRupture,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FreezingStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PiercingQuillsAbilityDefinition - hand-written target-anchored cone (RequiresTarget),
                // unconditional damage + status.
                new()
                {
                    Feat = FeatType.PiercingQuills,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PouncingStrikeAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.PouncingStrike,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PrecisionShotAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.PrecisionShot,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(MarkedForDeathStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PressureLockAbilityDefinition - NPCSignatureAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.PressureLock,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RakingClawsAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.RakingClaws,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RallyBreakerAbilityDefinition - NPCSignatureAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.RallyBreaker,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(MarkedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RangefinderShotAbilityDefinition - NPCSignatureAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.RangefinderShot,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RendingBiteAbilityDefinition - hand-written single-target impact, unconditional
                // damage + status.
                new()
                {
                    Feat = FeatType.RendingBite,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RendingCarveAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.RendingCarve,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HemorrhageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RimePounceAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.RimePounce,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FreezingStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RupturingQuakeAbilityDefinition - hand-written self-centered sphere (no
                // RequiresTarget), unconditional damage + status.
                new()
                {
                    Feat = FeatType.RupturingQuake,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SavageRoarAbilityDefinition - InnateAbility.BuildArea, self-centered sphere with a
                // deliberate 0 base damage (pure debuff howl); status is still unconditional.
                new()
                {
                    Feat = FeatType.SavageRoar,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (pure debuff howl); status is still unconditional on a landed hit.",
                },

                // ScorchingBreathAbilityDefinition - hand-written target-anchored cone
                // (RequiresTarget), unconditional damage + status.
                new()
                {
                    Feat = FeatType.ScorchingBreath,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SeismicSlamAbilityDefinition - hand-written self-centered sphere (no
                // RequiresTarget), unconditional damage + status.
                new()
                {
                    Feat = FeatType.SeismicSlam,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SerratedSlashAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.SerratedSlash,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HemorrhageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ShrapnelBurstAbilityDefinition - InnateAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.ShrapnelBurst,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SignalSnareAbilityDefinition - NPCSignatureAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.SignalSnare,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SnapRushAbilityDefinition - NPCSignatureAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.SnapRush,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SonicShriekAbilityDefinition - InnateAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.SonicShriek,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // StaticBurstAbilityDefinition - InnateAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.StaticBurst,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // StaticWebAbilityDefinition - InnateAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.StaticWeb,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // StimCanisterAbilityDefinition - InnateAbility.BuildArea, target-anchored sphere.
                new()
                {
                    Feat = FeatType.StimCanister,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SuppressingShotAbilityDefinition - InnateAbility.BuildArea, target-anchored line.
                new()
                {
                    Feat = FeatType.SuppressingShot,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SustainBurnAbilityDefinition - NPCSignatureAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.SustainBurn,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TacticalMarkAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.TacticalMark,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposeWeakPointStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TailSweepAbilityDefinition - InnateAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.TailSweep,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TargetLockAbilityDefinition - InnateAbility.BuildSingleTarget.
                new()
                {
                    Feat = FeatType.TargetLock,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(VulnerableStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TerrifyingBellowAbilityDefinition - hand-written self-centered sphere (no
                // RequiresTarget) with a deliberate 0 base damage (pure fear howl); status is
                // still unconditional.
                new()
                {
                    Feat = FeatType.TerrifyingBellow,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(TerrifiedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (pure fear howl); status is still unconditional on a landed hit.",
                },

                // ToxicCloudAbilityDefinition - InnateAbility.BuildArea, target-anchored sphere.
                new()
                {
                    Feat = FeatType.ToxicCloud,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ToxinStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ToxicSpitAbilityDefinition - hand-written single-target impact (RequiresTarget),
                // unconditional damage + status.
                new()
                {
                    Feat = FeatType.ToxicSpit,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // VenomSprayAbilityDefinition - InnateAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.VenomSpray,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenClampAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenClamp,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenMarkAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenMark,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(MarkedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenMaulAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenMaul,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenOrderAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenOrder,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenRendAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenRend,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(TerrifiedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenSweepAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenSweep,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenWallAbilityDefinition - NPCSignatureAbility.BuildArea, self-centered sphere.
                new()
                {
                    Feat = FeatType.WardenWall,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WillFractureAbilityDefinition - NPCSignatureAbility.BuildArea, target-anchored cone.
                new()
                {
                    Feat = FeatType.WillFracture,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FoggyMindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}

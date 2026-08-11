using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class ForceAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Force ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new ForceAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // BenevolenceAbilityDefinition - self/ally scaled heal (ApplyActivatedScaledHeal), no
                // trackable status effect type. ValidateFriendlyTarget allows self.
                new()
                {
                    Feat = FeatType.Benevolence1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Heals via ApplyActivatedScaledHeal (raw EffectHeal), not a tracked status effect.",
                },
                new()
                {
                    Feat = FeatType.Benevolence2,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Benevolence3,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // CreepingTerrorAbilityDefinition - area field that schedules 3s-interval pulses
                // (CombatAreaPulses.SchedulePulses); each pulse unconditionally hits hostiles in
                // radius with Ability.ApplyHostileCombatImpact(..., typeof(HobbleStatusEffect)).
                // First pulse lands ~3s after activation, well inside the 20s wait window.
                new()
                {
                    Feat = FeatType.CreepingTerror1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HobbleStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Field pulses every 3s over 30s; first pulse damage/Hobble land within the wait window.",
                },
                new()
                {
                    Feat = FeatType.CreepingTerror2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HobbleStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CreepingTerror3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HobbleStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // EclipseOfResolveAbilityDefinition - capstone self-centered sphere (OriginOnSelf),
                // instant telegraph (0s), unconditional status on the hostile caught in the shape.
                new()
                {
                    Feat = FeatType.EclipseOfResolve1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(EclipseOfResolve1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Sphere targeting has OriginOnSelf, so no location target is required; impact is centered on the activator and catches the nearby hostile.",
                },

                // ForceChokeAbilityDefinition - hostile single-target: immobilize + a DOT status
                // effect (Frequency 3s) both applied unconditionally on a successful hit.
                new()
                {
                    Feat = FeatType.ForceChoke1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect), typeof(ForceChokeDamageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from the ForceChokeDamageStatusEffect DOT (3s tick), not a direct hit.",
                },
                new()
                {
                    Feat = FeatType.ForceChoke2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect), typeof(ForceChokeDamageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceChoke3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect), typeof(ForceChokeDamageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceChoke4,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect), typeof(ForceChokeDamageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // ForceDrainAbilityDefinition - hostile direct-damage siphon; heal is raw EffectHeal
                // on the activator (not a status effect), gated on damage > 0.
                new()
                {
                    Feat = FeatType.ForceDrain1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Drain heal is a raw EffectHeal applied to the activator, not a tracked status effect.",
                },
                new()
                {
                    Feat = FeatType.ForceDrain2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceDrain3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // ForceInterceptAbilityDefinition - ValidateFriendlyTarget(..., allowSelf: false),
                // so it rejects a self target; the harness only offers Self or HostileCreature and
                // HostileCreature fails the friendly check, so no target kind can pass validation.
                new()
                {
                    Feat = FeatType.ForceIntercept1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetDistanceMeters = 10f,
                    MaximumActivatorDistanceToTargetAfterImpact = 2f,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceIntercept1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "ValidateFriendlyTarget with allowSelf:false - cast on a spawned same-faction ally. Impact jumps the caster to the ally and applies ForceIntercept1StatusEffect to them; 5 FP / 24s recast per the definition.",
                },

                // ForceJudgmentAbilityDefinition - hostile direct/area damage with an unconditional
                // status effect per tier.
                new()
                {
                    Feat = FeatType.ForceJudgment1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceJudgment1StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceJudgment2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceJudgment2StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceJudgment3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceJudgment3StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Tier 3 is an area (maxTargets 0 -> IsAreaAbility path via ApplyTelegraphedCombatImpact).",
                },

                // ForceLeapAbilityDefinition - hostile gap-closer + direct Force damage.
                new()
                {
                    Feat = FeatType.ForceLeap1,
                    Target = AbilityTargetKind.HostileCreature,
                    TargetDistanceMeters = 10f,
                    MaximumActivatorDistanceToTargetAfterImpact = 2f,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceLeap2,
                    Target = AbilityTargetKind.HostileCreature,
                    TargetDistanceMeters = 10f,
                    MaximumActivatorDistanceToTargetAfterImpact = 2f,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // ForceLightningAbilityDefinition - hostile direct damage + unconditional Shock
                // status effect, plus an unasserted arc to nearby hostiles.
                new()
                {
                    Feat = FeatType.ForceLightning1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceLightning2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceLightning3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // ForcePushAbilityDefinition - hostile cone knockdown + unconditional Hobble rider
                // on a successful hit. Cone shape always resolves to a location target internally;
                // the hostile creature spawned near the caster still falls inside the short cone.
                new()
                {
                    Feat = FeatType.ForcePush1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect), typeof(HobbleStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Cone origin is centerOnActivator when no valid target object is passed; harness passes a valid hostile target so the cone is aimed at it.",
                },
                new()
                {
                    Feat = FeatType.ForcePush2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect), typeof(HobbleStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForcePush3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect), typeof(HobbleStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // ForceSanctuaryAbilityDefinition - friendly zone status + zone healing, both
                // scheduled on 3s pulses (AbilityAreaEffects.ScheduleFriendlyZoneStatus/Healing);
                // sphere targeting has no OriginOnSelf flag, but the zone is still built at the
                // resolved impact location which falls back to the activator when no valid target.
                new()
                {
                    Feat = FeatType.ForceSanctuary1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ForceSanctuary1StatusEffect) },
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Zone status pulses starting at t=0 on a 3s cadence; healing pulses start at t=3s. Self is used because ResolveImpactLocation falls back to the activator's location without a real target.",
                },

                // ForceSparkAbilityDefinition - hostile direct damage + unconditional status.
                new()
                {
                    Feat = FeatType.ForceSpark1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceSpark1StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceSpark2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceSpark2StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                // FuryStanceAbilityDefinition - self toggle stance via WeaponActiveAbilityDefinitionBase
                // .ConfigureToggle: the impact unconditionally applies the stance status to self.
                new()
                {
                    Feat = FeatType.FuryStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FuryStance1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.FuryStance2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FuryStance2StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // GuardianWardAbilityDefinition - grants temporary HP (raw EffectTemporaryHitpoints,
                // not a tracked status effect) plus conditional riders gated on stat adjustments a
                // base NPC doesn't have; the unconditional temp HP is asserted, the riders are not.
                new()
                {
                    Feat = FeatType.GuardianWard1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants raw temporary HP (asserted via the engine-effect scan); LightGuardianPowerSupport riders are gated on stat adjustments the NPC doesn't have.",
                },
                new()
                {
                    Feat = FeatType.GuardianWard2,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardianWard3,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardianWard4,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // HungerOfTheDarkAbilityDefinition - capstone self-buff, unconditional status.
                new()
                {
                    Feat = FeatType.HungerOfTheDark1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(HungerOfTheDark1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // LastStandOfTheLightAbilityDefinition - capstone friendly buff, allowSelf true.
                new()
                {
                    Feat = FeatType.LastStandOfTheLight1,
                    Target = AbilityTargetKind.Self,
                    ExpectedTargetStatusEffects = new[] { typeof(LastStandOfTheLight1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // MindTrickAbilityDefinition - hostile control; duration comes from a Willpower
                // contest (caster vs target) that both spawn as identical nw_rat001, so it resolves
                // to the 30s base duration and the status is unconditionally applied.
                new()
                {
                    Feat = FeatType.MindTrick1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ConfusionStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Duration is a Willpower contest between caster and target; both are nw_rat001 so it resolves to the 30s base rather than being resisted to 0.",
                },
                new()
                {
                    Feat = FeatType.MindTrick2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ConfusionStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // NightmareFieldAbilityDefinition - self-centered sphere (OriginOnSelf), instant
                // telegraph, unconditional status on the hostile in range.
                new()
                {
                    Feat = FeatType.NightmareField1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(NightmareField1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // PurifyingWaveAbilityDefinition - self-centered sphere that unconditionally damages
                // nearby hostiles and heals nearby friendlies (raw EffectHeal, not a status effect).
                new()
                {
                    Feat = FeatType.PurifyingWave1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Asserts both halves of the hybrid area impact: hostile damage and raw EffectHeal on the wounded caster.",
                },

                // RadiantLanceAbilityDefinition - hostile line AOE, unconditional direct damage.
                new()
                {
                    Feat = FeatType.RadiantLance1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RadiantLance2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RadiantLance3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // RenewalAbilityDefinition - friendly heal + unconditional status (RegenerativeHealing).
                new()
                {
                    Feat = FeatType.Renewal1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
                    ExpectsTargetHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Renewal2,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
                    ExpectsTargetHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Renewal3,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
                    ExpectsTargetHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // ThrowLightsaberAbilityDefinition - hostile line AOE that requires an equipped
                // weapon (ValidateWeapon checks right/left hand for a weapon base item type).
                new()
                {
                    Feat = FeatType.ThrowLightsaber1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = "nw_wswls001",
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ThrowLightsaber2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = "nw_wswls001",
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ThrowLightsaber3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = "nw_wswls001",
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ThrowRockAbilityDefinition - hostile direct physical damage, no status.
                new()
                {
                    Feat = FeatType.ThrowRock1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ThrowRock2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ThrowRock3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // WeakenResolveAbilityDefinition - hostile debuff, unconditional status, no damage.
                new()
                {
                    Feat = FeatType.WeakenResolve1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenResolve1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.WeakenResolve2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenResolve2StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}

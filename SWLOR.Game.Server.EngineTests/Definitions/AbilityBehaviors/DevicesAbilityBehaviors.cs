using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Devices
    /// ability definitions (SWLOR.Game.Server/Feature/AbilityDefinition/Devices). None of
    /// these abilities validate a specific equipped weapon (SkillType.Devices is not weapon
    /// gated), so no EquipMainHandResref is set anywhere in this tree.
    /// </summary>
    public class DevicesAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Devices ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new DevicesAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AdhesiveGrenadeAbilityDefinition - hostile AoE, 0 base damage but unconditional
                // Slow (via statusEffectFactory) on a landed hit.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AdhesiveGrenade1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(AdhesiveGrenadeStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only grenade); RequirementItem(\"explosives\") is skipped for non-PC casters."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AdhesiveGrenade2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(AdhesiveGrenadeStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only grenade); RequirementItem(\"explosives\") is skipped for non-PC casters."
                },

                // ArcProjectorAbilityDefinition - hostile single-target damage; Tactical Uplink rider
                // is conditional on a stat bonus most casters won't have.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ArcProjector1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Tactical Uplink rider requires a stat bonus (AssaultGadgetTacticalUplink); not present on a fresh caster, not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ArcProjector2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Tactical Uplink rider requires a stat bonus (AssaultGadgetTacticalUplink); not present on a fresh caster, not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ArcProjector3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Tactical Uplink rider requires a stat bonus (AssaultGadgetTacticalUplink); not present on a fresh caster, not asserted."
                },

                // BlasterBeaconAbilityDefinition - schedules periodic single-target pulses (no upfront
                // damage/status; first pulse fires ~3s after activation via a tracked emitter).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.BlasterBeacon1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from a scheduled pulse emitter (first pulse ~3s after activation, guaranteed hit), not an immediate impact."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.BlasterBeacon2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from a scheduled pulse emitter (first pulse ~3s after activation, guaranteed hit), not an immediate impact."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.BlasterBeacon3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from a scheduled pulse emitter (first pulse ~3s after activation, guaranteed hit), not an immediate impact."
                },

                // ClusterGrenadeAbilityDefinition - hostile AoE; 3 offset mini-blasts around the impact
                // point, no unconditional status.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ClusterGrenade1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from 3 small blasts offset ~1m around the impact point rather than a single centered blast."
                },

                // ConcussionGrenadeAbilityDefinition - hostile AoE damage + unconditional Knockdown.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ConcussionGrenade1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ConcussionGrenade2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // CryoSprayerAbilityDefinition - hostile cone damage + unconditional Hobble.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CryoSprayer1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HobbleStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // DeflectorShieldAbilityDefinition - friendly single-target temporary HP shield; self
                // targeting is allowed (ValidateFriendlyTarget defaults allowSelf:true). No status
                // effect class is applied - ApplyShieldTemporaryHP grants a raw EffectTemporaryHitpoints
                // to the resolved friendly target (the activator, on a self-cast).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DeflectorShield1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants raw EffectTemporaryHitpoints (not a status effect class); field-support riders are conditional on stat bonuses absent here."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DeflectorShield2,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants raw EffectTemporaryHitpoints (not a status effect class); field-support riders are conditional on stat bonuses absent here."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DeflectorShield3,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants raw EffectTemporaryHitpoints (not a status effect class); field-support riders are conditional on stat bonuses absent here."
                },

                // DisruptionPulseAbilityDefinition - hostile AoE damage + unconditional (typed
                // resistance-checked) Disruption status.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DisruptionPulse1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisruptionPulseStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },

                // EmergencyBunkerAbilityDefinition - capstone friendly zone buff; the activator counts
                // as its own first \"friendly near location\" tick, so the zone status lands on self
                // almost immediately (first pulse at elapsed:0). The same per-friendly callback
                // (ApplyBunkerTemporaryHP) also grants raw temporary HP, so the caster receives it too.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.EmergencyBunker1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(EmergencyBunker1StatusEffect) },
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "ApplyBunkerTemporaryHP grants raw temporary HP (TemporaryHitPointEffects.ApplyFlat, not a status effect class) to the zone's first friendly - the self-targeting caster."
                },

                // FlamethrowerAbilityDefinition - hostile cone damage; tier I applies no unconditional
                // status, tiers II/III apply unconditional Burn.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Flamethrower1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Flamethrower2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Flamethrower3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // FlashGrenadeAbilityDefinition - hostile AoE, 0 base damage but unconditional Flash
                // (via statusEffectFactory - the FlashStatusEffect type param is superseded by the
                // factory, matching the pattern used elsewhere for factory-produced statuses).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FlashGrenade1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FlashGrenade1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only grenade); RequirementItem(\"explosives\") is skipped for non-PC casters."
                },

                // FragGrenadeAbilityDefinition - hostile AoE damage; tier I applies no unconditional
                // status, tiers II/III apply unconditional Bleed.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FragGrenade1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FragGrenade2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FragGrenade3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },

                // GroupDeflectorAbilityDefinition - friendly party temporary HP shield; requires
                // neither a target object nor a location (self-centered, party-wide). No status
                // effect class is applied - GetFriendlyTargets(activator, activator, true) includes
                // the activator itself, so it also receives the raw EffectTemporaryHitpoints.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GroupDeflector1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants raw EffectTemporaryHitpoints to nearby party members (not a status effect class), including the activator itself; field-support riders are conditional."
                },

                // IncendiaryFieldAbilityDefinition - hostile area-pulse damage (scheduled emitter,
                // first pulse ~3s after activation), no unconditional status.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IncendiaryField1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from a scheduled area-pulse emitter (first pulse ~3s after activation)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IncendiaryField2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from a scheduled area-pulse emitter (first pulse ~3s after activation)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IncendiaryField3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage comes from a scheduled area-pulse emitter (first pulse ~3s after activation)."
                },

                // IonGrenadeAbilityDefinition - hostile AoE damage; tier I applies no unconditional
                // status, tier II applies unconditional Shock.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IonGrenade1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IonGrenade2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },

                // IonLanceAbilityDefinition - hostile line damage, no unconditional status.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IonLance1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IonLance2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.IonLance3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // KillzoneBeaconAbilityDefinition - capstone; two independent scheduled area-pulse
                // emitters (damage-only, and damage+Shock), both guaranteed-hit, first pulse ~3s in.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.KillzoneBeacon1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage/status come from two scheduled area-pulse emitters (first pulse ~3s after activation, guaranteed hit)."
                },

                // OverloadBarrageAbilityDefinition - capstone hostile single-target; three separate
                // impacts, each unconditionally applying its own status on a landed hit.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.OverloadBarrage1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect), typeof(KnockdownStatusEffect), typeof(SonicBurst3StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Three independent impacts each roll their own hit chance; all three statuses are unconditional on a landed hit."
                },

                // PowerCellAbilityDefinition - friendly single-/area-target ally buff; self targeting
                // is allowed (ValidateFriendlyTarget defaults allowSelf:true).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PowerCell1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PowerCell1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Uses a distinct ally so its stamina restore cannot mask the caster's 4 STM activation cost."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PowerCell2,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PowerCell2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Uses a distinct ally so its stamina restore cannot mask the caster's activation cost."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PowerCell3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PowerCell3StatusEffect) },
                    ExpectsRecast = true,
                    CostAssertionWaiverReason = "The self-centered area always restores the caster's stamina by more than its 7 STM cost in the same engine tick.",
                    Notes = "Unlike ranks I-II, the area rank always includes the caster, so its same-tick stamina restore masks the declared cost."
                },

                // RailDartAbilityDefinition - hostile single-target damage + unconditional Bleed.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.RailDart1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.RailDart2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.RailDart3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // RemoteChargeAbilityDefinition - hostile AoE damage after a short telegraph delay;
                // tier I applies no unconditional status, tier II applies unconditional Knockdown.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.RemoteCharge1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Detonation is telegraphed with a 3s delay before damage/status apply."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.RemoteCharge2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Detonation is telegraphed with a 6s delay before damage/status apply."
                },

                // ShockBeaconAbilityDefinition - scheduled single-target pulses (guaranteed hit),
                // damage + unconditional Shock.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ShockBeacon1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage/status come from a scheduled pulse emitter (first pulse ~3s after activation, guaranteed hit)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ShockBeacon2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Damage/status come from a scheduled pulse emitter (first pulse ~3s after activation, guaranteed hit)."
                },

                // SignalJammerAbilityDefinition - scheduled area-pulse status, 0 base damage; hit
                // chance is normally resolved per pulse but repeats every 3s for up to 45s.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SignalJammer1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SignalJammerStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design; status comes from a scheduled area-pulse emitter (first pulse ~3s after activation, repeating)."
                },

                // SonicBurstAbilityDefinition - hostile AoE damage; tier I applies no unconditional
                // status, tiers II/III apply their own unconditional Sonic Burst status.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SonicBurst1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SonicBurst2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SonicBurst2StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SonicBurst3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SonicBurst3StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // ThermalDetonatorAbilityDefinition - capstone hostile AoE damage + unconditional Burn.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ThermalDetonator1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "RequirementItem(\"explosives\") is skipped for non-PC casters."
                },

                // WeaponJamAbilityDefinition - hostile single-target, 0 base damage but unconditional
                // Weapon Jam status.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.WeaponJam1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeaponJam1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (utility debuff)."
                },

                // WristRocketAbilityDefinition - hostile single-target damage; tier I applies no
                // unconditional status, tiers II/III apply unconditional Knockdown.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.WristRocket1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.WristRocket2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.WristRocket3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                }
            };
        }
    }
}

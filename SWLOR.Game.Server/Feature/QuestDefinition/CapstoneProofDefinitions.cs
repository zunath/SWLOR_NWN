using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    /// <summary>
    /// The mechanic each capstone line proves on its dungeon's enemies. Every proof is the central mechanic of the
    /// capstone's style and is reachable before the capstone perk: the capstone's own ability and status never count.
    /// </summary>
    public static class CapstoneProofDefinitions
    {
        public static IReadOnlyList<CapstoneProofDefinition> All { get; } = new[]
        {
            // Pistol
            new CapstoneProofDefinition("last_word", "while your Disarming Shot weakens them",
                enemyStatusEffects: new[] { typeof(DisarmingShotStatusEffect) }),
            new CapstoneProofDefinition("dead_mans_hand", "with Quick Draw, Double Shot or Fan the Hammer",
                abilityHits: new[]
                {
                    FeatType.QuickDraw1, FeatType.QuickDraw2, FeatType.QuickDraw3, FeatType.QuickDraw4,
                    FeatType.DoubleShot1, FeatType.DoubleShot2, FeatType.DoubleShot3,
                    FeatType.FanTheHammer1, FeatType.FanTheHammer2,
                }),

            // Rifle
            new CapstoneProofDefinition("kill_box", "while your Suppression is on them",
                enemyStatusEffects: new[] { typeof(SuppressionStatusEffect) }),
            new CapstoneProofDefinition("one_shot", "with Aimed Shot, Piercing Round or Headshot",
                abilityHits: new[]
                {
                    FeatType.AimedShot1, FeatType.AimedShot2, FeatType.AimedShot3, FeatType.AimedShot4,
                    FeatType.PiercingRound1, FeatType.PiercingRound2, FeatType.PiercingRound3,
                    FeatType.Headshot1, FeatType.Headshot2,
                }),

            // Throwing
            new CapstoneProofDefinition("rain_of_steel", "with Explosive Toss or Concussive Toss",
                abilityHits: new[]
                {
                    FeatType.ExplosiveToss1, FeatType.ExplosiveToss2, FeatType.ExplosiveToss3, FeatType.ExplosiveToss4,
                    FeatType.ConcussiveToss1, FeatType.ConcussiveToss2,
                }),
            new CapstoneProofDefinition("perfect_flurry", "while they bleed from your throws",
                enemyStatusEffects: new[] { typeof(BleedStatusEffect) }),

            // Twin Blade
            new CapstoneProofDefinition("tempest_bloom", "with Spinning Whirl, Blade Vortex or Serrated Arc",
                abilityHits: new[]
                {
                    FeatType.SpinningWhirl1, FeatType.SpinningWhirl2, FeatType.SpinningWhirl3,
                    FeatType.BladeVortex1, FeatType.BladeVortex2,
                    FeatType.SerratedArc1, FeatType.SerratedArc2, FeatType.SerratedArc3,
                }),
            new CapstoneProofDefinition("red_bloom", "while they bleed from your blades",
                enemyStatusEffects: new[] { typeof(BleedStatusEffect) }),

            // Vibroblade
            new CapstoneProofDefinition("invincible", "after drawing them to you with Covering Strike",
                enemyStatusEffects: new[] { typeof(CoveringStrikeStatusEffect) }),

            // Vibroknife
            new CapstoneProofDefinition("viral_cascade", "after applying Venom with Virulent Blade or building Infection on them",
                enemyStatusEffects: new[] { typeof(VenomStatusEffect), typeof(InfectionStatusEffect) },
                enemyStatusEffectFilter: (type, ability) => type != typeof(VenomStatusEffect) || ability?.SkillType == SkillType.Vibroknife),
            new CapstoneProofDefinition("systemic_shutdown", "after exposing them with Crippling Slice or dropping them with Backstab",
                enemyStatusEffects: new[] { typeof(ExposedStatusEffect), typeof(KnockdownStatusEffect) }),

            // Heavy Vibroblade
            new CapstoneProofDefinition("absolute_defense", "after dazzling them with Flash",
                enemyStatusEffects: new[] { typeof(FlashStatusEffect) },
                abilityHits: new[] { FeatType.Flash1 }),
            new CapstoneProofDefinition("soul_ascension", "with Sacrificial Blade or Soul Burst",
                abilityHits: new[] { FeatType.SacrificialBlade1, FeatType.SoulBurst1 }),

            // Katar
            new CapstoneProofDefinition("adamantine_guard", "while you guard an ally with Steel Shoulder or hold Whirling Guard or Iron Wall",
                supportStatusEffects: new[]
                {
                    typeof(GuardedStatusEffect), typeof(GuardingStatusEffect),
                    typeof(WhirlingGuardStatusEffect), typeof(IronWallStanceStatusEffect),
                }),
            new CapstoneProofDefinition("scrapheap_lockdown", "after hobbling them with Hooking Strike or dazing them with Joint Lock",
                enemyStatusEffects: new[] { typeof(HamstringStatusEffect), typeof(DazedStatusEffect) }),

            // Lightsaber
            new CapstoneProofDefinition("saber_storm", "after sundering their defenses",
                enemyStatusEffects: new[] { typeof(SunderStatusEffect) }),
            new CapstoneProofDefinition("guardian_master", "while warded by your Saber Ward",
                supportStatusEffects: new[] { typeof(SaberWardStatusEffect) }),

            // Saberstaff
            new CapstoneProofDefinition("saber_cyclone", "with Circle Slash or Maelstrom Arc",
                abilityHits: new[]
                {
                    FeatType.CircleSlash1, FeatType.CircleSlash2, FeatType.CircleSlash3,
                    FeatType.MaelstromArc1, FeatType.MaelstromArc2,
                }),
            new CapstoneProofDefinition("infinite_conduit", "with Focused Arc, Sever Focus or Guarded Channel",
                abilityHits: new[]
                {
                    FeatType.FocusedArc1, FeatType.FocusedArc2, FeatType.FocusedArc3, FeatType.FocusedArc4,
                    FeatType.SeverFocus1, FeatType.SeverFocus2,
                    FeatType.GuardedChannel1, FeatType.GuardedChannel2, FeatType.GuardedChannel3,
                }),

            // Spear
            new CapstoneProofDefinition("forcebane", "after clouding them with Disabling Strike or Disruption Field",
                enemyStatusEffects: new[] { typeof(FoggyMindStatusEffect), typeof(ForceDisruptionStatusEffect) }),
            new CapstoneProofDefinition("crippling_defense", "with Disabling Strike, Disruption Field, Hampering Barrage, Interruption Strike, Sweeping Flank or Vigor Thrust",
                abilityHits: new[]
                {
                    FeatType.DisablingStrike1, FeatType.DisablingStrike2, FeatType.DisablingStrike3, FeatType.DisablingStrike4,
                    FeatType.DisruptionField1, FeatType.DisruptionField2,
                    FeatType.HamperingBarrage1, FeatType.HamperingBarrage2,
                    FeatType.InterruptionStrike1, FeatType.InterruptionStrike2, FeatType.InterruptionStrike3,
                    FeatType.SweepingFlank1, FeatType.SweepingFlank2, FeatType.SweepingFlank3,
                    FeatType.VigorThrust1, FeatType.VigorThrust2, FeatType.VigorThrust3, FeatType.VigorThrust4,
                }),

            // Staff
            new CapstoneProofDefinition("unmoving_center", "with Sweeping Guard, Leg Sweep or Line Breaker, or inside your Shelter Circle",
                abilityHits: new[]
                {
                    FeatType.SweepingGuard1, FeatType.LegSweep1, FeatType.LegSweep2, FeatType.LegSweep3,
                    FeatType.LineBreaker1, FeatType.LineBreaker2, FeatType.LineBreaker3, FeatType.LineBreaker4,
                },
                supportStatusEffects: new[] { typeof(ShelterCircleStatusEffect) }),
            new CapstoneProofDefinition("worldbreaker", "after dazing, knocking down or disorienting them",
                enemyStatusEffects: new[] { typeof(DazedStatusEffect), typeof(KnockdownStatusEffect), typeof(DisorientedStatusEffect) }),

            // Force
            new CapstoneProofDefinition("last_stand_of_the_light", "after using Force Intercept on an ally, or granting Reflective Barrier or Courageous Resolve to yourself or an ally",
                supportStatusEffects: new[]
                {
                    typeof(ForceIntercept1StatusEffect), typeof(ReflectiveBarrier1StatusEffect), typeof(CourageousResolve1StatusEffect),
                }),
            new CapstoneProofDefinition("hunger_of_the_dark", "with Force Drain, Force Lightning or Force Spark",
                abilityHits: new[]
                {
                    FeatType.ForceDrain1, FeatType.ForceDrain2, FeatType.ForceDrain3,
                    FeatType.ForceLightning1, FeatType.ForceLightning2, FeatType.ForceLightning3,
                    FeatType.ForceSpark1, FeatType.ForceSpark2,
                }),
            new CapstoneProofDefinition("eclipse_of_resolve", "after weakening their resolve",
                enemyStatusEffects: new[]
                {
                    typeof(NightmareField1StatusEffect), typeof(WeakenResolve1StatusEffect), typeof(WeakenResolve2StatusEffect),
                }),

            // Devices
            new CapstoneProofDefinition("thermal_detonator", "with your grenades",
                abilityHits: new[]
                {
                    FeatType.FragGrenade1, FeatType.FragGrenade2, FeatType.FragGrenade3,
                    FeatType.ConcussionGrenade1, FeatType.ConcussionGrenade2, FeatType.FlashGrenade1,
                    FeatType.IonGrenade1, FeatType.IonGrenade2, FeatType.AdhesiveGrenade1, FeatType.AdhesiveGrenade2,
                    FeatType.ClusterGrenade1, FeatType.DisruptionPulse1,
                }),
            new CapstoneProofDefinition("overload_barrage", "with your assault gadgets",
                abilityHits: new[]
                {
                    FeatType.Flamethrower1, FeatType.Flamethrower2, FeatType.Flamethrower3,
                    FeatType.WristRocket1, FeatType.WristRocket2, FeatType.WristRocket3,
                    FeatType.SonicBurst1, FeatType.SonicBurst2, FeatType.SonicBurst3,
                    FeatType.ArcProjector1, FeatType.ArcProjector2, FeatType.ArcProjector3,
                    FeatType.IonLance1, FeatType.IonLance2, FeatType.IonLance3,
                    FeatType.RailDart1, FeatType.RailDart2, FeatType.RailDart3, FeatType.CryoSprayer1,
                }),
            new CapstoneProofDefinition("killzone_beacon", "with your beacons, fields and remote charges",
                abilityHits: new[]
                {
                    FeatType.BlasterBeacon1, FeatType.BlasterBeacon2, FeatType.BlasterBeacon3,
                    FeatType.ShockBeacon1, FeatType.ShockBeacon2,
                    FeatType.IncendiaryField1, FeatType.IncendiaryField2, FeatType.IncendiaryField3,
                    FeatType.RemoteCharge1, FeatType.RemoteCharge2,
                }),
            new CapstoneProofDefinition("emergency_bunker", "while you keep your allies' gear charged with Power Cell",
                supportStatusEffects: new[]
                {
                    typeof(PowerCell1StatusEffect), typeof(PowerCell2StatusEffect), typeof(PowerCell3StatusEffect),
                }),

            // First Aid
            new CapstoneProofDefinition("emergency_cocktail", "while you keep your allies dosed with combat stims",
                supportStatusEffects: new[]
                {
                    typeof(AdrenalStimStatusEffect), typeof(Shielding1StatusEffect), typeof(Shielding2StatusEffect),
                    typeof(Shielding3StatusEffect), typeof(FocusStim1StatusEffect), typeof(FocusStim2StatusEffect),
                    typeof(Antitoxin1StatusEffect), typeof(PainSuppressant1StatusEffect), typeof(PainSuppressant2StatusEffect),
                }),

            // Leadership
            new CapstoneProofDefinition("hold_the_line", "while your Cleanse Order or Rousing Shout steadies your party",
                supportStatusEffects: new[]
                {
                    typeof(CleanseOrder1StatusEffect), typeof(CleanseOrder2StatusEffect),
                },
                supportAbilityHits: new[] { FeatType.RousingShout1, FeatType.RousingShout2, FeatType.RousingShout3 }),
            new CapstoneProofDefinition("decisive_command", "while your Press the Attack drives your party",
                supportStatusEffects: new[]
                {
                    typeof(PressTheAttack1StatusEffect), typeof(PressTheAttack2StatusEffect), typeof(PressTheAttack3StatusEffect),
                }),

            // Any owned beast can prove these lines through its normal attacks or damaging abilities.
            new CapstoneProofDefinition("primal_overrun", "after your beast hits them", countsOwnedBeastDamage: true),
            new CapstoneProofDefinition("untouchable_instinct", "after your beast hits them", countsOwnedBeastDamage: true),
            new CapstoneProofDefinition("force_bonded_beast", "after your beast hits them", countsOwnedBeastDamage: true),
            new CapstoneProofDefinition("apex_bite", "after your beast hits them", countsOwnedBeastDamage: true),
            new CapstoneProofDefinition("unbreakable_beast", "after your beast hits them", countsOwnedBeastDamage: true),
            new CapstoneProofDefinition("alpha_rhythm", "after your beast hits them", countsOwnedBeastDamage: true),
        };
    }
}

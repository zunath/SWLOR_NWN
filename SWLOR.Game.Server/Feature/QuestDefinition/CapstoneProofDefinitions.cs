using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;
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
            new CapstoneProofDefinition("viral_cascade", "while your venom festers in them",
                enemyStatusEffects: new[] { typeof(VenomStatusEffect), typeof(InfectionStatusEffect) }),
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
            new CapstoneProofDefinition("crippling_defense", "with your heaviest spear techniques",
                abilityHits: new[]
                {
                    FeatType.DisablingStrike3, FeatType.DisablingStrike4, FeatType.DisruptionField2,
                    FeatType.HamperingBarrage2, FeatType.InterruptionStrike2, FeatType.InterruptionStrike3,
                    FeatType.SweepingFlank2, FeatType.SweepingFlank3, FeatType.VigorThrust3, FeatType.VigorThrust4,
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
            new CapstoneProofDefinition("last_stand_of_the_light", "while you shield an ally with Force Intercept",
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
                    typeof(RousingShout1StatusEffect), typeof(RousingShout2StatusEffect), typeof(RousingShout3StatusEffect),
                }),
            new CapstoneProofDefinition("decisive_command", "while your Press the Attack drives your party",
                supportStatusEffects: new[]
                {
                    typeof(PressTheAttack1StatusEffect), typeof(PressTheAttack2StatusEffect), typeof(PressTheAttack3StatusEffect),
                }),

            // Beast Mastery: the beast's actions prove the mechanic for its master.
            new CapstoneProofDefinition("primal_overrun", "with your beast's Crushing Slam or Rampage",
                abilityHits: new[] { FeatType.CrushingSlam1, FeatType.CrushingSlam2, FeatType.CrushingSlam3, FeatType.Rampage1 }),
            new CapstoneProofDefinition("untouchable_instinct", "with your beast's Assault or Distracting Feint",
                abilityHits: new[]
                {
                    FeatType.Assault1, FeatType.Assault2, FeatType.Assault3,
                    FeatType.DistractingFeint1, FeatType.DistractingFeint2, FeatType.DistractingFeint3,
                },
                enemyStatusEffects: new[]
                {
                    typeof(DistractingFeint1StatusEffect), typeof(DistractingFeint2StatusEffect), typeof(DistractingFeint3StatusEffect),
                }),
            new CapstoneProofDefinition("force_bonded_beast", "while your beast's Warding Howl shields you, or with its Force Touch",
                abilityHits: new[] { FeatType.ForceTouch1, FeatType.ForceTouch2, FeatType.ForceTouch3 },
                supportStatusEffects: new[]
                {
                    typeof(WardingHowl1StatusEffect), typeof(WardingHowl2StatusEffect), typeof(WardingHowl3StatusEffect),
                }),
            new CapstoneProofDefinition("apex_bite", "with your beast's Bite, Rending Claw, Pounce or prey strikes",
                abilityHits: new[]
                {
                    FeatType.Bite1, FeatType.Bite2, FeatType.Bite3,
                    FeatType.RendingClaw1, FeatType.RendingClaw2, FeatType.RendingClaw3,
                    FeatType.Pounce1, FeatType.Pounce2, FeatType.ExposePrey1, FeatType.ExecutePrey1,
                }),
            new CapstoneProofDefinition("unbreakable_beast", "while your beast intercepts for you or holds Guarding Roar",
                supportStatusEffects: new[]
                {
                    typeof(Intercept1StatusEffect), typeof(Intercept2StatusEffect),
                    typeof(GuardingRoar1SelfStatusEffect), typeof(GuardingRoar2SelfStatusEffect), typeof(GuardingRoar3SelfStatusEffect),
                }),
            new CapstoneProofDefinition("alpha_rhythm", "with your beast's Claw, Guarded Bite or Coordinated Strike",
                abilityHits: new[]
                {
                    FeatType.Claw1, FeatType.Claw2, FeatType.Claw3,
                    FeatType.GuardedBite1, FeatType.GuardedBite2,
                    FeatType.CoordinatedStrike1, FeatType.CoordinatedStrike2,
                }),
        };
    }
}

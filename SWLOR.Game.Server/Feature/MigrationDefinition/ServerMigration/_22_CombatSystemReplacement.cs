using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.CraftService;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _22_CombatSystemReplacement : ServerMigrationBase, IServerMigration
    {
        private static readonly Dictionary<PerkType, int[]> PlayerRemovedPerks = new()
        {
            { PerkType.DemolitionExpert, new[] { 1, 2, 3 } },
            { PerkType.FlashbangGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoBomb, new[] { 2, 3, 3 } },
            { PerkType.IncendiaryBomb, new[] { 2, 3, 3 } },
            { PerkType.GasBomb, new[] { 2, 3, 3 } },
            { PerkType.StealthGenerator, new[] { 2, 3, 3 } },
            { PerkType.Bloodseeker, new[] { 2, 2 } },
            { PerkType.GuardiansRiposte, new[] { 2 } },

            { PerkType.RangedHealing, new[] { 2, 3, 4, 5 } },
            { PerkType.FrugalMedic, new[] { 1, 2, 2 } },
            { PerkType.KoltoRecovery, new[] { 3, 4, 5 } },
            { PerkType.StasisField, new[] { 2, 3, 4 } },
            { PerkType.CombatEnhancement, new[] { 3, 3, 4 } },

            { PerkType.ForceHeal, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ForceBurst, new[] { 2, 2, 3, 3 } },
            { PerkType.Disturbance, new[] { 2, 2, 2 } },
            { PerkType.ForceValor, new[] { 2, 3 } },
            { PerkType.ThrowRock, new[] { 1, 2, 2, 2, 3 } },
            { PerkType.BurstOfSpeed, new[] { 2, 2 } },
            { PerkType.ThrowLightsaber, new[] { 2, 2, 2 } },
            { PerkType.ForceStun, new[] { 2, 2, 3 } },
            { PerkType.BattleInsight, new[] { 2, 2 } },
            { PerkType.ForceMind, new[] { 3, 4 } },
            { PerkType.Premonition, new[] { 2, 2 } },
            { PerkType.ForceInspiration, new[] { 2, 3, 4 } },

            { PerkType.Dedication, new[] { 1, 2, 2 } },
            { PerkType.SoldiersSpeed, new[] { 2, 2, 2 } },
            { PerkType.SoldiersStrike, new[] { 2, 2, 2 } },
            { PerkType.Charge, new[] { 2, 2 } },
            { PerkType.SoldiersPrecision, new[] { 2, 2, 2 } },
            { PerkType.ShockingShout, new[] { 3 } },
            { PerkType.Rejuvenation, new[] { 2, 2, 2 } },
            { PerkType.FrenziedShout, new[] { 2, 2, 2 } },
            { PerkType.ShoutRange, new[] { 2, 2 } },

            { PerkType.CapacitorRig, new[] { 2, 2, 4 } },
            { PerkType.PulseRelay, new[] { 3, 3 } },

            { PerkType.Decoy, new[] { 3 } },
            { LegacyPerkType(388), new[] { 3 } },
            { PerkType.WhirlwindAssault, new[] { 3, 3 } },

            { PerkType.SeveringStrike, new[] { 2, 2, 3, 5 } },
            { PerkType.DeflectionTraining, new[] { 2 } },
            { PerkType.SeveranceRiposte, new[] { 2, 4, 4 } },
            { PerkType.BladeBlitz, new[] { 4 } },
            { PerkType.LegSlash, new[] { 2, 4, 4 } },
            { PerkType.SeveranceFlow, new[] { 4 } },
            { PerkType.SurgeStrike, new[] { 3, 3 } },
            { PerkType.FocusedStance, new[] { 4 } },
            { PerkType.Purify, new[] { 2 } },
            { PerkType.SaberStorm, new[] { 6 } },
            { PerkType.WardBond, new[] { 2, 2, 3, 5 } },
            { PerkType.GuardiansOath, new[] { 2 } },
            { PerkType.ReactiveWard, new[] { 2, 4, 4 } },
            { PerkType.DeflectivePresence, new[] { 4 } },
            { PerkType.PunishingGuard, new[] { 3, 3 } },
            { PerkType.ImpenetrableGuard, new[] { 4 } },
            { PerkType.GuardiansInfluence, new[] { 4 } },
            { PerkType.OverwhelmingDefense, new[] { 2 } },
            { PerkType.GuardianMaster, new[] { 6 } },

            { PerkType.WeaponBlueprints, new[] { 2, 3, 4, 5, 6 } },
            { PerkType.ArmorBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.AccessoryBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.FurnitureBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.StructureBlueprints, new[] { 6, 6 } },
            { PerkType.StarshipBlueprints, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.EnhancementBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.DroidEquipmentBlueprints, new[] { 1, 1, 2, 3, 3 } },

            { PerkType.AbsoluteDefense, new[] { 4 } },
            { PerkType.AbsoluteImmortality, new[] { 6 } },
            { PerkType.AdamantineGuard, new[] { 4 } },
            { PerkType.AdaptivePrecisionStrike, new[] { 4 } },
            { PerkType.Alacrity, new[] { 3 } },
            { PerkType.AmbushTactics, new[] { 3 } },
            { PerkType.AngerStrike, new[] { 2 } },
            { PerkType.ArcStrike, new[] { 3 } },
            { PerkType.AssassinsFocus, new[] { 3 } },
            { PerkType.Backstab, new[] { 2, 3, 4 } },
            { PerkType.BallisticMastery, new[] { 4 } },
            { PerkType.BastionStance, new[] { 3 } },
            { PerkType.BindingCross, new[] { 3, 4 } },
            { PerkType.BlazingSpikes, new[] { 3 } },
            { PerkType.BleedersEye, new[] { 4 } },
            { PerkType.BloodWeapon, new[] { 3 } },
            { PerkType.BombardierStance, new[] { 2 } },
            { PerkType.Bonecrusher, new[] { 3 } },
            { PerkType.BreakerStance, new[] { 4 } },
            { PerkType.BreachStrike, new[] { 4 } },
            { PerkType.BrutalAssault, new[] { 3 } },
            { PerkType.BrutalEfficiency, new[] { 3 } },
            { PerkType.CalmingStance, new[] { 3 } },
            { PerkType.Carve, new[] { 3 } },
            { PerkType.Centering, new[] { 4 } },
            { PerkType.CenterlineGuard, new[] { 2 } },
            { PerkType.CheapShot, new[] { 2, 3 } },
            { PerkType.ClusterStorm, new[] { 4 } },
            { PerkType.CobraStance, new[] { 2 } },
            { PerkType.CobraReflexes, new[] { 2 } },
            { PerkType.CombatMomentum, new[] { 2, 4, 4 } },
            { PerkType.ConduitFlare, new[] { 3 } },
            { PerkType.CoveringClaws, new[] { 3 } },
            { PerkType.CrimsonFury, new[] { 3 } },
            { PerkType.CripplingPrecision, new[] { 3 } },
            { PerkType.CriticalWard, new[] { 2 } },
            { PerkType.CrushingBlow, new[] { 2 } },
            { PerkType.CrushingStyle, new[] { 3 } },
            { PerkType.CurrentOverload, new[] { 4 } },
            { PerkType.CycloneMastery, new[] { 4 } },
            { PerkType.DeadeyeMastery, new[] { 4 } },
            { PerkType.DeadeyeStance, new[] { 2 } },
            { PerkType.DeadlyPrecision, new[] { 3 } },
            { PerkType.DefensiveHarmony, new[] { 3 } },
            { PerkType.DeflectionCounter, new[] { 3 } },
            { PerkType.DeflectionMastery, new[] { 3 } },
            { PerkType.DeflectionRiposte, new[] { 3 } },
            { PerkType.DuelistStance, new[] { 2 } },
            { PerkType.DuelistsChallenge, new[] { 3 } },
            { PerkType.Earthshatter, new[] { 3, 3 } },
            { PerkType.EssenceCleave, new[] { 2, 2, 3, 5 } },
            { PerkType.EssenceHunter, new[] { 3 } },
            { PerkType.EvasiveCombat, new[] { 2, 3 } },
            { PerkType.ExposedNerve, new[] { 2, 4, 4 } },
            { PerkType.ExposeWeakPoint, new[] { 3 } },
            { PerkType.FeintingCut, new[] { 3, 2, 4 } },
            { PerkType.FerocityStance, new[] { 2 } },
            { PerkType.FieldSedatives, new[] { 2 } },
            { PerkType.FinalForm, new[] { 4 } },
            { PerkType.FinishingToss, new[] { 3 } },
            { PerkType.FireburstToss, new[] { 3 } },
            { PerkType.Flanking, new[] { 3, 2 } },
            { PerkType.FlankingBarrage, new[] { 3 } },
            { PerkType.FlankingStance, new[] { 3 } },
            { PerkType.Flash, new[] { 4 } },
            { PerkType.FlurryStyle, new[] { 2 } },
            { PerkType.ForceCapacitor, new[] { 3 } },
            { PerkType.ForceNullification, new[] { 3 } },
            { PerkType.ForceSuppression, new[] { 3 } },
            { PerkType.FortressStrike, new[] { 2, 2, 4 } },
            { PerkType.FrenzySlash, new[] { 2, 2, 3, 5 } },
            { PerkType.GraveTremor, new[] { 3, 3 } },
            { PerkType.GuardCounter, new[] { 2, 2, 3 } },
            { PerkType.GuardLock, new[] { 3, 3 } },
            { PerkType.GuardedFlow, new[] { 4 } },
            { PerkType.GuardianReflexes, new[] { 4 } },
            { PerkType.GuardiansResolve, new[] { 4 } },
            { PerkType.GunfighterStance, new[] { 2 } },
            { PerkType.GunslingerFocus, new[] { 3 } },
            { PerkType.HackingBlade, new[] { 2, 3, 4 } },
            { PerkType.ImmortalStance, new[] { 4 } },
            { PerkType.ImpenetrableGrip, new[] { 3 } },
            { PerkType.Incapacitate, new[] { 3 } },
            { PerkType.IronElbows, new[] { 4 } },
            { PerkType.IronGuardStance, new[] { 4 } },
            { PerkType.IronWallStance, new[] { 4 } },
            { PerkType.KillZone, new[] { 3 } },
            { PerkType.LateralStrike, new[] { 3, 2 } },
            { PerkType.LowShot, new[] { 3 } },
            { PerkType.MarkedForDeath, new[] { 4 } },
            { PerkType.MarkedTempo, new[] { 2 } },
            { PerkType.MarkingToss, new[] { 3 } },
            { PerkType.MirrorStep, new[] { 3 } },
            { PerkType.NeuralShock, new[] { 3 } },
            { PerkType.NeurotoxinMastery, new[] { 4 } },
            { PerkType.NeutralizingShot, new[] { 4 } },
            { PerkType.Opportunist, new[] { 3 } },
            { PerkType.OpportunistStance, new[] { 4 } },
            { PerkType.Overcharge, new[] { 4 } },
            { PerkType.OverwhelmingStrike, new[] { 3 } },
            { PerkType.PacificationField, new[] { 3 } },
            { PerkType.PatientSentinel, new[] { 3 } },
            { PerkType.PerfectBalance, new[] { 4 } },
            { PerkType.PerfectFootwork, new[] { 4 } },
            { PerkType.PerfectThrow, new[] { 4 } },
            { PerkType.PrecisionArc, new[] { 3 } },
            { PerkType.PrecisionStrikes, new[] { 2 } },
            { PerkType.PunishingAngle, new[] { 2 } },
            { PerkType.PunishingStrike, new[] { 3 } },
            { PerkType.Rampart, new[] { 4 } },
            { PerkType.ReapingStrike, new[] { 2, 4, 4 } },
            { PerkType.ReactiveDeflection, new[] { 2, 3 } },
            { PerkType.RetaliatoryFlow, new[] { 2 } },
            { PerkType.ReversalCut, new[] { 3 } },
            { PerkType.RicochetShot, new[] { 3 } },
            { PerkType.RippleSlash, new[] { 4 } },
            { PerkType.SavageCleave, new[] { 2, 3 } },
            { PerkType.SecondWind, new[] { 3 } },
            { PerkType.SentinelGuard, new[] { 3 } },
            { PerkType.SentinelStance, new[] { 2 } },
            { PerkType.SerpentsEclipse, new[] { 4 } },
            { PerkType.ShadowRecovery, new[] { 3 } },
            { PerkType.ShadowStrike, new[] { 3, 4 } },
            { PerkType.ShelterCircle, new[] { 3 } },
            { PerkType.SideAssault, new[] { 2, 3, 4 } },
            { PerkType.SkirmishersNerve, new[] { 4 } },
            { PerkType.SmokeBomb, new[] { 4, 3 } },
            { PerkType.SmokeRound, new[] { 3 } },
            { PerkType.SoftTarget, new[] { 2 } },
            { PerkType.SoulAmplification, new[] { 3 } },
            { PerkType.SoulReaping, new[] { 4 } },
            { PerkType.SoulSacrifice, new[] { 3 } },
            { PerkType.SoulStorm, new[] { 3 } },
            { PerkType.SplitGuardStrike, new[] { 2, 4, 3 } },
            { PerkType.SpotterStance, new[] { 2 } },
            { PerkType.SpreadingVenom, new[] { 2 } },
            { PerkType.StasisVolley, new[] { 4 } },
            { PerkType.StaticPalm, new[] { 3, 3, 3 } },
            { PerkType.StormRelease, new[] { 3 } },
            { PerkType.StrikingCobra, new[] { 3, 4, 4 } },
            { PerkType.TauntingDeflection, new[] { 4 } },
            { PerkType.TempestRelease, new[] { 3 } },
            { PerkType.TotalForceDenial, new[] { 4 } },
            { PerkType.ToxicCoating, new[] { 2, 3 } },
            { PerkType.ToxicRush, new[] { 3 } },
            { PerkType.ToxicTempo, new[] { 2 } },
            { PerkType.TranqCone, new[] { 3, 3 } },
            { PerkType.TranquilizerShot, new[] { 2, 4 } },
            { PerkType.TwinGuardStance, new[] { 3 } },
            { PerkType.TwinFangFlurry, new[] { 3 } },
            { PerkType.VampiricFury, new[] { 3 } },
            { PerkType.VenomRhythm, new[] { 3 } },
            { PerkType.VenomSplash, new[] { 3 } },
            { PerkType.VersatileStrike, new[] { 2, 3, 3 } },
            { PerkType.VeteranTracker, new[] { 4 } },
            { PerkType.VitalStrike, new[] { 4 } },
            { PerkType.VolatilePayload, new[] { 4 } },
            { PerkType.WardStrike, new[] { 2, 2, 3, 5 } },
            { PerkType.WhirlingGuard, new[] { 4 } },

            { PerkType.BasicSynthesis, new[] { 0, 0, 0, 0 } },
            { PerkType.RapidSynthesisSmithery, new[] { 1 } },
            { PerkType.CarefulSynthesisSmithery, new[] { 1 } },
            { PerkType.BasicTouchSmithery, new[] { 1 } },
            { PerkType.StandardTouchSmithery, new[] { 1 } },
            { PerkType.PreciseTouchSmithery, new[] { 1 } },
            { PerkType.MastersMendSmithery, new[] { 1 } },
            { PerkType.SteadyHandSmithery, new[] { 1 } },
            { PerkType.MuscleMemorySmithery, new[] { 1 } },
            { PerkType.VenerationSmithery, new[] { 1 } },
            { PerkType.WasteNotSmithery, new[] { 1 } },
            { PerkType.RapidSynthesisFabrication, new[] { 1 } },
            { PerkType.CarefulSynthesisFabrication, new[] { 1 } },
            { PerkType.BasicTouchFabrication, new[] { 1 } },
            { PerkType.StandardTouchFabrication, new[] { 1 } },
            { PerkType.PreciseTouchFabrication, new[] { 1 } },
            { PerkType.MastersMendFabrication, new[] { 1 } },
            { PerkType.SteadyHandFabrication, new[] { 1 } },
            { PerkType.MuscleMemoryFabrication, new[] { 1 } },
            { PerkType.VenerationFabrication, new[] { 1 } },
            { PerkType.WasteNotFabrication, new[] { 1 } },
            { PerkType.RapidSynthesisCooking, new[] { 1 } },
            { PerkType.CarefulSynthesisCooking, new[] { 1 } },
            { PerkType.BasicTouchCooking, new[] { 1 } },
            { PerkType.StandardTouchCooking, new[] { 1 } },
            { PerkType.PreciseTouchCooking, new[] { 1 } },
            { PerkType.MastersMendCooking, new[] { 1 } },
            { PerkType.SteadyHandCooking, new[] { 1 } },
            { PerkType.MuscleMemoryCooking, new[] { 1 } },
            { PerkType.VenerationCooking, new[] { 1 } },
            { PerkType.WasteNotCooking, new[] { 1 } },
            { PerkType.CookingRecipes, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.RapidSynthesisEngineering, new[] { 1 } },
            { PerkType.CarefulSynthesisEngineering, new[] { 1 } },
            { PerkType.BasicTouchEngineering, new[] { 1 } },
            { PerkType.StandardTouchEngineering, new[] { 1 } },
            { PerkType.PreciseTouchEngineering, new[] { 1 } },
            { PerkType.MastersMendEngineering, new[] { 1 } },
            { PerkType.SteadyHandEngineering, new[] { 1 } },
            { PerkType.MuscleMemoryEngineering, new[] { 1 } },
            { PerkType.VenerationEngineering, new[] { 1 } },
            { PerkType.WasteNotEngineering, new[] { 1 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> PlayerTrimmedPerks = new()
        {
            { PerkType.IonGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.AdhesiveGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.MedKit, (4, new[] { 1, 2, 3, 4, 4 }) },
            { PerkType.Resuscitation, (2, new[] { 4, 4, 4 }) },
            { PerkType.Shielding, (3, new[] { 2, 3, 3, 4 }) },
        };

        private static readonly Dictionary<PerkType, int[]> BeastRemovedPerks = new()
        {
            { PerkType.FlameBreath, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ShockingSlash, new[] { 1, 1, 1, 2, 2 } },
            { PerkType.DiseasedTouch, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.Clip, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.SpinningClaw, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.BeastSpeed, new[] { 3, 3, 3 } },
            { PerkType.BolsterArmor, new[] { 1, 1, 1, 2, 2 } },
            { PerkType.PredatorRush, new[] { 4 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> BeastTrimmedPerks = new()
        {
            { PerkType.Bite, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Claw, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.BolsterAttack, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Hasten, (2, new[] { 4, 4, 4 }) },
            { PerkType.PoisonBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.IceBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.EvasiveManeuver, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Assault, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.ForceTouch, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Innervate, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Anger, (2, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.FocusAttention, (3, new[] { 2, 2, 2, 3, 3 }) },
        };

        private static readonly IReadOnlyDictionary<string, ResistanceType> ResistanceKeyMap =
            new Dictionary<string, ResistanceType>
            {
                { ((int)ResistanceType.Fire).ToString(), ResistanceType.Fire },
                { ((int)ResistanceType.Poison).ToString(), ResistanceType.Poison },
                { ((int)ResistanceType.Electrical).ToString(), ResistanceType.Electrical },
                { ((int)ResistanceType.Ice).ToString(), ResistanceType.Ice },
                { "5", ResistanceType.Mind },
                { "6", ResistanceType.Mobility },
                { "7", ResistanceType.Trauma },
                { "8", ResistanceType.Disruption },
                { ((int)ResistanceType.Mind).ToString(), ResistanceType.Mind },
                { ((int)ResistanceType.Mobility).ToString(), ResistanceType.Mobility },
                { ((int)ResistanceType.Trauma).ToString(), ResistanceType.Trauma },
                { ((int)ResistanceType.Disruption).ToString(), ResistanceType.Disruption },
            };

        // Player recipe dictionaries serialize RecipeType keys by member name, so renamed
        // enum members must be remapped in raw player JSON before invalid-key cleanup
        // discards them. Recipe unlock data is not reset by the forced full rebuild.
        private static readonly IReadOnlyDictionary<string, string> RenamedRecipeKeys =
            new Dictionary<string, string>
            {
                { "ArmorEnhancementRecastReduction1", "ArmorEnhancementCombatReadiness1" },
                { "ArmorEnhancementRecastReduction2", "ArmorEnhancementCombatReadiness2" },
                { "ArmorEnhancementRecastReduction3", "ArmorEnhancementCombatReadiness3" },
                { "ArmorEnhancementRecastReduction4", "ArmorEnhancementCombatReadiness4" },
                { "ArmorEnhancementRecastReduction5", "ArmorEnhancementCombatReadiness5" },
                { "CookingEnhancementRecastReduction1", "CookingEnhancementCombatReadiness1" },
                { "CookingEnhancementRecastReduction2", "CookingEnhancementCombatReadiness2" },
                { "CookingEnhancementRecastReduction3", "CookingEnhancementCombatReadiness3" },
                { "CookingEnhancementRecastReduction4", "CookingEnhancementCombatReadiness4" },
                { "CookingEnhancementRecastReduction5", "CookingEnhancementCombatReadiness5" },
                { "WeaponEnhancementDamage1", "WeaponEnhancementDMGPhysical1" },
                { "WeaponEnhancementDamage2", "WeaponEnhancementDMGPhysical2" },
                { "WeaponEnhancementDamage3", "WeaponEnhancementDMGPhysical3" },
                { "WeaponEnhancementDamage4", "WeaponEnhancementDMGPhysical4" },
                { "WeaponEnhancementDamage5", "WeaponEnhancementDMGPhysical5" },
                { "WeaponEnhancementForceDamage1", "WeaponEnhancementDMGForce1" },
                { "WeaponEnhancementForceDamage2", "WeaponEnhancementDMGForce2" },
                { "WeaponEnhancementForceDamage3", "WeaponEnhancementDMGForce3" },
                { "WeaponEnhancementForceDamage4", "WeaponEnhancementDMGForce4" },
                { "WeaponEnhancementForceDamage5", "WeaponEnhancementDMGForce5" },
                { "WeaponEnhancementPoisonDamage1", "WeaponEnhancementDMGPoison1" },
                { "WeaponEnhancementPoisonDamage2", "WeaponEnhancementDMGPoison2" },
                { "WeaponEnhancementPoisonDamage3", "WeaponEnhancementDMGPoison3" },
                { "WeaponEnhancementPoisonDamage4", "WeaponEnhancementDMGPoison4" },
                { "WeaponEnhancementPoisonDamage5", "WeaponEnhancementDMGPoison5" },
                { "WeaponEnhancementFireDamage1", "WeaponEnhancementDMGFire1" },
                { "WeaponEnhancementFireDamage2", "WeaponEnhancementDMGFire2" },
                { "WeaponEnhancementFireDamage3", "WeaponEnhancementDMGFire3" },
                { "WeaponEnhancementFireDamage4", "WeaponEnhancementDMGFire4" },
                { "WeaponEnhancementFireDamage5", "WeaponEnhancementDMGFire5" },
                { "WeaponEnhancementIceDamage1", "WeaponEnhancementDMGIce1" },
                { "WeaponEnhancementIceDamage2", "WeaponEnhancementDMGIce2" },
                { "WeaponEnhancementIceDamage3", "WeaponEnhancementDMGIce3" },
                { "WeaponEnhancementIceDamage4", "WeaponEnhancementDMGIce4" },
                { "WeaponEnhancementIceDamage5", "WeaponEnhancementDMGIce5" },
                { "SardineBall", "CookedSardine" },
                // The retired single-step saber upgrade kits are replaced by the
                // recipe-unlocked Chiro kits found through the same loot drops, so
                // existing unlocks carry over to the replacement recipes.
                { nameof(RecipeType.LightsaberUpgradeKit1), nameof(RecipeType.ChiroLightsaberUpgradeKit) },
                { nameof(RecipeType.SaberstaffUpgradeKit1), nameof(RecipeType.ChiroSaberstaffUpgradeKit) },
                // Accept numeric enum keys as well as names in saved recipe dictionaries.
                { RecipeType.LightsaberUpgradeKit1.ToString("D"), nameof(RecipeType.ChiroLightsaberUpgradeKit) },
                { RecipeType.SaberstaffUpgradeKit1.ToString("D"), nameof(RecipeType.ChiroSaberstaffUpgradeKit) },
            };

        private const string SavingThrowPuritiesKey = "SavingThrowPurities";
        private const string SavingThrowWillKey = "Will";
        private const string SavingThrowReflexKey = "Reflex";
        private const string SavingThrowFortitudeKey = "Fortitude";
        private const string SavingThrowWillValue = "3";
        private const string SavingThrowReflexValue = "2";
        private const string SavingThrowFortitudeValue = "1";

        public int Version => 22;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            LogProgress("Starting consolidated server migration.");
            MigratePlayers();
            MigrateBeasts();
            MigrateIncubationJobs();
            LinkedBankStorageMigration.RemoveRetiredUpgradeData();
            LogProgress("Finished consolidated server migration.");
        }

        private static PerkType LegacyPerkType(int perkId)
        {
            return (PerkType)perkId;
        }

        private void MigratePlayers()
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(dbQuery);
            var dbPlayersRaw = DB.SearchRawJson(dbQuery
                .AddPaging(playerCount, 0));
            var progress = new MigrationProgress("players", playerCount);
            progress.Begin();

            foreach (var dbPlayerJson in dbPlayersRaw)
            {
                var jObject = JObject.Parse(dbPlayerJson);
                var dbPlayer = MigratePlayerData(jObject, out var refundAmount);
                EnsureUnknownDisplayName(dbPlayer);
                DB.Set(dbPlayer);

                if (refundAmount > 0)
                    Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP.");

                progress.RecordProcessed(true);
            }

            progress.Finish($"{playerCount} players scanned.");
        }

        private Player MigratePlayerData(JObject jObject, out int refundAmount)
        {
            refundAmount = 0;
            ClearRecastTimes(jObject);
            refundAmount = LegacyPerkRefundMigration.Migrate(jObject, PlayerRemovedPerks.Keys);
            WeaponBlueprintPerkMigration.CollapsePlayerPerks(jObject, out var weaponBlueprintRefundAmount);
            refundAmount += weaponBlueprintRefundAmount;
            DroidBoostRecipeMigration.ExpandPlayerRecipeDictionaries(jObject);
            RenameRecipeKeys(jObject);
            NormalizeResistanceDictionary(jObject, nameof(Player.Resistances));
            SplitDefensesAndResistances(jObject);

            RemoveInvalidEnumDictionaryKeys(jObject["Perks"] as JObject, PlayerRemovedPerks.Keys);
            RemoveInvalidEnumDictionaryKeys<PerkType>(jObject["UnlockedPerks"] as JObject);
            RemoveInvalidEnumDictionaryKeys<RecipeType>(jObject["UnlockedRecipes"] as JObject);
            RemoveInvalidEnumDictionaryKeys<RecipeType>(jObject["CraftedRecipes"] as JObject);
            RemoveInvalidSkillDictionaryKeys(jObject);

            var hasOriginalAppearanceType = jObject[nameof(Player.OriginalAppearanceType)] != null;
            var dbPlayer = jObject.ToObject<Player>();
            if (!hasOriginalAppearanceType)
                dbPlayer.OriginalAppearanceType = AppearanceType.Invalid;

            EnsureDefinedPlayerSkills(dbPlayer);
            CombatReadinessMigration.ResetCombatReadiness(dbPlayer);
            dbPlayer.RebuildComplete = false;

            refundAmount += CleanPerks(
                dbPlayer.Perks,
                PlayerRemovedPerks,
                PlayerTrimmedPerks,
                out _);
            RemoveUnlockedPerks(dbPlayer);

            if (refundAmount > 0)
                dbPlayer.UnallocatedSP += refundAmount;

            return dbPlayer;
        }

        private static void EnsureUnknownDisplayName(Player dbPlayer)
        {
            if (dbPlayer == null ||
                !string.IsNullOrWhiteSpace(PlayerName.SanitizeKnownName(dbPlayer.UnknownDisplayName)))
            {
                return;
            }

            var generatedDisplayName = PlayerDescriptor.GenerateUnknownDisplayName(dbPlayer);
            if (string.IsNullOrWhiteSpace(generatedDisplayName))
                return;

            dbPlayer.UnknownDisplayName = generatedDisplayName;
        }

        private static void MigrateBeasts()
        {
            var query = new DBQuery<Beast>();
            var count = (int)DB.SearchCount(query);
            var beasts = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;
            var totalRefund = 0;
            var progress = new MigrationProgress("beasts", count);
            progress.Begin();

            foreach (var rawBeast in beasts)
            {
                var jObject = JObject.Parse(rawBeast);
                var migrated = false;
                migrated |= AddResistancePurities(jObject);
                migrated |= jObject.Remove(SavingThrowPuritiesKey);
                migrated |= NormalizeResistanceDictionary(jObject, nameof(Beast.ResistancePurities));
                migrated |= MigratePurities(jObject);

                // Every learned beast perk is refunded. Clear the raw dictionary
                // before deserialization so retired enum names cannot block it.
                migrated |= ClearBeastPerks(jObject);
                var beast = jObject.ToObject<Beast>();
                var refund = RefundBeastPerks(beast, out var perkChanged);
                migrated |= perkChanged;

                if (refund > 0)
                {
                    totalRefund += refund;
                    migrated = true;
                }

                if (!migrated)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                DB.Set(beast);
                migratedCount++;
                progress.RecordProcessed(true);
            }

            Log.Write(LogGroup.Migration, $"Migration #22: Migrated beast combat data for {migratedCount} beasts and refunded {totalRefund} SP.", true);
            progress.Finish($"{migratedCount}/{count} beasts changed. Refunded {totalRefund} SP.");
        }

        private static void MigrateIncubationJobs()
        {
            var query = new DBQuery<IncubationJob>();
            var count = (int)DB.SearchCount(query);
            var jobs = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;
            var progress = new MigrationProgress("incubation jobs", count);
            progress.Begin();

            foreach (var rawJob in jobs)
            {
                var jObject = JObject.Parse(rawJob);
                var migrated = false;
                migrated |= AddResistancePurities(jObject);
                migrated |= jObject.Remove(SavingThrowPuritiesKey);
                migrated |= NormalizeResistanceDictionary(jObject, nameof(IncubationJob.ResistancePurities));
                migrated |= MigratePurities(jObject);

                if (!migrated)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                var job = jObject.ToObject<IncubationJob>();
                DB.Set(job);
                migratedCount++;
                progress.RecordProcessed(true);
            }

            Log.Write(LogGroup.Migration, $"Migration #22: Migrated incubation job combat data for {migratedCount} jobs.", true);
            progress.Finish($"{migratedCount}/{count} incubation jobs changed.");
        }

        private static bool SplitDefensesAndResistances(JObject player)
        {
            var migrated = false;
            var defenses = player[nameof(Player.Defenses)] as JObject;
            var resistances = player[nameof(Player.Resistances)] as JObject;

            if (defenses == null)
            {
                defenses = new JObject();
                player[nameof(Player.Defenses)] = defenses;
                migrated = true;
            }

            if (resistances == null)
            {
                resistances = new JObject();
                player[nameof(Player.Resistances)] = resistances;
                migrated = true;
            }

            migrated |= MoveDefenseValue(defenses, resistances, CombatDamageType.Physical);
            migrated |= MoveDefenseValue(defenses, resistances, CombatDamageType.Force);

            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Ice, ResistanceType.Ice);

            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Ice, ResistanceType.Ice);

            migrated |= RemoveResistanceKeys(resistances, CombatDamageType.Physical);
            migrated |= RemoveResistanceKeys(resistances, CombatDamageType.Force);

            foreach (var type in Enum.GetValues(typeof(CombatDamageType)).Cast<CombatDamageType>())
            {
                if (!type.IsDefenseDamageType())
                    continue;

                migrated |= NormalizeDefenseValue(defenses, type);

                if (defenses[type.ToString()] != null)
                    continue;

                defenses[type.ToString()] = 0;
                migrated = true;
            }

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                if (resistances[type.ToString()] != null)
                    continue;

                resistances[type.ToString()] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveDefenseValue(JObject defenses, JObject resistances, CombatDamageType type)
        {
            var migrated = false;
            var key = type.ToString();
            var resistanceToken = GetToken(resistances, key, (int)type);
            var defenseToken = GetToken(defenses, key, (int)type);

            if (defenseToken == null && resistanceToken != null)
            {
                defenses[key] = resistanceToken.DeepClone();
                migrated = true;
            }

            return migrated;
        }

        private static bool NormalizeDefenseValue(JObject defenses, CombatDamageType type)
        {
            var migrated = false;
            var key = type.ToString();
            var numericKey = ((int)type).ToString();
            var token = defenses[key] ?? defenses[numericKey];

            if (defenses[key] == null && token != null)
            {
                defenses[key] = token.DeepClone();
                migrated = true;
            }

            if (defenses.Remove(numericKey))
                migrated = true;

            return migrated;
        }

        private static bool MoveLegacyElementalDefense(
            JObject defenses,
            JObject resistances,
            CombatDamageType legacyType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var targetKey = resistanceType.ToString();
            var legacyNameKey = legacyType.ToString();
            var legacyNumericKey = ((int)legacyType).ToString();
            var legacyToken = GetToken(defenses, legacyType.ToString(), (int)legacyType);

            migrated |= MergeResistanceValue(resistances, targetKey, resistances[legacyNameKey]);
            migrated |= MergeResistanceValue(resistances, targetKey, resistances[legacyNumericKey]);
            migrated |= MergeResistanceValue(resistances, targetKey, legacyToken);

            foreach (var key in new[] { legacyNameKey, legacyNumericKey })
            {
                if (defenses.Remove(key))
                    migrated = true;
            }

            return migrated;
        }

        private static bool NormalizeLegacyElementalResistance(
            JObject resistances,
            CombatDamageType legacyType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var key = resistanceType.ToString();
            var legacyNumericKey = ((int)legacyType).ToString();

            migrated |= MergeResistanceValue(resistances, key, resistances[legacyType.ToString()]);
            migrated |= MergeResistanceValue(resistances, key, resistances[legacyNumericKey]);

            if (resistances.Remove(legacyNumericKey))
                migrated = true;

            return migrated;
        }

        private static bool RemoveResistanceKeys(JObject resistances, CombatDamageType type)
        {
            var migrated = false;

            foreach (var key in new[] { type.ToString(), ((int)type).ToString() })
            {
                if (resistances.Remove(key))
                    migrated = true;
            }

            return migrated;
        }

        private static bool AddResistancePurities(JObject entity)
        {
            var migrated = false;
            var defensePurities = entity[nameof(Beast.DefensePurities)] as JObject;
            var savingThrowPurities = entity[SavingThrowPuritiesKey] as JObject;
            var resistancePurities = entity[nameof(Beast.ResistancePurities)] as JObject;

            if (resistancePurities == null)
            {
                resistancePurities = new JObject();
                entity[nameof(Beast.ResistancePurities)] = resistancePurities;
                migrated = true;
            }

            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Fire, GetToken(defensePurities, CombatDamageType.Fire));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Poison, GetToken(defensePurities, CombatDamageType.Poison));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Electrical, GetToken(defensePurities, CombatDamageType.Electrical));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Ice, GetToken(defensePurities, CombatDamageType.Ice));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Mind, GetToken(savingThrowPurities, SavingThrowWillKey, SavingThrowWillValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Mobility, GetToken(savingThrowPurities, SavingThrowReflexKey, SavingThrowReflexValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Trauma, GetToken(savingThrowPurities, SavingThrowFortitudeKey, SavingThrowFortitudeValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Disruption, GetToken(defensePurities, CombatDamageType.Force));

            return migrated;
        }

        private static bool AddResistancePurity(JObject resistancePurities, ResistanceType type, JToken legacyToken)
        {
            var key = type.ToString();
            if (resistancePurities[key] != null)
                return false;

            resistancePurities[key] = legacyToken?.DeepClone() ?? 0;
            return true;
        }

        private static bool NormalizeResistanceDictionary(JObject entity, string propertyName)
        {
            var migrated = false;
            var resistances = entity[propertyName] as JObject;

            if (resistances == null)
            {
                resistances = new JObject();
                entity[propertyName] = resistances;
                migrated = true;
            }

            foreach (var pair in ResistanceKeyMap)
            {
                migrated |= MoveResistanceValue(resistances, pair.Key, pair.Value.ToString());
            }

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                var key = type.ToString();
                if (resistances[key] != null)
                    continue;

                resistances[key] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveResistanceValue(JObject resistances, string sourceKey, string targetKey)
        {
            if (sourceKey == targetKey)
                return false;

            var sourceToken = resistances[sourceKey];
            if (sourceToken == null)
                return false;

            MergeResistanceValue(resistances, targetKey, sourceToken);

            resistances.Remove(sourceKey);
            return true;
        }

        private static bool MergeResistanceValue(JObject resistances, string targetKey, JToken sourceToken)
        {
            if (sourceToken == null)
                return false;

            var sourceValue = GetInt(sourceToken);
            var targetToken = resistances[targetKey];
            if (targetToken == null)
            {
                resistances[targetKey] = sourceToken.DeepClone();
                return true;
            }

            var targetValue = GetInt(targetToken);
            var mergedValue = MergeResistanceValues(targetValue, sourceValue);
            if (targetValue == mergedValue)
                return false;

            resistances[targetKey] = mergedValue;
            return true;
        }

        private static int MergeResistanceValues(int targetValue, int sourceValue)
        {
            if (targetValue == 0)
                return sourceValue;

            if (sourceValue == 0)
                return targetValue;

            return Math.Max(targetValue, sourceValue);
        }

        private static bool MigratePurities(JObject entity)
        {
            var migrated = false;
            var defensePurities = GetOrCreateObject(entity, nameof(Beast.DefensePurities), ref migrated);
            var resistancePurities = GetOrCreateObject(entity, nameof(Beast.ResistancePurities), ref migrated);

            migrated |= NormalizeDefensePurity(defensePurities, CombatDamageType.Physical);
            migrated |= NormalizeDefensePurity(defensePurities, CombatDamageType.Force);

            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Ice, ResistanceType.Ice);

            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                migrated |= NormalizeResistancePurity(resistancePurities, resistanceType);
            }

            return migrated;
        }

        private static JObject GetOrCreateObject(JObject entity, string propertyName, ref bool migrated)
        {
            if (entity[propertyName] is JObject existing)
                return existing;

            var created = new JObject();
            entity[propertyName] = created;
            migrated = true;

            return created;
        }

        private static bool NormalizeDefensePurity(JObject defensePurities, CombatDamageType type)
        {
            var migrated = false;
            var targetKey = type.ToString();
            var numericKey = ((int)type).ToString();
            var targetToken = defensePurities[targetKey];
            var numericToken = defensePurities[numericKey];

            if (targetToken == null && numericToken != null)
            {
                defensePurities[targetKey] = numericToken.DeepClone();
                migrated = true;
            }
            else if (targetToken != null && numericToken != null)
            {
                defensePurities[targetKey] = Math.Max(GetInt(targetToken), GetInt(numericToken));
                migrated = true;
            }

            if (numericToken != null)
            {
                defensePurities.Remove(numericKey);
                migrated = true;
            }

            if (defensePurities[targetKey] == null)
            {
                defensePurities[targetKey] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveDefensePurityToResistance(
            JObject defensePurities,
            JObject resistancePurities,
            CombatDamageType damageType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var nameKey = damageType.ToString();
            var numericKey = ((int)damageType).ToString();
            var sourceToken = defensePurities[nameKey] ?? defensePurities[numericKey];

            if (sourceToken != null)
            {
                migrated |= MergeResistancePurity(resistancePurities, resistanceType, GetInt(sourceToken));
            }

            if (defensePurities.Remove(nameKey))
                migrated = true;

            if (defensePurities.Remove(numericKey))
                migrated = true;

            return migrated;
        }

        private static bool NormalizeResistancePurity(JObject resistancePurities, ResistanceType type)
        {
            var migrated = false;
            var targetKey = type.ToString();
            var numericKey = ((int)type).ToString();
            var targetToken = resistancePurities[targetKey];
            var numericToken = resistancePurities[numericKey];

            if (targetToken == null && numericToken != null)
            {
                resistancePurities[targetKey] = numericToken.DeepClone();
                migrated = true;
            }
            else if (targetToken != null && numericToken != null)
            {
                resistancePurities[targetKey] = Math.Max(GetInt(targetToken), GetInt(numericToken));
                migrated = true;
            }

            if (numericToken != null)
            {
                resistancePurities.Remove(numericKey);
                migrated = true;
            }

            if (resistancePurities[targetKey] == null)
            {
                resistancePurities[targetKey] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MergeResistancePurity(JObject resistancePurities, ResistanceType type, int value)
        {
            var key = type.ToString();
            var existingValue = GetInt(resistancePurities[key]);
            var newValue = Math.Max(existingValue, value);

            if (resistancePurities[key] != null && existingValue == newValue)
                return false;

            resistancePurities[key] = newValue;
            return true;
        }

        private static int CleanPerks(
            Dictionary<PerkType, int> perks,
            Dictionary<PerkType, int[]> removedPerks,
            Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> trimmedPerks,
            out bool changed)
        {
            changed = false;
            if (perks == null)
                return 0;

            var refund = 0;

            foreach (var (perkType, pricesByLevel) in removedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel))
                    continue;

                refund += CalculateRefund(pricesByLevel, 1, purchasedLevel);
                perks.Remove(perkType);
                changed = true;
            }

            foreach (var (perkType, trim) in trimmedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel) ||
                    purchasedLevel <= trim.MaxLevel)
                    continue;

                refund += CalculateRefund(trim.PricesByLevel, trim.MaxLevel + 1, purchasedLevel);
                perks[perkType] = trim.MaxLevel;
                changed = true;
            }

            return refund;
        }

        private static int RefundBeastPerks(Beast beast, out bool changed)
        {
            changed = false;
            if (beast == null)
                return 0;

            var totalSkillPoints = Math.Clamp(beast.Level, 0, BeastMastery.MaxLevel);
            var refund = Math.Max(0, totalSkillPoints - beast.UnallocatedSP);

            if (beast.Perks == null)
            {
                beast.Perks = new Dictionary<PerkType, int>();
                changed = true;
            }
            else if (beast.Perks.Count > 0)
            {
                beast.Perks.Clear();
                changed = true;
            }

            if (beast.UnallocatedSP != totalSkillPoints)
            {
                beast.UnallocatedSP = totalSkillPoints;
                changed = true;
            }

            return refund;
        }

        private static bool ClearBeastPerks(JObject beast)
        {
            var changed = beast[nameof(Beast.Perks)] is not JObject perks || perks.HasValues;
            beast[nameof(Beast.Perks)] = new JObject();
            return changed;
        }

        private static int CalculateRefund(int[] pricesByLevel, int fromLevel, int purchasedLevel)
        {
            var refund = 0;
            var maxLevel = purchasedLevel > pricesByLevel.Length
                ? pricesByLevel.Length
                : purchasedLevel;

            for (var level = fromLevel; level <= maxLevel; level++)
            {
                refund += pricesByLevel[level - 1];
            }

            return refund;
        }

        private static bool RemoveUnlockedPerks(Player player)
        {
            if (player.UnlockedPerks == null)
                return false;

            var changed = false;
            foreach (var perkType in PlayerRemovedPerks.Keys)
            {
                changed |= player.UnlockedPerks.Remove(perkType);
            }

            return changed;
        }

        private static JToken GetToken(JObject obj, string name, int value)
        {
            return obj?[name] ?? obj?[value.ToString()];
        }

        private static JToken GetToken(JObject obj, CombatDamageType type)
        {
            return obj?[type.ToString()] ?? obj?[((int)type).ToString()];
        }

        private static JToken GetToken(JObject obj, string nameKey, string numericKey)
        {
            return obj?[nameKey] ?? obj?[numericKey];
        }

        private static int GetInt(JToken token)
        {
            return int.TryParse(token?.ToString(), out var value)
                ? value
                : 0;
        }

        private static void LogProgress(string message)
        {
            Log.Write(LogGroup.Migration, $"Migration #22: {message}", true);
        }

        private sealed class MigrationProgress
        {
            private const int PercentReportStep = 5;
            private const int RecordReportStep = 500;

            private readonly string _sectionName;
            private readonly int _totalCount;
            private int _nextPercentReport = PercentReportStep;
            private int _lastRecordReport;

            private int ProcessedCount { get; set; }
            private int ChangedCount { get; set; }

            public MigrationProgress(string sectionName, int totalCount)
            {
                _sectionName = sectionName;
                _totalCount = totalCount;
            }

            public void Begin()
            {
                LogProgress($"Scanning {_sectionName} ({_totalCount} records). {BuildProgressText()}");
            }

            public void RecordProcessed(bool changed)
            {
                ProcessedCount++;

                if (changed)
                    ChangedCount++;

                if (ShouldReportProgress())
                    ReportProgress();
            }

            public void Finish(string details)
            {
                LogProgress($"Finished {_sectionName}. {details} {BuildProgressText()}");
            }

            private bool ShouldReportProgress()
            {
                if (_totalCount <= 0)
                    return false;

                var percent = GetPercent();
                if (percent >= _nextPercentReport)
                {
                    while (_nextPercentReport <= percent)
                    {
                        _nextPercentReport += PercentReportStep;
                    }

                    return true;
                }

                if (ProcessedCount - _lastRecordReport < RecordReportStep)
                    return false;

                _lastRecordReport = ProcessedCount;
                return true;
            }

            private void ReportProgress()
            {
                _lastRecordReport = ProcessedCount;
                LogProgress(BuildProgressText());
            }

            private string BuildProgressText()
            {
                return _totalCount <= 0
                    ? $"Current migration progress: {_sectionName} 0/0 records (100.0%), 0 changed."
                    : $"Current migration progress: {_sectionName} {ProcessedCount}/{_totalCount} records ({GetPercent():0.0}%), {ChangedCount} changed.";
            }

            private double GetPercent()
            {
                return _totalCount <= 0
                    ? 100.0
                    : ProcessedCount * 100.0 / _totalCount;
            }
        }

        private static bool ClearRecastTimes(JObject player)
        {
            if (player == null)
                return false;

            if (player[nameof(Player.RecastTimes)] is not JObject recastTimes)
            {
                player[nameof(Player.RecastTimes)] = new JObject();
                return true;
            }

            if (!recastTimes.HasValues)
                return false;

            recastTimes.RemoveAll();
            return true;
        }

        private static void RenameRecipeKeys(JObject player)
        {
            RenameRecipeDictionaryKeys(player[nameof(Player.UnlockedRecipes)] as JObject);
            RenameRecipeDictionaryKeys(player[nameof(Player.CraftedRecipes)] as JObject);
        }

        private static void RenameRecipeDictionaryKeys(JObject dictionary)
        {
            if (dictionary == null)
                return;

            foreach (var property in dictionary.Properties().ToList())
            {
                if (!RenamedRecipeKeys.TryGetValue(property.Name, out var newRecipeName))
                    continue;

                if (dictionary[newRecipeName] == null)
                    dictionary[newRecipeName] = property.Value.DeepClone();

                property.Remove();
            }
        }

        private static void RemoveInvalidEnumDictionaryKeys<TEnum>(JObject dictionary, IEnumerable<TEnum> refundableValues = null)
            where TEnum : struct, Enum
        {
            if (dictionary == null)
                return;

            var keysToRemove = new List<JProperty>();
            var refundable = refundableValues?.ToHashSet();

            foreach (var property in dictionary.Properties())
            {
                if (Enum.TryParse(property.Name, out TEnum value) &&
                    (Enum.IsDefined(typeof(TEnum), value) || refundable?.Contains(value) == true))
                {
                    continue;
                }

                keysToRemove.Add(property);
            }

            foreach (var property in keysToRemove)
            {
                property.Remove();
            }
        }

        private static void RemoveInvalidSkillDictionaryKeys(JObject player)
        {
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["Skills"] as JObject);
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["Control"] as JObject);
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["Craftsmanship"] as JObject);
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["CPBonus"] as JObject);

            if (player["Settings"] is JObject settings)
                RemoveInvalidEnumDictionaryKeys<SkillType>(settings["LanguageChatColors"] as JObject);
        }

        private static void EnsureDefinedPlayerSkills(Player dbPlayer)
        {
            dbPlayer.Skills ??= new Dictionary<SkillType, PlayerSkill>();
            dbPlayer.Control ??= new Dictionary<SkillType, int>();
            dbPlayer.Craftsmanship ??= new Dictionary<SkillType, int>();
            dbPlayer.CPBonus ??= new Dictionary<SkillType, int>();
            dbPlayer.Settings ??= new PlayerSettings();
            dbPlayer.Settings.LanguageChatColors ??= new Dictionary<SkillType, PlayerColor>();

            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                if (skillType == SkillType.Invalid)
                    continue;

                dbPlayer.Skills.TryAdd(skillType, new PlayerSkill());
            }
        }
    }

    // The same migration version finishes its native item work only once the skill
    // and recipe caches are available. Raw enum repairs must still precede caching.
    public sealed class StoredItemSchemaMigration : IServerMigration
    {
        public int Version => 22;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

        public void Migrate()
        {
            StoredItemDataMigration.Migrate();
            LinkedBankStorageMigration.MigrateInventoryItemsToGlobalBank();
        }
    }
}

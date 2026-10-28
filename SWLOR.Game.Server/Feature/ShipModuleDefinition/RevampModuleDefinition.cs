using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    /// <summary>
    /// Registers every module step of the space revamp (<see cref="RevampModuleCatalog"/>). Rank gating is the
    /// RequiresSkill property on each item, so no module requires a perk.
    /// </summary>
    public class RevampModuleDefinition : IShipModuleListDefinition
    {
        /// <summary>Modules that act on the firing ship or an aura around it never check the distance to a target.</summary>
        private const float SelfRange = 1000f;

        private const int MaxAuraAllies = 6;
        private const int MaxAuraEnemies = 10;

        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            foreach (var step in RevampModuleCatalog.Steps)
            {
                Build(step);
            }

            return _builder.Build();
        }

        private void Build(ShipModuleStep step)
        {
            _builder.Create(step.Tag)
                .Name(step.Name)
                .ShortName(step.ShortName)
                .Texture(step.Texture)
                .Description(step.Description)
                .PowerType(step.Row)
                .Type(GetModuleType(step.Family))
                .Step(step);

            if (step.Capital)
                _builder.CapitalClassModule();

            switch (step.Activation)
            {
                case ShipModuleActivation.Toggle:
                    _builder.Toggle((float)step.CycleSeconds).Capacitor(step.CapacitorCost);
                    break;
                case ShipModuleActivation.OnePress:
                    _builder.Recast((float)step.CycleSeconds).OnePress().Capacitor(step.CapacitorCost);
                    break;
            }

            switch (step.Family)
            {
                case ShipModuleFamily.LaserCannon:
                case ShipModuleFamily.PointDefenseTurret:
                case ShipModuleFamily.Turbolaser:
                case ShipModuleFamily.QuadLaser:
                case ShipModuleFamily.IonCannon:
                case ShipModuleFamily.StormCannon:
                case ShipModuleFamily.BeamCannon:
                case ShipModuleFamily.LaserBattery:
                    Weapon(step);
                    break;
                case ShipModuleFamily.MissileLauncher:
                    AmmoWeapon(step, "You need a missile to fire this weapon.");
                    break;
                case ShipModuleFamily.TorpedoLauncher:
                    AmmoWeapon(step, "You need a proton bomb to fire this weapon.");
                    break;
                case ShipModuleFamily.AssaultConcussionMissile:
                    AmmoWeapon(step, "You need an assault concussion missile to fire this weapon.");
                    break;
                case ShipModuleFamily.MiningLaser:
                    MiningLaser(step);
                    break;
                case ShipModuleFamily.StripMiner:
                    StripMiner(step);
                    break;
                case ShipModuleFamily.GasHarvester:
                    GasHarvester(step);
                    break;
                case ShipModuleFamily.Salvager:
                    Salvager(step);
                    break;
                case ShipModuleFamily.ShieldRepairer:
                    Repairer(step, ShipModuleActions.RepairShield);
                    break;
                case ShipModuleFamily.HullRepairer:
                    Repairer(step, ShipModuleActions.RepairHull);
                    break;
                case ShipModuleFamily.ShieldHardener:
                    ShieldHardener(step);
                    break;
                case ShipModuleFamily.HypermatterInjector:
                    HypermatterInjector(step);
                    break;
                case ShipModuleFamily.BulwarkShieldGenerator:
                    BulwarkShieldGenerator(step);
                    break;
                case ShipModuleFamily.RepairField:
                    RepairField(step);
                    break;
                case ShipModuleFamily.EWarArray:
                    EWarArray(step);
                    break;
                case ShipModuleFamily.ProbeLauncher:
                    ProbeLauncher(step);
                    break;
                case ShipModuleFamily.DataAnalyzer:
                    DataAnalyzer(step);
                    break;
                case ShipModuleFamily.CloakingDevice:
                    CloakingDevice(step);
                    break;
                case ShipModuleFamily.IonWeb:
                    IonWeb(step);
                    break;
                case ShipModuleFamily.Interdictor:
                    Interdictor(step);
                    break;
                case ShipModuleFamily.Jammer:
                    HostileElectronicWarfare(step, Space.ApplyJam);
                    break;
                case ShipModuleFamily.TargetPainter:
                    HostileElectronicWarfare(step, Space.ApplyPaint);
                    break;
                case ShipModuleFamily.Afterburner:
                    Afterburner(step);
                    break;
            }
        }

        private static ShipModuleType GetModuleType(ShipModuleFamily family)
        {
            return family switch
            {
                ShipModuleFamily.LaserCannon => ShipModuleType.CombatLaser,
                ShipModuleFamily.PointDefenseTurret => ShipModuleType.CombatLaser,
                ShipModuleFamily.IonCannon => ShipModuleType.IonCannon,
                ShipModuleFamily.BeamCannon => ShipModuleType.BeamLaser,
                ShipModuleFamily.MissileLauncher => ShipModuleType.Missile,
                ShipModuleFamily.TorpedoLauncher => ShipModuleType.ProtonBomb,
                ShipModuleFamily.Turbolaser => ShipModuleType.TurboLaser,
                ShipModuleFamily.QuadLaser => ShipModuleType.QuadLaser,
                ShipModuleFamily.StormCannon => ShipModuleType.StormCannon,
                ShipModuleFamily.LaserBattery => ShipModuleType.LaserBattery,
                ShipModuleFamily.AssaultConcussionMissile => ShipModuleType.AssaultConcussionMissile,
                ShipModuleFamily.MiningLaser => ShipModuleType.MiningLaser,
                ShipModuleFamily.StripMiner => ShipModuleType.StripMiner,
                ShipModuleFamily.GasHarvester => ShipModuleType.GasHarvester,
                ShipModuleFamily.Salvager => ShipModuleType.Salvager,
                ShipModuleFamily.ShieldRepairer => ShipModuleType.ShieldRepairer,
                ShipModuleFamily.HullRepairer => ShipModuleType.HullRepairer,
                ShipModuleFamily.ShieldHardener => ShipModuleType.ShieldHardener,
                ShipModuleFamily.HypermatterInjector => ShipModuleType.HypermatterInjector,
                ShipModuleFamily.BulwarkShieldGenerator => ShipModuleType.BulwarkShieldGenerator,
                ShipModuleFamily.RepairField => ShipModuleType.RepairFieldGenerator,
                ShipModuleFamily.EWarArray => ShipModuleType.CapitalEwar,
                ShipModuleFamily.ProbeLauncher => ShipModuleType.ProbeLauncher,
                ShipModuleFamily.DataAnalyzer => ShipModuleType.DataAnalyzer,
                ShipModuleFamily.CloakingDevice => ShipModuleType.CloakingDevice,
                ShipModuleFamily.IonWeb => ShipModuleType.IonWeb,
                ShipModuleFamily.Interdictor => ShipModuleType.Interdictor,
                ShipModuleFamily.Jammer => ShipModuleType.Jammer,
                ShipModuleFamily.TargetPainter => ShipModuleType.TargetPainter,
                ShipModuleFamily.Afterburner => ShipModuleType.Afterburner,
                _ => ShipModuleType.Passive,
            };
        }

        private static SpaceWeaponShot BuildShot(ShipModuleStep step, ShipStatus status, ShipStatus targetStatus) =>
            ShipShotBuilder.BuildFor(step, status, targetStatus);

        /// <summary>The step as the firing module's component traits make it.</summary>
        private static ShipModuleStep Effective(ShipModuleStep step) => ShipTraitRules.Apply(step, ShipModuleTraitContext.Current);

        private void Weapon(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ActivatedAction((activator, status, target, targetStatus, _) =>
                {
                    var fired = Effective(step);
                    ShipModuleActions.FireStep(
                        fired, activator, status, target, targetStatus, BuildShot(fired, status, targetStatus), false);
                });
        }

        /// <summary>
        /// A launcher: it spends one ammo item per shot, the pilot's loaded variant first, then plain ammo, then any
        /// variant carried, and a variant changes the shot and adds its rider (<see cref="ShipAmmoRules"/>).
        /// </summary>
        private void AmmoWeapon(ShipModuleStep step, string missingAmmoMessage)
        {
            var ammoResref = ShipModuleActions.GetAmmoResref(step.Family);
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ValidationAction((activator, status, _, targetStatus, _) =>
                {
                    var ammoError = ShipModuleActions.LauncherAmmoError(activator, step.Family, ammoResref, missingAmmoMessage);
                    return string.IsNullOrWhiteSpace(ammoError)
                        ? ShipNpcRules.TargetError(step.Family, status, targetStatus?.Size ?? ShipSize.Small)
                        : ammoError;
                })
                .ActivatedAction((activator, status, target, targetStatus, _) =>
                {
                    var fired = Effective(step);
                    var ammo = ShipModuleActions.PickAmmo(activator, step.Family, ammoResref);
                    var variant = ShipAmmoRules.Variant(ammo);
                    var shot = ShipAmmoRules.Apply(BuildShot(fired, status, targetStatus), variant);
                    ShipModuleActions.ConsumeAmmo(activator, ammo ?? ammoResref);
                    ShipModuleActions.FireStep(fired, activator, status, target, targetStatus, shot, false,
                        ShipModuleActions.AmmoRider(activator, target, shot, variant));
                });
        }

        private void MiningLaser(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((activator, _, target, _, _) => ShipModuleActions.MiningLaserError(activator, target, step.MiningTier))
                .Recast((activator, _, _) => ShipModuleActions.MiningCycleSeconds(activator, (float)step.CycleSeconds))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                    ShipModuleActions.MineAsteroid(activator, target, (float)step.CycleSeconds, moduleBonus));
        }

        private void StripMiner(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((activator, status, target, _, _) =>
                {
                    if (status.Size != ShipSize.Large && status.Size != ShipSize.Capital)
                        return "A strip miner needs a Large or Capital hull.";

                    return ShipModuleActions.StripMinerError(activator, target, step.MiningTier);
                })
                .Recast((activator, _, _) => ShipModuleActions.MiningCycleSeconds(activator, (float)step.CycleSeconds))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                    ShipModuleActions.StripAsteroid(activator, target, (float)step.CycleSeconds, moduleBonus));
        }

        private void GasHarvester(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((activator, _, target, _, _) => ShipSalvageActions.GasHarvesterError(activator, target, step.MiningTier))
                .Recast((activator, _, _) => ShipModuleActions.MiningCycleSeconds(activator, (float)step.CycleSeconds))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                    ShipSalvageActions.HarvestGasCloud(activator, target, (float)step.CycleSeconds, moduleBonus));
        }

        private void Salvager(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((activator, status, target, _, _) => ShipSalvageActions.SalvagerError(activator, status, target, step.MiningTier))
                .Recast((activator, _, _) => ShipModuleActions.MiningCycleSeconds(activator, (float)step.CycleSeconds))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                    ShipSalvageActions.SalvageWreck(activator, target, (float)step.CycleSeconds, moduleBonus));
        }

        private void Repairer(ShipModuleStep step, System.Action<uint, ShipStatus, uint, ShipStatus, int> repair)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ActivatedAction((activator, status, target, targetStatus, _) =>
                    repair(activator, status, target, targetStatus, ShipShotBuilder.ScaleSupport(Effective(step).Repair, status.Size)));
        }

        private void ShieldHardener(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, status, _, _, _) =>
                {
                    status.Harden(Effective(step).ResistAll, step.CycleSeconds, DateTime.UtcNow);
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), activator, 1.0f);
                });
        }

        private void HypermatterInjector(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ValidTargetType(ObjectType.Creature)
                .ValidationAction((activator, _, _, _, _) =>
                    ShipModuleActions.AmmoError(activator, ShipModuleActions.FuelCapsuleResref, "You need a fuel capsule to activate this module."))
                .ActivatedAction((activator, status, _, _, _) =>
                    ShipModuleActions.InjectCapacitor(activator, status, activator, status, ShipShotBuilder.ScaleSupport(Effective(step).CapacitorRestore, status.Size)));
        }

        private void BulwarkShieldGenerator(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, _, _, _, _) =>
                {
                    var fired = Effective(step);
                    var seconds = step.CycleSeconds + 0.5;
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), activator, (float)step.CycleSeconds);

                    var supported = new List<uint>();
                    foreach (var ally in ShipModuleActions.GetNearbyShips(activator, step.Range, MaxAuraAllies, true))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), ally, 2.0f);
                        ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), ally, 2.0f);

                        Space.ApplyShieldAura(ally, Space.GetShipStatus(ally), fired.AuraShieldPercent, seconds);
                        supported.Add(ally);
                    }

                    Enmity.ModifyEnmityOnAll(activator, 100);
                    ShipModuleActions.AddSupportCombatPoint(activator, supported);
                });
        }

        private void RepairField(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, _, _, _, _) =>
                {
                    var repair = Effective(step).Repair;
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_White), activator, (float)step.CycleSeconds);

                    var supported = new List<uint>();
                    foreach (var ally in ShipModuleActions.GetNearbyShips(activator, step.Range, MaxAuraAllies, true))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest), ally, 1.0f);

                        var allyStatus = Space.GetShipStatus(ally);
                        var shieldBefore = allyStatus.Shield;
                        Space.RestoreAllyShield(ally, allyStatus, ShipModuleActions.ApplyRepairEfficiency(activator, repair));
                        Space.ApplyRepairEnmity(activator, ally, allyStatus.Shield - shieldBefore);
                        supported.Add(ally);
                    }

                    Enmity.ModifyEnmityOnAll(activator, 100 + repair);
                    ShipModuleActions.AddSupportCombatPoint(activator, supported);
                });
        }

        private void ProbeLauncher(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ValidationAction((activator, _, _, _, _) => Space.ProbeLauncherError(activator))
                .ActivatedAction((activator, status, _, _, _) => Space.RunProbeCycle(activator, status, Effective(step)));
        }

        private void DataAnalyzer(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((activator, _, target, _, _) => Space.DataAnalyzerError(activator, target))
                .ActivatedAction((activator, _, target, _, _) => Space.StartDataAnalyzer(activator, target));
        }

        private void CloakingDevice(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ValidationAction((activator, status, _, _, _) => Space.CloakError(activator, status))
                .ActivatedAction((activator, status, _, _, _) => Space.RunCloakCycle(activator, status, Effective(step)));
        }

        /// <summary>
        /// An Ion Web (W11k): slows the ship's target every cycle. The Interceptor role webs harder and farther, the
        /// Republic Scout farther still, and the Barracuda shrugs webs off.
        /// </summary>
        private void IonWeb(ShipModuleStep step)
        {
            _builder
                .MaxDistance((_, status, _, _, _) => Space.GetIonWebRange(status, step.Range))
                .ValidTargetType(ObjectType.Creature)
                .ValidationAction((activator, _, target, targetStatus, _) => Space.IonWebError(activator, target, targetStatus))
                .ActivatedAction((activator, status, target, _, _) =>
                {
                    Space.ApplyIonWeb(activator, status, target, Effective(step));
                    TauntTarget(activator, target);
                });
        }

        /// <summary>
        /// An Interdictor (W11k): holds the ship's target every cycle, so it cannot jump, dock, cloak or run an
        /// Afterburner. The Interceptor role reaches farther.
        /// </summary>
        private void Interdictor(ShipModuleStep step)
        {
            _builder
                .MaxDistance((_, status, _, _, _) => Space.GetInterdictorRange(status, step.InterdictRange))
                .ValidTargetType(ObjectType.Creature)
                .ValidationAction((activator, _, target, targetStatus, _) => Space.HostileElectronicWarfareError(activator, target, targetStatus))
                .ActivatedAction((activator, _, target, targetStatus, _) =>
                {
                    Space.ApplyInterdiction(activator, target, targetStatus, Effective(step));
                    TauntTarget(activator, target);
                });
        }

        /// <summary>A Jammer or Target Painter (W11k): renews its debuff on the ship's target every cycle.</summary>
        private void HostileElectronicWarfare(ShipModuleStep step, System.Action<uint, uint, ShipStatus, ShipModuleStep> apply)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ValidationAction((activator, _, target, targetStatus, _) => Space.HostileElectronicWarfareError(activator, target, targetStatus))
                .ActivatedAction((activator, _, target, targetStatus, _) =>
                {
                    apply(activator, target, targetStatus, Effective(step));
                    TauntTarget(activator, target);
                });
        }

        /// <summary>The E-War target puts the ship on its enmity table, so supporting it credits the target (as the E-War Array does).</summary>
        private static void TauntTarget(uint activator, uint target)
        {
            Enmity.ModifyEnmity(activator, target, 100);
            ShipModuleActions.AddSupportCombatPoint(activator, activator);
        }

        /// <summary>An Afterburner (W11k): + speed while on; an Interdictor's hold shuts it down.</summary>
        private void Afterburner(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ValidationAction((_, status, _, _, _) => Space.AfterburnerError(status))
                .ActivatedAction((activator, status, _, _, _) => Space.RunAfterburnerCycle(activator, status));
        }

        private void EWarArray(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, _, _, _, _) =>
                {
                    // A station's array works at the effectiveness of whoever runs it (a crew member or a droid), as
                    // weapon damage and repairs do (plan section 10); the pilot's own runs at 100%.
                    var accuracy = ShipCrewSeats.ScaleByEffectiveness(
                        Effective(step).AuraAccuracy, SpaceCrew.GetFireEffectiveness(activator));
                    var seconds = step.CycleSeconds + 0.5;
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), activator, (float)step.CycleSeconds);

                    foreach (var enemy in ShipModuleActions.GetNearbyShips(activator, step.Range, MaxAuraEnemies, false))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), enemy, 2.0f);
                        ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), enemy, 2.0f);

                        Space.ApplyAccuracyPenalty(enemy, Space.GetShipStatus(enemy), accuracy, seconds);
                        Enmity.ModifyEnmity(activator, enemy, 100);
                    }

                    // The taunt puts each affected enemy on the E-War ship's enmity table, so supporting it credits them.
                    ShipModuleActions.AddSupportCombatPoint(activator, activator);
                });
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
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
                ShipModuleFamily.ShieldRepairer => ShipModuleType.ShieldRepairer,
                ShipModuleFamily.HullRepairer => ShipModuleType.HullRepairer,
                ShipModuleFamily.ShieldHardener => ShipModuleType.ShieldHardener,
                ShipModuleFamily.HypermatterInjector => ShipModuleType.HypermatterInjector,
                ShipModuleFamily.BulwarkShieldGenerator => ShipModuleType.BulwarkShieldGenerator,
                ShipModuleFamily.RepairField => ShipModuleType.RepairFieldGenerator,
                ShipModuleFamily.EWarArray => ShipModuleType.CapitalEwar,
                _ => ShipModuleType.Passive,
            };
        }

        private static SpaceWeaponShot BuildShot(ShipModuleStep step, ShipStatus status, ShipStatus targetStatus) =>
            ShipShotBuilder.BuildFor(step, status, targetStatus);

        private void Weapon(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ActivatedAction((activator, status, target, targetStatus, _) =>
                    ShipModuleActions.FireStep(
                        step, activator, status, target, targetStatus, BuildShot(step, status, targetStatus), false));
        }

        private void AmmoWeapon(ShipModuleStep step, string missingAmmoMessage)
        {
            var ammoResref = ShipModuleActions.GetAmmoResref(step.Family);
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ValidationAction((activator, status, _, targetStatus, _) =>
                {
                    var ammoError = ShipModuleActions.AmmoError(activator, ammoResref, missingAmmoMessage);
                    return string.IsNullOrWhiteSpace(ammoError)
                        ? ShipNpcRules.TargetError(step.Family, status, targetStatus?.Size ?? ShipSize.Small)
                        : ammoError;
                })
                .ActivatedAction((activator, status, target, targetStatus, _) =>
                {
                    var shot = BuildShot(step, status, targetStatus);
                    ShipModuleActions.ConsumeAmmo(activator, ammoResref);
                    ShipModuleActions.FireStep(step, activator, status, target, targetStatus, shot, false);
                });
        }

        private void MiningLaser(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((_, _, target, _, _) => ShipModuleActions.MiningLaserError(target, step.MiningTier))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                    ShipModuleActions.MineAsteroid(activator, target, (float)step.CycleSeconds, moduleBonus));
        }

        private void StripMiner(ShipModuleStep step)
        {
            _builder
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Placeable)
                .ValidationAction((_, status, target, _, _) =>
                {
                    if (status.Size != ShipSize.Large && status.Size != ShipSize.Capital)
                        return "A strip miner needs a Large or Capital hull.";

                    return ShipModuleActions.StripMinerError(target, step.MiningTier);
                })
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                    ShipModuleActions.StripAsteroid(activator, target, (float)step.CycleSeconds, moduleBonus));
        }

        private void Repairer(ShipModuleStep step, System.Action<uint, ShipStatus, uint, ShipStatus, int> repair)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(step.Range)
                .ValidTargetType(ObjectType.Creature)
                .ActivatedAction((activator, status, target, targetStatus, _) =>
                    repair(activator, status, target, targetStatus, ShipShotBuilder.ScaleSupport(step.Repair, status.Size)));
        }

        private void ShieldHardener(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, status, _, _, _) =>
                {
                    status.Harden(step.ResistAll, step.CycleSeconds, DateTime.UtcNow);
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
                    ShipModuleActions.InjectCapacitor(activator, status, activator, status, ShipShotBuilder.ScaleSupport(step.CapacitorRestore, status.Size)));
        }

        private void BulwarkShieldGenerator(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, _, _, _, _) =>
                {
                    var seconds = step.CycleSeconds + 0.5;
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), activator, (float)step.CycleSeconds);

                    var supported = new List<uint>();
                    foreach (var ally in ShipModuleActions.GetNearbyShips(activator, step.Range, MaxAuraAllies, true))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), ally, 2.0f);
                        ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), ally, 2.0f);

                        Space.ApplyShieldAura(ally, Space.GetShipStatus(ally), step.AuraShieldPercent, seconds);
                        supported.Add(ally);
                    }

                    Enmity.ModifyEnmityOnAll(activator, 100);
                    CombatPoint.AddSupportCombatPoint(activator, supported, SkillType.Piloting);
                });
        }

        private void RepairField(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, _, _, _, _) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_White), activator, (float)step.CycleSeconds);

                    var supported = new List<uint>();
                    foreach (var ally in ShipModuleActions.GetNearbyShips(activator, step.Range, MaxAuraAllies, true))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest), ally, 1.0f);

                        var allyStatus = Space.GetShipStatus(ally);
                        var shieldBefore = allyStatus.Shield;
                        Space.RestoreAllyShield(ally, allyStatus, step.Repair);
                        Space.ApplyRepairEnmity(activator, ally, allyStatus.Shield - shieldBefore);
                        supported.Add(ally);
                    }

                    Enmity.ModifyEnmityOnAll(activator, 100 + step.Repair);
                    CombatPoint.AddSupportCombatPoint(activator, supported, SkillType.Piloting);
                });
        }

        private void EWarArray(ShipModuleStep step)
        {
            _builder
                .CanTargetSelf()
                .MaxDistance(SelfRange)
                .ActivatedAction((activator, _, _, _, _) =>
                {
                    var seconds = step.CycleSeconds + 0.5;
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), activator, (float)step.CycleSeconds);

                    foreach (var enemy in ShipModuleActions.GetNearbyShips(activator, step.Range, MaxAuraEnemies, false))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), enemy, 2.0f);
                        ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), enemy, 2.0f);

                        Space.ApplyAccuracyPenalty(enemy, Space.GetShipStatus(enemy), step.AuraAccuracy, seconds);
                        Enmity.ModifyEnmity(activator, enemy, 100);
                    }

                    // The taunt puts each affected enemy on the E-War ship's enmity table, so supporting it credits them.
                    CombatPoint.AddSupportCombatPoint(activator, activator, SkillType.Piloting);
                });
        }
    }
}

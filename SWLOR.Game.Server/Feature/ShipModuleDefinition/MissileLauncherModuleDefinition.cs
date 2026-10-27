using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class MissileLauncherModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MissileLauncher("msl_launch_b", "Basic Missile Launcher", "B. Msl Launch", "Requires a missile. Deals 12 explosive DMG to your target. Bonus damage on unshielded targets.", 1, 12);
            MissileLauncher("msl_launch_1", "Missile Launcher I", "Msl Launch I", "Requires a missile. Deals 24 explosive DMG to your target. Bonus damage on unshielded targets.", 2, 24);
            MissileLauncher("msl_launch_2", "Missile Launcher II", "Msl Launch II", "Requires a missile. Deals 36 explosive DMG to your target. Bonus damage on unshielded targets.", 3, 36);
            MissileLauncher("msl_launch_3", "Missile Launcher III", "Msl Launch III", "Requires a missile. Deals 48 explosive DMG  to your target. Bonus damage on unshielded targets.", 4, 48);
            MissileLauncher("msl_launch_4", "Missile Launcher IV", "Msl Launch IV", "Requires a missile. Deals 60 explosive DMG to your target. Bonus damage on unshielded targets.", 5, 60);

            return _builder.Build();
        }

        private const string MissileItemResref = "ship_missile";

        private void MissileLauncher(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_smmsllaunch")
                .Type(ShipModuleType.Missile)
                .MaxDistance(55f)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Toggle(12f)
                .ValidationAction((activator, _, _, _, _) =>
                {
                    var item = GetItemPossessedBy(activator, MissileItemResref);
                    var stackSize = GetItemStackSize(item);
                    if(stackSize <= 0 && GetIsPC(activator) == true)
                    {
                        return "You need a missile to fire this weapon.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var moduleDamage = dmg + moduleBonus;
                    // Missiles do 25% more damage to unshielded targets. Due to shield recharge starting instantly, allow for up to 10 shield points to be considered "unshielded".
                    if (targetShipStatus.Shield <= 10)
                    {
                        moduleDamage += moduleDamage / 4;
                    }
                    var item = GetItemPossessedBy(activator, MissileItemResref);
                    var stackSize = GetItemStackSize(item);
                    if (stackSize <= 1)
                    {
                        DestroyObject(item);
                    }
                    else
                    {
                        SetItemStackSize(item, stackSize - 1);
                    }

                    var targetDistance = GetDistanceBetween(activator, target);
                    var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                    var rank = (requiredLevel - 1) * 10;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Launcher,
                        DamageType = CombatDamageType.Explosive,
                        Dmg = moduleDamage,
                        Rank = rank,
                        Attack = rank * 2 + moduleBonus * 2,
                        Accuracy = rank * 2
                    };
                    var result = Space.RollWeaponShot(activator, activatorShipStatus, target, targetShipStatus, shot);
                    var isHit = result.IsHit;

                    // Shoot some missiles out to the target.
                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Trp), activator);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_Torpedo, !isHit), target);
                    });

                    // Display an explosion at the target location in a few seconds (based on travel distance of the initial missile graphic)
                    // Then apply damage on target and those nearby.
                    DelayCommand(delay, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball, !isHit, 0.5f), target);
                        Space.CompleteWeaponShot(activator, target, shot, result);
                    });
                });
        }
    }
}

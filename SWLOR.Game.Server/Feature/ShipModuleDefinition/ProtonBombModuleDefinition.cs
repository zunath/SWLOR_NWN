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
    public class ProtonBombModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ProtonBomb("protbomblnch", "Proton Bomb Launcher", "Prot B. Launch", "Requires a proton bomb. Can only target capital ships. Deals 120 DMG to target, bypassing shields.", 120);

            return _builder.Build();
        }

        private void ProtonBomb(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_smbomblaunch")
                .Type(ShipModuleType.ProtonBomb)
                .MaxDistance(10f)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Recast(18f)
                .OnePress()
                .ValidationAction((activator, _, target, _, _) =>
                {
                    var ammoError = ShipModuleActions.AmmoError(activator, ShipModuleActions.ProtonBombResref, "You need a proton bomb to fire this weapon.");
                    if (!string.IsNullOrEmpty(ammoError))
                    {
                        return ammoError;
                    }

                    if (!Space.GetShipStatus(target).CapitalShip)
                    {
                        return "This weapon can only target capital ships.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var moduleDamage = dmg + (moduleBonus * 2);
                    // Bombs do 50% more damage to unshielded targets. Due to shield recharge starting instantly, allow for up to 10 shield points to be considered "unshielded".
                    if (targetShipStatus.Shield <= 10)
                    {
                        moduleDamage += moduleDamage / 2;
                    }
                    ShipModuleActions.ConsumeAmmo(activator, ShipModuleActions.ProtonBombResref);

                    // A capital-class bomb: rank 40, and it bypasses shields and the size penalty.
                    const int rank = 40;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Launcher,
                        DamageType = CombatDamageType.Explosive,
                        Dmg = moduleDamage,
                        Rank = rank,
                        Attack = rank * 2 + moduleBonus * 2,
                        Accuracy = rank * 2,
                        Size = ShipSize.Capital,
                        ShieldBypass = true,
                        IgnoreSizePenalty = true
                    };
                    ShipModuleActions.FireMissile(activator, activatorShipStatus, target, targetShipStatus, shot, true);
                });
        }
    }
}

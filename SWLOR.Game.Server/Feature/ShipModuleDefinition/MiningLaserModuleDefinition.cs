using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class MiningLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MiningLaser("min_laser_b", "Basic Mining Laser", "B. Min. Laser", 1, 5, 6f);
            MiningLaser("min_laser_1", "Mining Laser I", "Min. Laser I", 2, 7, 7f);
            MiningLaser("min_laser_2", "Mining Laser II", "Min. Laser II", 3, 9, 8f);
            MiningLaser("min_laser_3", "Mining Laser III", "Min. Laser III", 4, 11, 9f);
            MiningLaser("min_laser_4", "Mining Laser IV", "Min. Laser IV", 5, 13, 10f);

            return _builder.Build();
        }

        private void MiningLaser(string itemTag, string name, string shortName, int requiredLevel, int capacitor, float recast)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_084")
                .Type(ShipModuleType.MiningLaser)
                .ValidTargetType(ObjectType.Placeable)
                .Description($"Mines targets up to tier {requiredLevel}.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.MiningModules, requiredLevel)
                .Capacitor(capacitor)
                .Recast(recast)
                .ValidationAction((activator, status, target, shipStatus) =>
                {
                    // Ensure an asteroid ore type has been specified by the builder.
                    var oreResref = GetLocalString(target, "ASTEROID_ORE_RESREF");
                    if (string.IsNullOrWhiteSpace(oreResref))
                    {
                        return "Only asteroids may be targeted with this module.";
                    }

                    // Ensure required level of the module matches or exceeds tier of asteroid.
                    var tierRequired = GetLocalInt(target, "ASTEROID_TIER");
                    if (requiredLevel < tierRequired)
                    {
                        return "This mining laser is not powerful enough to harvest that asteroid.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, status, target, shipStatus) =>
                {
                    // Remaining units aren't set - pick a random number to assign.
                    var remainingUnits = GetLocalInt(target, "ASTEROID_REMAINING_UNITS");
                    if (remainingUnits <= 0)
                    {
                        remainingUnits = Random.D4(1) + 2;
                        SetLocalInt(target, "ASTEROID_REMAINING_UNITS", remainingUnits);
                    }

                    AssignCommand(activator, () =>
                    {
                        var beam = EffectBeam(VisualEffect.Vfx_Beam_Holy, activator, BodyNode.Chest);
                        ApplyEffectToObject(DurationType.Temporary, beam, target, recast + 0.1f);
                    });

                    // At the end of the process, spawn the ore on the activator and reduce remaining units.
                    DelayCommand(recast + 0.1f, () =>
                    {
                        // Safety check - if another player pulls all of the ore from the asteroid, give an error message.
                        if (!GetIsObjectValid(target))
                        {
                            SendMessageToPC(activator, "Your target has been fully mined.");
                            return;
                        }

                        remainingUnits = GetLocalInt(target, "ASTEROID_REMAINING_UNITS");

                        // Perk bonuses
                        var amountToMine = 1 + Perk.GetEffectivePerkLevel(activator, PerkType.StarshipMining);
                        if (amountToMine > remainingUnits)
                            amountToMine = remainingUnits;

                        remainingUnits -= amountToMine;

                        // Refresh remaining units (could have changed since the start)
                        var oreResref = GetLocalString(target, "ASTEROID_ORE_RESREF");

                        // Fully deplete the rock - destroy it.
                        if (remainingUnits <= 0)
                        {
                            DestroyObject(target);
                            SendMessageToPC(activator, $"{GetName(target)} has been fully mined.");
                        }
                        // Update remaining units.
                        else
                        {
                            SetLocalInt(target, "ASTEROID_REMAINING_UNITS", remainingUnits);
                        }

                        // Spawn the units.
                        for (var count = 1; count <= amountToMine; count++)
                        {
                            CreateItemOnObject(oreResref, activator);
                        }
                    });
                });
        }
    }
}

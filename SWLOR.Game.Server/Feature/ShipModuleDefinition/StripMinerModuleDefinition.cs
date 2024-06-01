using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class StripMinerModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            StripMiner("cap_minlas", "Strip Miner", "Strip Miner");

            return _builder.Build();
        }

        private const string Mined = "BEING_MINED";

        private void StripMiner(string itemTag, string name, string shortName)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_087")
                .Type(ShipModuleType.StripMiner)
                .MaxDistance(10f)
                .ValidTargetType(ObjectType.Placeable)
                .Description("Mines a target asteroid over 18 seconds, retrieving an extremely large number of ores up to the asteroid's tier. Your ship is immobile while this happens.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.MiningModules, 5)
                .Capacitor(30)
                .Recast(18)
                .CapitalClassModule()
                .ValidationAction((_, _, target, _, _) =>
                {
                    var lootTableId = GetLocalString(target, "ASTEROID_LOOT_TABLE_ID");
                    if (string.IsNullOrWhiteSpace(lootTableId))
                    {
                        return "Only asteroids may be targeted with this module.";
                    }

                    if (GetLocalBool(target, Mined))
                    {
                        return "This asteroid is already being mined.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                {
                    SetLocalBool(target, Mined, true);

                    ApplyEffectToObject(DurationType.Temporary, EffectEntangle(), activator, 18f);

                    AssignCommand(activator, () =>
                    {
                        var beam = EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest);
                        ApplyEffectToObject(DurationType.Temporary, beam, target, 18f);
                    });

                    DelayCommand(18f, () =>
                    {
                        var industrialBonus = Space.GetShipStatus(activator).Industrial;

                        var amountToMine = 1 + Perk.GetPerkLevel(activator, PerkType.StarshipMining) + (int)((industrialBonus + moduleBonus) / 3f);

                        SetPlotFlag(target, false);
                        ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                        SendMessageToPC(activator, $"{GetName(target)} has been depleted by your strip miner.");

                        Loot.SpawnLoot(target, activator, "LOOT_TABLE_");

                        var lootTableId = GetLocalString(target, "ASTEROID_LOOT_TABLE_ID");
                        var lootTable = Loot.GetLootTableByName(lootTableId);

                        for (var count = 1; count <= amountToMine; count++)
                        {
                            var item = lootTable.GetRandomItem();
                            CreateItemOnObject(item.Resref, activator);
                        }

                        if (GetIsPC(activator) && !GetIsDM(activator) && !GetIsDMPossessed(activator))
                        {
                            var playerId = GetObjectUUID(activator);
                            var dbPlayer = DB.Get<Player>(playerId);
                            var rank = dbPlayer.Skills[SkillType.Piloting].Rank;
                            var asteroidLevel = GetLocalInt(target, "ASTEROID_TIER") * 10;
                            var delta = asteroidLevel - rank;
                            var xp = Skill.GetDeltaXP(delta);

                            Skill.GiveSkillXP(activator, SkillType.Piloting, xp);
                        }
                    });
                    SetLocalBool(target, Mined, false);
                });
        }
    }
}

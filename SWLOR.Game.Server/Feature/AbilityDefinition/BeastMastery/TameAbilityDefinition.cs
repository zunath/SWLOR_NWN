using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.BeastMastery
{
    public class TameAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Tame();

            return _builder.Build();
        }

        private void Tame()
        {
            _builder.Create(FeatType.Tame, PerkType.Tame)
                .Name("Tame")
                .Level(1)
                .HasRecastDelay(RecastGroup.Tame, 60f * 10f)
                .HasActivationDelay(18f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
                    {
                        return "Only players may use this ability.";
                    }

                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (!string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId))
                    {
                        return "You already have a beast.";
                    }

                    if (GetObjectType(target) != ObjectType.Creature || GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                    {
                        return "Only NPCs may be targeted.";
                    }

                    if (GetIsObjectValid(GetMaster(target)))
                    {
                        return "That target cannot be tamed.";
                    }

                    var type = Service.BeastMastery.GetBeastType(target);
                    if (type == BeastType.Invalid)
                    {
                        return "That target cannot be tamed.";
                    }

                    var tameLevel = Perk.GetEffectivePerkLevel(activator, PerkType.Tame) * 10;
                    var npcStats = Stat.GetNPCStats(target);

                    if (tameLevel < npcStats.Level)
                    {
                        return $"You may only tame creatures between levels 0-{tameLevel}. Your target is level {npcStats.Level}.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var tameLevel = Perk.GetEffectivePerkLevel(activator, PerkType.Tame);



                });
        }
    }
}

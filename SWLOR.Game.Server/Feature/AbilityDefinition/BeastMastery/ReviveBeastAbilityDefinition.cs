using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.BeastMastery
{
    public class ReviveBeastAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ReviveBeast1();
            ReviveBeast2();
            ReviveBeast3();

            return _builder.Build();
        }

        private string Validation(uint activator)
        {
            if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
            {
                return "Only players may use this ability.";
            }

            if (GetIsObjectValid(GetAssociate(AssociateType.Henchman, activator)))
            {
                return "You already have a companion active.";
            }

            var playerId = GetObjectUUID(activator);
            var dbPlayer = DB.Get<Player>(playerId);

            if (string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId))
            {
                return "You do not have an active beast.";
            }

            var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);

            if (!dbBeast.IsDead)
            {
                return "Your beast is not unconscious.";
            }

            return string.Empty;
        }

        private void ReviveBeast1()
        {
            _builder.Create(FeatType.ReviveBeast1, PerkType.ReviveBeast)
                .Name("Revive Beast I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .HasActivationDelay(4f)
                .RequirementStamina(15)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);

                    dbBeast.IsDead = false;

                    DB.Set(dbBeast);

                    Service.BeastMastery.SpawnBeast(activator, dbBeast.Id, 0);
                });
        }

        private void ReviveBeast2()
        {
            _builder.Create(FeatType.ReviveBeast2, PerkType.ReviveBeast)
                .Name("Revive Beast II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .HasActivationDelay(4f)
                .RequirementStamina(17)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);

                    dbBeast.IsDead = false;

                    DB.Set(dbBeast);

                    var hpPercentage = 10 + GetAbilityScore(activator, AbilityType.Social);
                    Service.BeastMastery.SpawnBeast(activator, dbBeast.Id, hpPercentage);
                });
        }

        private void ReviveBeast3()
        {
            _builder.Create(FeatType.ReviveBeast3, PerkType.ReviveBeast)
                .Name("Revive Beast III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .HasActivationDelay(4f)
                .RequirementStamina(18)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);

                    dbBeast.IsDead = false;

                    DB.Set(dbBeast);

                    var hpPercentage = 30 + GetAbilityScore(activator, AbilityType.Social) * 2;
                    Service.BeastMastery.SpawnBeast(activator, dbBeast.Id, hpPercentage);
                });
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class ReviveBeastAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ReviveBeastAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IDatabaseService DB => _serviceProvider.GetRequiredService<IDatabaseService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ReviveBeast1(builder);
            ReviveBeast2(builder);
            ReviveBeast3(builder);

            return builder.Build();
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

        private void ReviveBeast1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ReviveBeast1, PerkType.ReviveBeast)
                .Name("Revive Beast I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ReviveBeast, 60f * 5)
                .UsesAnimation(AnimationType.LoopingGetMid)
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

                    BeastMastery.SpawnBeast(activator, dbBeast.Id, 0);
                    EnmityService.ModifyEnmityOnAll(activator, 500);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }

        private void ReviveBeast2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ReviveBeast2, PerkType.ReviveBeast)
                .Name("Revive Beast II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ReviveBeast, 60f * 5)
                .UsesAnimation(AnimationType.LoopingGetMid)
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
                    BeastMastery.SpawnBeast(activator, dbBeast.Id, hpPercentage);
                    EnmityService.ModifyEnmityOnAll(activator, 500);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }

        private void ReviveBeast3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ReviveBeast3, PerkType.ReviveBeast)
                .Name("Revive Beast III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ReviveBeast, 60f * 5)
                .UsesAnimation(AnimationType.LoopingGetMid)
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
                    BeastMastery.SpawnBeast(activator, dbBeast.Id, hpPercentage);
                    EnmityService.ModifyEnmityOnAll(activator, 500);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}

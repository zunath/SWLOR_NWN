using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class CallBeastAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public CallBeastAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IDatabaseService DB => _serviceProvider.GetRequiredService<IDatabaseService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CallBeast(builder);

            return builder.Build();
        }

        private void CallBeast(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CallBeast, PerkType.Tame) // Intentionally tied to Tame
                .Name("Call Beast")
                .Level(1)
                .HasRecastDelay(RecastGroupType.CallBeast, 60f * 10f)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (GetIsInCombat(activator) || EnmityService.HasEnmity(activator))
                    {
                        return "You are in combat and cannot call your beast.";
                    }

                    var maxBeastLevel = PerkService.GetPerkLevel(activator, PerkType.Tame) * 10;

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

                    if (dbBeast.IsDead)
                    {
                        return "Your beast is unconscious.";
                    }

                    if (dbBeast.Level > maxBeastLevel)
                    {
                        return $"Your Tame level is too low to call this beast. (Required: {maxBeastLevel/10})";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);
                    
                    BeastMastery.SpawnBeast(activator, dbPlayer.ActiveBeastId, 50);

                    EnmityService.ModifyEnmityOnAll(activator, 230);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}

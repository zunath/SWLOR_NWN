using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Ability.Definitions.Beastmaster
{
    public class TameAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public TameAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IDatabaseService DB => _serviceProvider.GetRequiredService<IDatabaseService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICharacterResourceService CharacterResourceService => _serviceProvider.GetRequiredService<ICharacterResourceService>();
        private IStatCalculationService StatCalculationService => _serviceProvider.GetRequiredService<IStatCalculationService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IBeastRepository BeastRepository => _serviceProvider.GetRequiredService<IBeastRepository>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Tame(builder);

            return builder.Build();
        }

        private void Tame(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Tame, PerkType.Tame)
                .Name("Tame")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Tame, 60f * 2f)
                .UsesAnimation(AnimationType.LoopingGetMid)
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

                    if (GetIsObjectValid(GetMaster(target)) || GetIsDead(target) || CharacterResourceService.GetCurrentHP(target) <= 0 || !GetIsObjectValid(target))
                    {
                        return "That target cannot be tamed.";
                    }

                    var type = BeastMastery.GetBeastType(target);
                    if (type == BeastType.Invalid)
                    {
                        return "That target cannot be tamed.";
                    }

                    var tameLevel = PerkService.GetPerkLevel(activator, PerkType.Tame) * 10;
                    var targetLevel = StatCalculationService.CalculateLevel(target);

                    if (tameLevel < targetLevel)
                    {
                        return $"You may only tame creatures between levels 0-{tameLevel}. Your target is level {targetLevel}.";
                    }

                    var maxBeasts = 1 + PerkService.GetPerkLevel(activator, PerkType.Stabling);
                    var beastCount = (int)BeastRepository.GetCountByOwnerPlayerId(playerId);
                    if (beastCount >= maxBeasts)
                    {
                        return $"You have already tamed the maximum number of beasts your perks support.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var type = BeastMastery.GetBeastType(target);
                    var skill = dbPlayer.Skills[SkillType.BeastMastery].Rank;
                    var targetLevel = StatCalculationService.CalculateLevel(target);
                    var socialMod = GetAbilityModifier(AbilityType.Social, activator);
                    var chance = 40 + (skill - targetLevel) * 3 + socialMod * 4;

                    if (chance > 95)
                        chance = 95;

                    if (Random.D100(1) > chance)
                    {
                        SendMessageToPC(activator, ColorToken.Red($"Failed to tame {GetName(target)}..."));
                        EnmityService.ModifyEnmity(activator, target, 600);
                        return;
                    }

                    var (likedFood, hatedFood) = BeastMastery.GetLikedAndHatedFood();

                    var dbBeast = new Beast
                    {
                        Name = GetName(target),
                        OwnerPlayerId = playerId,
                        Level = 1,
                        UnallocatedSP = 1,
                        IsDead = false,
                        Type = type,
                        FavoriteFood = likedFood,
                        HatedFood = hatedFood,

                        AttackPurity = Random.Next(0, 10),
                        AccuracyPurity = Random.Next(0, 10),
                        EvasionPurity = Random.Next(0, 10),
                        LearningPurity = Random.Next(0, 10),

                        DefensePurities = new Dictionary<CombatDamageType, int>
                        {
                            { CombatDamageType.Physical, Random.Next(0, 10) },
                            { CombatDamageType.Force, Random.Next(0, 10) },
                            { CombatDamageType.Fire, Random.Next(0, 10) },
                            { CombatDamageType.Ice, Random.Next(0, 10) },
                            { CombatDamageType.Poison, Random.Next(0, 10) },
                            { CombatDamageType.Electrical, Random.Next(0, 10) },
                        },

                        SavingThrowPurities = new Dictionary<SavingThrowCategoryType, int>
                        {
                            { SavingThrowCategoryType.Fortitude, Random.Next(0, 10)},
                            { SavingThrowCategoryType.Will, Random.Next(0, 10)},
                            { SavingThrowCategoryType.Reflex, Random.Next(0, 10)},
                        }
                    };

                    DB.Set(dbBeast);

                    dbPlayer.ActiveBeastId = dbBeast.Id;
                    DB.Set(dbPlayer);

                    SendMessageToPC(activator, ColorToken.Green($"Successfully tamed {GetName(target)}!"));
                    DestroyObject(target);
                });
        }
    }
}

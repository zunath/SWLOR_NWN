using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Associate.Entity;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class TameAbilityDefinition: IAbilityListDefinition
    {
        private readonly IRandomService _random;
        private readonly IDatabaseService _db;
        private readonly IPerkService _perkService;
        private readonly IStatService _statService;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IEnmityService _enmityService;

        public TameAbilityDefinition(
            IRandomService random, 
            IDatabaseService db, 
            IPerkService perkService, 
            IStatService statService, 
            IBeastMasteryService beastMastery, 
            IEnmityService enmityService)
        {
            _random = random;
            _db = db;
            _perkService = perkService;
            _statService = statService;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
        }

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
                .HasRecastDelay(RecastGroup.Tame, 60f * 2f)
                .UsesAnimation(Animation.LoopingGetMid)
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
                    var dbPlayer = _db.Get<Player>(playerId);

                    if (!string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId))
                    {
                        return "You already have a beast.";
                    }

                    if (GetObjectType(target) != ObjectType.Creature || GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                    {
                        return "Only NPCs may be targeted.";
                    }

                    if (GetIsObjectValid(GetMaster(target)) || GetIsDead(target) || GetCurrentHitPoints(target) <= 0 || !GetIsObjectValid(target))
                    {
                        return "That target cannot be tamed.";
                    }

                    var type = _beastMastery.GetBeastType(target);
                    if (type == BeastType.Invalid)
                    {
                        return "That target cannot be tamed.";
                    }

                    var tameLevel = _perkService.GetPerkLevel(activator, PerkType.Tame) * 10;
                    var npcStats = _statService.GetNPCStats(target);

                    if (tameLevel < npcStats.Level)
                    {
                        return $"You may only tame creatures between levels 0-{tameLevel}. Your target is level {npcStats.Level}.";
                    }

                    var maxBeasts = 1 + _perkService.GetPerkLevel(activator, PerkType.Stabling);
                    var dbQuery = new DBQuery<Beast>()
                        .AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
                    var beastCount = (int)_db.SearchCount(dbQuery);
                    if (beastCount >= maxBeasts)
                    {
                        return $"You have already tamed the maximum number of beasts your perks support.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var type = _beastMastery.GetBeastType(target);
                    var skill = dbPlayer.Skills[SkillType.BeastMastery].Rank;
                    var npcStats = _statService.GetNPCStats(target);
                    var socialMod = GetAbilityModifier(AbilityType.Social, activator);
                    var chance = 40 + (skill - npcStats.Level) * 3 + socialMod * 4;

                    if (chance > 95)
                        chance = 95;

                    if (_random.D100(1) > chance)
                    {
                        SendMessageToPC(activator, ColorToken.Red($"Failed to tame {GetName(target)}..."));
                        _enmityService.ModifyEnmity(activator, target, 600);
                        return;
                    }

                    var (likedFood, hatedFood) = _beastMastery.GetLikedAndHatedFood();

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

                        AttackPurity = _random.Next(0, 10),
                        AccuracyPurity = _random.Next(0, 10),
                        EvasionPurity = _random.Next(0, 10),
                        LearningPurity = _random.Next(0, 10),

                        DefensePurities = new Dictionary<CombatDamageType, int>
                        {
                            { CombatDamageType.Physical, _random.Next(0, 10) },
                            { CombatDamageType.Force, _random.Next(0, 10) },
                            { CombatDamageType.Fire, _random.Next(0, 10) },
                            { CombatDamageType.Ice, _random.Next(0, 10) },
                            { CombatDamageType.Poison, _random.Next(0, 10) },
                            { CombatDamageType.Electrical, _random.Next(0, 10) },
                        },

                        SavingThrowPurities = new Dictionary<SavingThrow, int>
                        {
                            { SavingThrow.Fortitude, _random.Next(0, 10)},
                            { SavingThrow.Will, _random.Next(0, 10)},
                            { SavingThrow.Reflex, _random.Next(0, 10)},
                        }
                    };

                    _db.Set(dbBeast);

                    dbPlayer.ActiveBeastId = dbBeast.Id;
                    _db.Set(dbPlayer);

                    SendMessageToPC(activator, ColorToken.Green($"Successfully tamed {GetName(target)}!"));
                    DestroyObject(target);
                });
        }
    }
}

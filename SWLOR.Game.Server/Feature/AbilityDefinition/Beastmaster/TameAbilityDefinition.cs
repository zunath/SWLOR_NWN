using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public class TameAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private const int BaseTameChance = 40;
        private const int SkillLevelDeltaChancePercent = 3;
        private const int SocialChancePercentPerPoint = 3;
        private const int MaximumTameChancePercent = 75;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Tame();

            return _builder.Build();
        }

        public static int CalculateTameChance(int beastMasterySkillRank, int npcLevel, int social)
        {
            var baseChance = BaseTameChance + (beastMasterySkillRank - npcLevel) * SkillLevelDeltaChancePercent;
            var socialChance = System.Math.Max(0, social) * SocialChancePercentPerPoint;

            return System.Math.Clamp(baseChance + socialChance, 0, MaximumTameChancePercent);
        }

        private void Tame()
        {
            _builder
                .Create(FeatType.Tame, PerkType.Tame)
                .Name("Tame")
                .Level(1)
                .HasRecastDelay(RecastGroup.Tame, 60f * 2f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(18f)
                .RequirementStamina(10)
                .IsCastedAbility()
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

                    if (GetIsObjectValid(GetMaster(target)) || GetIsDead(target) || GetCurrentHitPoints(target) <= 0 || !GetIsObjectValid(target))
                    {
                        return "That target cannot be tamed.";
                    }

                    var type = BeastMastery.GetBeastType(target);
                    if (type == BeastType.Invalid)
                    {
                        return "That target cannot be tamed.";
                    }

                    var tameLevel = Perk.GetPerkLevel(activator, PerkType.Tame) * 10;
                    var npcStats = Stat.GetNPCStats(target);

                    if (tameLevel < npcStats.Level)
                    {
                        return $"You may only tame creatures between levels 0-{tameLevel}. Your target is level {npcStats.Level}.";
                    }

                    var maxBeasts = 1 + Perk.GetPerkLevel(activator, PerkType.Stabling);
                    var dbQuery = new DBQuery<Beast>()
                        .AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
                    var beastCount = (int)DB.SearchCount(dbQuery);
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
                    var npcStats = Stat.GetNPCStats(target);
                    var social = GetAbilityScore(activator, AbilityType.Social);
                    var chance = CalculateTameChance(skill, npcStats.Level, social);

                    if (Random.D100(1) > chance)
                    {
                        SendMessageToPC(activator, ColorToken.Red($"Failed to tame {GetName(target)}..."));
                        Enmity.ModifyEnmity(activator, target, 600);
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

                        DefensePurities = BeastResistanceCalculator.CreateRandomDefensePurities(),
                        ResistancePurities = BeastResistanceCalculator.CreateRandomResistancePurities()
                    };

                    DB.Set(dbBeast);

                    dbPlayer.ActiveBeastId = dbBeast.Id;
                    DB.Set(dbPlayer);
                    Achievement.GiveAchievement(activator, AchievementType.ANewBond);

                    SendMessageToPC(activator, ColorToken.Green($"Successfully tamed {GetName(target)}!"));
                    DestroyObject(target);
                });
        }
    }
}

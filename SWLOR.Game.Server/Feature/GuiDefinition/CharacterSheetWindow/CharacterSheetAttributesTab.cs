using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// The attributes tab: resources, ability scores, the upgrade buttons and the attribute upgrade itself, and the skill point
    /// and AP or level lines.
    /// </summary>
    public class CharacterSheetAttributesTab
    {
        private IBeastMasteryService BeastMastery { get; }
        private ICreatureStatService CreatureStat { get; }
        private IDBService DB { get; }
        private IPlayerNameService PlayerName { get; }
        private ISkillService Skill { get; }
        private ISkillRankService SkillRank { get; }
        private IStatService Stat { get; }

        public CharacterSheetAttributesTab(IBeastMasteryService beastMastery, ICreatureStatService creatureStat, IDBService db, IPlayerNameService playerName, ISkillService skill, ISkillRankService skillRank, IStatService stat)
        {
            BeastMastery = beastMastery;
            CreatureStat = creatureStat;
            DB = db;
            PlayerName = playerName;
            Skill = skill;
            SkillRank = skillRank;
            Stat = stat;
        }

        private const int MaxPurchasedAttributeScore = 26;
        private const int RacialAttributeBonus = 1;
        private const int MaxRacialAttributeScore = MaxPurchasedAttributeScore + RacialAttributeBonus;

        private static int GetPurchasedAttributeScore(Player dbPlayer, AbilityType ability)
        {
            var baseScore = dbPlayer.BaseStats.TryGetValue(ability, out var baseValue)
                ? baseValue
                : 0;
            var upgradedScore = dbPlayer.UpgradedStats.TryGetValue(ability, out var upgradedValue)
                ? upgradedValue
                : 0;

            return baseScore + upgradedScore;
        }

        public void UpgradeAttribute(ICharacterSheetHost host, AbilityType ability, string abilityName)
        {
            if (!host.IsPlayerMode || !GetIsObjectValid(host.Target))
                return;

            var playerId = GetObjectUUID(host.Target);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var isRacial = dbPlayer.RacialStat == AbilityType.Invalid;
            var promptMessage = isRacial
                ? "WARNING: You are about to spend your one-time racial stat bonus. Once spent, this action can only be undone with a stat rebuild. Are you SURE you want to upgrade this stat?"
                : $"Upgrading your {abilityName} attribute will consume 1 AP. Are you sure you want to upgrade it?";

            host.ShowModal(promptMessage, () =>
            {
                if (!GetIsObjectValid(host.Target) || GetObjectUUID(host.Target) != playerId)
                    return;

                if (GetResRef(GetArea(host.Target)) == "char_migration")
                {
                    FloatingTextStringOnCreature($"Stats cannot be upgraded in this area.", host.Target, false);
                    return;
                }

                dbPlayer = DB.Get<Player>(playerId);
                if (dbPlayer == null)
                    return;

                isRacial = dbPlayer.RacialStat == AbilityType.Invalid;
                var rawScore = CreaturePlugin.GetRawAbilityScore(host.Target, ability);
                var purchasedScore = GetPurchasedAttributeScore(dbPlayer, ability);

                if (isRacial)
                {
                    if (rawScore >= MaxRacialAttributeScore || purchasedScore > MaxPurchasedAttributeScore)
                    {
                        FloatingTextStringOnCreature($"You cannot upgrade this attribute beyond {MaxRacialAttributeScore} with a racial bonus.", host.Target, false);
                        return;
                    }

                    dbPlayer.RacialStat = ability;
                }
                else
                {
                    if (purchasedScore >= MaxPurchasedAttributeScore)
                    {
                        FloatingTextStringOnCreature($"You cannot upgrade this attribute beyond {MaxPurchasedAttributeScore} with AP.", host.Target, false);
                        return;
                    }

                    if (rawScore >= MaxRacialAttributeScore)
                    {
                        FloatingTextStringOnCreature($"You cannot upgrade this attribute beyond {MaxRacialAttributeScore}.", host.Target, false);
                        return;
                    }

                    if (dbPlayer.UnallocatedAP <= 0)
                    {
                        FloatingTextStringOnCreature("You do not have enough AP to purchase this upgrade.", host.Target, false);
                        return;
                    }

                    dbPlayer.UnallocatedAP--;
                    dbPlayer.UpgradedStats[ability]++;
                }

                DB.Set(dbPlayer);
                CreatureStat.ApplyPlayerStat(dbPlayer, host.Target, ability);

                FloatingTextStringOnCreature($"Your {abilityName} attribute has increased!", host.Target, false);
                host.LoadData();
            });
        }

        private static bool IsAttributeUpgradeAvailable(ICharacterSheetHost host, Player dbPlayer, AbilityType ability, bool isRacialBonusAvailable)
        {
            var rawScore = CreaturePlugin.GetRawAbilityScore(host.Target, ability);
            var purchasedScore = GetPurchasedAttributeScore(dbPlayer, ability);

            if (isRacialBonusAvailable)
            {
                return rawScore < MaxRacialAttributeScore &&
                       purchasedScore <= MaxPurchasedAttributeScore;
            }

            return dbPlayer.UnallocatedAP > 0 &&
                   purchasedScore < MaxPurchasedAttributeScore &&
                   rawScore < MaxRacialAttributeScore;
        }

        public void RefreshStats(ICharacterSheetHost host)
        {
            host.RefreshHitPoints();

            if (GetClassByPosition(1, host.Target) == ClassType.Standard)
            {
                host.FP = $"0 / 0";
            }
            else
            {
                var currentFP = Stat.GetCurrentFP(host.Target);
                var maxFP = Stat.GetMaxFP(host.Target);
                if (currentFP < 0)
                    currentFP = 0;
                if (maxFP < 0)
                    maxFP = 0;

                host.FP = $"{currentFP} / {maxFP}";
            }

            var currentSTM = Stat.GetCurrentStamina(host.Target);
            var maxSTM = Stat.GetMaxStamina(host.Target);
            if (currentSTM < 0)
                currentSTM = 0;
            if (maxSTM < 0)
                maxSTM = 0;

            host.STM = $"{currentSTM} / {maxSTM}";
            host.Name = PlayerName.GetDisplayName(host.Player, host.Target);
            host.Might = GetAbilityScore(host.Target, AbilityType.Might);
            host.Perception = GetAbilityScore(host.Target, AbilityType.Perception);
            host.Vitality = GetAbilityScore(host.Target, AbilityType.Vitality);
            host.Willpower = GetAbilityScore(host.Target, AbilityType.Willpower);
            host.Agility = GetAbilityScore(host.Target, AbilityType.Agility);
            host.Social = GetAbilityScore(host.Target, AbilityType.Social);

            if (host.IsPlayerMode)
            {
                var playerId = GetObjectUUID(host.Target);
                var dbPlayer = DB.Get<Player>(playerId);

                var isRacialBonusAvailable = dbPlayer.RacialStat == AbilityType.Invalid;
                host.IsMightUpgradeAvailable = IsAttributeUpgradeAvailable(host, dbPlayer, AbilityType.Might, isRacialBonusAvailable);
                host.IsPerceptionUpgradeAvailable = IsAttributeUpgradeAvailable(host, dbPlayer, AbilityType.Perception, isRacialBonusAvailable);
                host.IsVitalityUpgradeAvailable = IsAttributeUpgradeAvailable(host, dbPlayer, AbilityType.Vitality, isRacialBonusAvailable);
                host.IsWillpowerUpgradeAvailable = IsAttributeUpgradeAvailable(host, dbPlayer, AbilityType.Willpower, isRacialBonusAvailable);
                host.IsAgilityUpgradeAvailable = IsAttributeUpgradeAvailable(host, dbPlayer, AbilityType.Agility, isRacialBonusAvailable);
                host.IsSocialUpgradeAvailable = IsAttributeUpgradeAvailable(host, dbPlayer, AbilityType.Social, isRacialBonusAvailable);
            }

            host.RefreshCharacterStatsList();
        }

        public void RefreshAttributes(ICharacterSheetHost host)
        {
            if (GetIsPC(host.Target))
            {
                var playerId = GetObjectUUID(host.Target);
                var dbPlayer = DB.Get<Player>(playerId);

                host.SkillRanks = $"{SkillRank.GetTotalContributingSkillRanks(dbPlayer)} / {SkillService.SkillCap}";
                host.SP = $"{Skill.GetTotalSkillPoints(dbPlayer)} / {SkillService.TotalSkillPointCap} ({dbPlayer.UnallocatedSP})";
                host.APOrLevel = $"{dbPlayer.TotalAPAcquired} / {Skill.APCap} ({dbPlayer.UnallocatedAP})";
            }
            else if (BeastMastery.IsPlayerBeast(host.Target))
            {
                var beastId = BeastMastery.GetBeastId(host.Target);
                var dbBeast = DB.Get<Beast>(beastId);

                host.SP = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel} ({dbBeast.UnallocatedSP})";
                host.APOrLevel = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel}";
                host.APOrLevelTooltip = $"XP: {dbBeast.XP} / {BeastMastery.GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent)}";
            }
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Space achievements (plan section 14): first kill of each faction tier, a capital kill, each ace, a named module
    /// fitted, the first jump to each kind of scanned site, Wild Space and trade earnings. Which achievement a milestone
    /// earns is <see cref="SpaceAchievementRules"/>. W11o added a crewed kill; the Fighter, Corvette and Basilisk interiors
    /// grant their exploration achievements through the area's EXPLORE_ACHIEVEMENT_ID local, as every starship interior does.
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// When a preset NPC ship is destroyed: the killer earns its faction tier's first-kill achievement, and the
        /// killing party in the area earns the capital or ace achievement.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void GrantSpaceKillAchievements()
        {
            var creature = OBJECT_SELF;
            if (GetIsPC(creature) || !_npcPresets.TryGetValue(GetTag(creature), out var preset))
                return;

            var killer = GetLastKiller();
            if (!GetIsPC(killer))
                killer = Enmity.GetHighestEnmityTarget(creature);
            if (!GetIsObjectValid(killer) || !GetIsPC(killer) || GetIsDM(killer))
                return;

            var tierKill = SpaceAchievementRules.TierKill(ShipSalvageRules.WreckTier(GetTag(creature), preset.Level));
            if (tierKill != AchievementType.Invalid)
                Achievement.GiveAchievement(killer, tierKill);

            // A kill made with a crewmate at a station (W11o): everyone aboard the killing ship earns it.
            var aboard = GetShipCrewPlayers(GetFiringShip(killer));
            if (SpaceAchievementRules.IsCrewedKill(aboard.Count))
            {
                foreach (var player in aboard)
                    Achievement.GiveAchievement(player, AchievementType.SpaceCrewedKill);
            }

            var ace = ShipAceRules.Get(GetTag(creature));
            if (preset.Size != ShipSize.Capital && ace == null)
                return;

            foreach (var member in GetPartyInArea(killer))
            {
                if (preset.Size == ShipSize.Capital)
                    Achievement.GiveAchievement(member, AchievementType.SpaceCapitalKill);
                if (ace != null)
                    Achievement.GiveAchievement(member, ace.Achievement);
            }
        }

        /// <summary>The player and the members of their party in the same area.</summary>
        private static List<uint> GetPartyInArea(uint player)
        {
            var area = GetArea(player);
            var members = new List<uint> { player };
            for (var member = GetFirstFactionMember(player); GetIsObjectValid(member); member = GetNextFactionMember(player))
            {
                if (member != player && GetArea(member) == area && !GetIsDM(member))
                    members.Add(member);
            }

            return members;
        }

        /// <summary>The first jump to each kind of scanned site.</summary>
        private static void GrantSiteAchievement(uint player, SignatureType type)
        {
            var achievement = SpaceAchievementRules.Site(type);
            if (achievement != AchievementType.Invalid)
                Achievement.GiveAchievement(player, achievement);
        }

        /// <summary>Fitting a named rank-50 module to a ship.</summary>
        public static void GrantModuleFittedAchievement(uint player, ShipModuleDetail module)
        {
            if (SpaceAchievementRules.IsNamedModule(module?.Step))
                Achievement.GiveAchievement(player, AchievementType.SpaceNamedModule);
        }

        /// <summary>Entering a Wild Space area (an area with the WILD_SPACE local; plan section 23).</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void GrantWildSpaceAchievement()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || !IsWildSpace(OBJECT_SELF))
                return;

            Achievement.GiveAchievement(player, AchievementType.SpaceWildSpace);
        }

        /// <summary>Adds credits earned from trade to the account's running total and grants the milestones it reached.</summary>
        public static void RecordTradeEarnings(uint player, int credits)
        {
            if (credits <= 0 || !GetIsPC(player) || GetIsDM(player))
                return;

            var cdKey = GetPCPublicCDKey(player);
            var account = DB.Get<Account>(cdKey) ?? new Account(cdKey);
            account.AchievementProgress.TradeCreditsEarned += (ulong)credits;
            DB.Set(account);

            foreach (var achievement in SpaceAchievementRules.TradeEarnings(account.AchievementProgress.TradeCreditsEarned))
                Achievement.GiveAchievement(player, achievement);
        }
    }
}

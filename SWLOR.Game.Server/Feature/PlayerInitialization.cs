using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using Player = SWLOR.Game.Server.Entity.Player;
using Skill = SWLOR.Game.Server.Core.NWScript.Enum.Skill;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Race = SWLOR.Game.Server.Service.Race;

namespace SWLOR.Game.Server.Feature
{
    public class PlayerInitialization
    {
        /// <summary>
        /// Handles 
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void InitializePlayer()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            // Already been initialized. Don't do it again.
            if (dbPlayer.Version >= 1) return;

            ClearInventory(player);
            AutoLevelPlayer(player);
            InitializeSkills(player);
            InitializeSavingThrows(player);
            RemoveNWNSpells(player);
            ClearFeats(player);
            GrantBasicFeats(player);
            InitializeHotBar(player);
            AdjustStats(player, dbPlayer);
            AdjustAlignment(player);
            InitializeLanguages(player, dbPlayer);
            AssignRacialAppearance(player, dbPlayer);
            GiveStartingItems(player);
            AssignCharacterType(player, dbPlayer);
            RegisterDefaultRespawnPoint(dbPlayer);

            DB.Set(playerId, dbPlayer);
        }

        private static void AutoLevelPlayer(uint player)
        {
            // Capture original stats before we level up the player.
            var str = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Might);
            var con = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Vitality);
            var dex = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Perception);
            var @int = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Unused);
            var wis = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Willpower);
            var cha = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Diplomacy);

            GiveXPToCreature(player, 10000);

            for(var level = 1; level <= 5; level++)
            {
                var @class = GetClassByPosition(1, player);
                LevelUpHenchman(player, @class);
            }

            // Set stats back to how they were on entry.
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Might, str);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, con);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Perception, dex);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Unused, @int);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Willpower, wis);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Diplomacy, cha);
        }

        /// <summary>
        /// Wipes a player's equipped items and inventory.
        /// </summary>
        /// <param name="player">The player to wipe an inventory for.</param>
        private static void ClearInventory(uint player)
        {
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                var item = GetItemInSlot((InventorySlot)slot, player);
                if (!GetIsObjectValid(item)) continue;

                DestroyObject(item);
            }

            var inventory = GetFirstItemInInventory(player);
            while (GetIsObjectValid(inventory))
            {
                DestroyObject(inventory);
                inventory = GetNextItemInInventory(player);
            }

            TakeGoldFromCreature(GetGold(player), player, true);
        }

        /// <summary>
        /// Initializes all player NWN skills to zero.
        /// </summary>
        /// <param name="player">The player to modify</param>
        private static void InitializeSkills(uint player)
        {
            for (var iCurSkill = 1; iCurSkill <= 27; iCurSkill++)
            {
                var skill = (Skill) (iCurSkill - 1);
                CreaturePlugin.SetSkillRank(player, skill, 0);
            }
        }

        /// <summary>
        /// Initializes all player saving throws to zero.
        /// </summary>
        /// <param name="player">The player to modify</param>
        private static void InitializeSavingThrows(uint player)
        {
            SetFortitudeSavingThrow(player, 0);
            SetReflexSavingThrow(player, 0);
            SetWillSavingThrow(player, 0);
        }

        /// <summary>
        /// Removes all NWN spells from a player.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        private static void RemoveNWNSpells(uint player)
        {
            var @class = GetClassByPosition(1, player);
            for (var index = 0; index <= 255; index++)
            {
                CreaturePlugin.RemoveKnownSpell(player, @class, 0, index);
            }
        }

        private static void ClearFeats(uint player)
        {
            var numberOfFeats = CreaturePlugin.GetFeatCount(player);
            for (var currentFeat = numberOfFeats; currentFeat >= 0; currentFeat--)
            {
                CreaturePlugin.RemoveFeat(player, CreaturePlugin.GetFeatByIndex(player, currentFeat - 1));
            }
        }

        private static void GrantBasicFeats(uint player)
        {
            CreaturePlugin.AddFeatByLevel(player, FeatType.ArmorProficiencyLight, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ArmorProficiencyMedium, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ArmorProficiencyHeavy, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ShieldProficiency, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.WeaponProficiencyExotic, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.WeaponProficiencyMartial, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.WeaponProficiencySimple, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.UncannyDodge1, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.OpenRestMenu, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ChatCommandTargeter, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.StructureTool, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.Rest, 1);
        }


        private static void InitializeHotBar(uint player)
        {
            var openRestMenu = PlayerQuickBarSlot.UseFeat(FeatType.OpenRestMenu);
            var chatCommandTargeter = PlayerQuickBarSlot.UseFeat(FeatType.ChatCommandTargeter);
            var restAbility = PlayerQuickBarSlot.UseFeat(FeatType.Rest);
            var structureTool = PlayerQuickBarSlot.UseFeat(FeatType.StructureTool);

            Core.NWNX.Player.SetQuickBarSlot(player, 0, openRestMenu);
            Core.NWNX.Player.SetQuickBarSlot(player, 1, chatCommandTargeter);
            Core.NWNX.Player.SetQuickBarSlot(player, 2, restAbility);
            Core.NWNX.Player.SetQuickBarSlot(player, 3, structureTool);
        }

        /// <summary>
        /// Modifies the player's unallocated SP, version, max HP, and other assorted stats.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The player entity.</param>
        private static void AdjustStats(uint player, Player dbPlayer)
        {
            dbPlayer.UnallocatedSP = 10;
            dbPlayer.Version = 1;
            dbPlayer.Name = GetName(player);
            Stat.AdjustPlayerMaxHP(dbPlayer, player, 40);
            Stat.AdjustPlayerMaxSTM(dbPlayer, 10);
            CreaturePlugin.SetBaseAttackBonus(player, 1);
            dbPlayer.HP = GetCurrentHitPoints(player);
            dbPlayer.FP = Stat.GetMaxFP(player, dbPlayer);
            dbPlayer.Stamina = Stat.GetMaxStamina(player, dbPlayer);

            dbPlayer.BaseStats[AbilityType.Might] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Might);
            dbPlayer.BaseStats[AbilityType.Perception] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Perception);
            dbPlayer.BaseStats[AbilityType.Vitality] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Vitality);
            dbPlayer.BaseStats[AbilityType.Willpower] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Willpower);
            dbPlayer.BaseStats[AbilityType.Unused] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Unused);
            dbPlayer.BaseStats[AbilityType.Diplomacy] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Diplomacy);
        }

        /// <summary>
        /// Modifies the player's alignment to Neutral/Neutral since we don't use alignment at all here.
        /// </summary>
        /// <param name="player">The player to object.</param>
        private static void AdjustAlignment(uint player)
        {
            CreaturePlugin.SetAlignmentLawChaos(player, 50);
            CreaturePlugin.SetAlignmentGoodEvil(player, 50);
        }

        /// <summary>
        /// Initializes all of the languages for a player based on their racial type.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="dbPlayer">The player entity.</param>
        private static void InitializeLanguages(uint player, Player dbPlayer)
        {
            var race = GetRacialType(player);
            var languages = new List<SkillType>(new[] { SkillType.Basic });

            switch (race)
            {
                case RacialType.Bothan:
                    languages.Add(SkillType.Bothese);
                    break;
                case RacialType.Chiss:
                    languages.Add(SkillType.Cheunh);
                    break;
                case RacialType.Zabrak:
                    languages.Add(SkillType.Zabraki);
                    break;
                case RacialType.Wookiee:
                    languages.Add(SkillType.Shyriiwook);
                    break;
                case RacialType.Twilek:
                    languages.Add(SkillType.Twileki);
                    break;
                case RacialType.Cathar:
                    languages.Add(SkillType.Catharese);
                    break;
                case RacialType.Trandoshan:
                    languages.Add(SkillType.Dosh);
                    break;
                case RacialType.Cyborg:
                    languages.Add(SkillType.Droidspeak);
                    break;
                case RacialType.Mirialan:
                    languages.Add(SkillType.Mirialan);
                    break;
                case RacialType.MonCalamari:
                    languages.Add(SkillType.MonCalamarian);
                    break;
                case RacialType.Ugnaught:
                    languages.Add(SkillType.Ugnaught);
                    break;
            }

            // Fair warning: We're short-circuiting the skill system here.
            // Languages don't level up like normal skills (no stat increases, SP, etc.)
            // So it's safe to simply set the player's rank in the skill to max.
            foreach (var language in languages)
            {
                var skill = Service.Skill.GetSkillDetails(language);
                if (!dbPlayer.Skills.ContainsKey(language))
                    dbPlayer.Skills[language] = new PlayerSkill();

                var level = skill.MaxRank;
                dbPlayer.Skills[language].Rank = level;

                dbPlayer.Skills[language].XP = Service.Skill.GetRequiredXP(level) - 1;
            }
        }

        /// <summary>
        /// Assigns and stores the player's original racial appearance.
        /// This value is primarily used in the space system for switching between space and character modes.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The database entity</param>
        private static void AssignRacialAppearance(uint player, Player dbPlayer)
        {
            DelayCommand(0.1f, () =>
            {
                Race.SetDefaultRaceAppearance(player);
            });
            dbPlayer.OriginalAppearanceType = GetAppearanceType(player);
        }

        /// <summary>
        /// Gives the starting items to the player.
        /// </summary>
        /// <param name="player">The player to receive the starting items.</param>
        private static void GiveStartingItems(uint player)
        {
            var item = CreateItemOnObject("survival_knife", player);
            SetName(item, GetName(player) + "'s Survival Knife");
            SetItemCursedFlag(item, true);

            item = CreateItemOnObject("tk_omnidye", player);
            SetItemCursedFlag(item, true);

            GiveGoldToCreature(player, 100);
        }

        /// <summary>
        /// Sets the character type (either Standard or Force Sensitive) based on their character class.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="dbPlayer">The player entity</param>
        private static void AssignCharacterType(uint player, Player dbPlayer)
        {
            var @class = GetClassByPosition(1, player);

            if (@class == ClassType.Standard)
                dbPlayer.CharacterType = CharacterType.Standard;
            else if (@class == ClassType.ForceSensitive)
                dbPlayer.CharacterType = CharacterType.ForceSensitive;
        }

        /// <summary>
        /// Sets the player's default respawn location to that of the waypoint with tag 'DTH_DEFAULT_RESPAWN_POINT'.
        /// If no waypoint by that tag can be found, an error will be logged.
        /// </summary>
        /// <param name="dbPlayer">The player's database entity.</param>
        private static void RegisterDefaultRespawnPoint(Player dbPlayer)
        {
            const string DefaultRespawnWaypointTag = "DTH_DEFAULT_RESPAWN_POINT";
            var waypoint = GetWaypointByTag(DefaultRespawnWaypointTag);

            if (!GetIsObjectValid(waypoint))
            {
                Log.Write(LogGroup.Error, $"Default respawn waypoint could not be located. Did you place a waypoint with the tag 'DTH_DEFAULT_RESPAWN_POINT'?");
                return;
            }

            var location = GetLocation(waypoint);
            var position = GetPositionFromLocation(location);
            var orientation = GetFacingFromLocation(location);
            var area = GetAreaFromLocation(location);
            var areaResref = GetResRef(area);

            dbPlayer.RespawnAreaResref = areaResref;
            dbPlayer.RespawnLocationX = position.X;
            dbPlayer.RespawnLocationY = position.Y;
            dbPlayer.RespawnLocationZ = position.Z;
            dbPlayer.RespawnLocationOrientation = orientation;
        }

    }
}

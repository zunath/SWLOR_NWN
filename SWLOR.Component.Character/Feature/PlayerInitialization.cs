using SWLOR.Component.Character.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Feature
{
    public class PlayerInitialization
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IStatService _statService;
        private readonly ISkillService _skillService;
        private readonly IMigrationService _migrationService;
        private readonly IRaceService _raceService;

        public PlayerInitialization(
            ILogger logger, 
            IDatabaseService db, 
            IStatService statService, 
            ISkillService skillService, 
            IMigrationService migrationService, 
            IRaceService raceService)
        {
            _logger = logger;
            _db = db;
            _statService = statService;
            _skillService = skillService;
            _migrationService = migrationService;
            _raceService = raceService;
        }
        /// <summary>
        /// Handles 
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void InitializePlayer()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

            // Already been initialized. Don't do it again.
            if (dbPlayer.Version >= 1 || dbPlayer.Version == -1) // Note: -1 signifies legacy characters. The Migration service handles upgrading legacy characters.
            {
                ExecuteScript(ScriptName.OnCharacterInitAfter, OBJECT_SELF);
                return;
            }

            ClearInventory(player);
            AutoLevelPlayer(player);
            InitializeSkills(player);
            RemoveNWNSpells(player);
            ClearFeats(player);
            GrantBasicFeats(player);
            InitializeHotBar(player);
            AdjustStats(player, dbPlayer);
            AdjustAlignment(player);
            InitializeSavingThrows(player);
            InitializeLanguages(player, dbPlayer);
            AssignRacialAppearance(player, dbPlayer);
            GiveStartingItems(player);
            AssignCharacterType(player, dbPlayer);
            RegisterDefaultRespawnPoint(dbPlayer);
            ApplyMovementRate(player);

            _db.Set(dbPlayer);

            ExecuteScript(ScriptName.OnCharacterInitAfter, OBJECT_SELF);
        }

        private void AutoLevelPlayer(uint player)
        {
            // Capture original stats before we level up the player.
            var str = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Might);
            var con = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Vitality);
            var dex = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Perception);
            var @int = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Agility);
            var wis = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Willpower);
            var cha = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Social);

            GiveXPToCreature(player, 800000);
            var @class = GetClassByPosition(1, player);

            for (var level = 1; level <= 40; level++)
            {
                LevelUpHenchman(player, @class);
            }

            // Set stats back to how they were on entry.
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Might, str);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, con);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Perception, dex);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Agility, @int);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Willpower, wis);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Social, cha);
        }

        /// <summary>
        /// Wipes a player's equipped items and inventory.
        /// </summary>
        /// <param name="player">The player to wipe an inventory for.</param>
        private void ClearInventory(uint player)
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
        public void InitializeSkills(uint player)
        {
            for (var iCurSkill = 1; iCurSkill <= 27; iCurSkill++)
            {
                var skill = (NWNSkillType) (iCurSkill - 1);
                CreaturePlugin.SetSkillRank(player, skill, 0);
            }
        }

        /// <summary>
        /// Initializes all player saving throws to zero.
        /// </summary>
        /// <param name="player">The player to modify</param>
        public void InitializeSavingThrows(uint player)
        {
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Fortitude, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Will, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Reflex, 0);
        }

        /// <summary>
        /// Removes all NWN spells from a player.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        private void RemoveNWNSpells(uint player)
        {
            var @class = GetClassByPosition(1, player);
            for (var index = 0; index <= 255; index++)
            {
                CreaturePlugin.RemoveKnownSpell(player, @class, 0, index);
            }
        }

        public void ClearFeats(uint player)
        {
            var numberOfFeats = CreaturePlugin.GetFeatCount(player);
            for (var currentFeat = numberOfFeats; currentFeat >= 0; currentFeat--)
            {
                CreaturePlugin.RemoveFeat(player, CreaturePlugin.GetFeatByIndex(player, currentFeat - 1));
            }
        }

        public void GrantBasicFeats(uint player)
        {
            CreaturePlugin.AddFeatByLevel(player, FeatType.ArmorProficiencyLight, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ArmorProficiencyMedium, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ArmorProficiencyHeavy, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.ShieldProficiency, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.WeaponProficiencyExotic, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.WeaponProficiencyMartial, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.WeaponProficiencySimple, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.UncannyDodge1, 1);
            CreaturePlugin.AddFeatByLevel(player, FeatType.PropertyMenu, 1);
        }

        public void InitializeHotBar(uint player)
        {
            var structureTool = PlayerQuickBarSlot.UseFeat(FeatType.PropertyMenu);
            
            PlayerPlugin.SetQuickBarSlot(player, 0, structureTool);
        }

        /// <summary>
        /// Modifies the player's unallocated SP, version, max HP, and other assorted stats.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The player entity.</param>
        private void AdjustStats(uint player, Player dbPlayer)
        {
            dbPlayer.UnallocatedSP = 10;
            dbPlayer.Version = _migrationService.GetLatestPlayerVersion();
            dbPlayer.Name = GetName(player);
            dbPlayer.BAB = 1;
            _statService.AdjustPlayerMaxHP(dbPlayer, player, _statService.BaseHP);
            _statService.AdjustPlayerMaxFP(dbPlayer, _statService.BaseFP, player);
            _statService.AdjustPlayerMaxSTM(dbPlayer, _statService.BaseSTM, player);
            CreaturePlugin.SetBaseAttackBonus(player, 1);
            dbPlayer.HP = GetCurrentHitPoints(player);
            dbPlayer.FP = _statService.GetMaxFP(player, dbPlayer);
            dbPlayer.Stamina = _statService.GetMaxStamina(player, dbPlayer);

            dbPlayer.BaseStats[AbilityType.Might] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Might);
            dbPlayer.BaseStats[AbilityType.Perception] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Perception);
            dbPlayer.BaseStats[AbilityType.Vitality] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Vitality);
            dbPlayer.BaseStats[AbilityType.Willpower] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Willpower);
            dbPlayer.BaseStats[AbilityType.Agility] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Agility);
            dbPlayer.BaseStats[AbilityType.Social] = CreaturePlugin.GetRawAbilityScore(player, AbilityType.Social);

            dbPlayer.RebuildComplete = true;
        }

        /// <summary>
        /// Modifies the player's alignment to Neutral/Neutral since we don't use alignment at all here.
        /// </summary>
        /// <param name="player">The player to object.</param>
        public void AdjustAlignment(uint player)
        {
            CreaturePlugin.SetAlignmentLawChaos(player, 50);
            CreaturePlugin.SetAlignmentGoodEvil(player, 50);
        }

        /// <summary>
        /// Initializes all of the languages for a player based on their racial type.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="dbPlayer">The player entity.</param>
        private void InitializeLanguages(uint player, Player dbPlayer)
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
                case RacialType.Togruta:
                    languages.Add(SkillType.Togruti);
                    break;
                case RacialType.Rodian:
                    languages.Add(SkillType.Rodese);
                    break;
                case RacialType.KelDor:
                    languages.Add(SkillType.KelDor);
                    break;
                case RacialType.Droid:
                    languages.Add(SkillType.Droidspeak);
                    break;
                case RacialType.Nautolan:
                    languages.Add(SkillType.Nautila);
                    break;
                case RacialType.Ewok:
                    languages.Add(SkillType.Ewokese);
                    break;
            }

            // Fair warning: We're short-circuiting the skill system here.
            // Languages don't level up like normal skills (no stat increases, SP, etc.)
            // So it's safe to simply set the player's rank in the skill to max.
            foreach (var language in languages)
            {
                var skill = _skillService.GetSkillDetails(language);
                if (!dbPlayer.Skills.ContainsKey(language))
                    dbPlayer.Skills[language] = new PlayerSkill();

                var level = skill.MaxRank;
                dbPlayer.Skills[language].Rank = level;

                dbPlayer.Skills[language].XP = _skillService.GetRequiredXP(level) - 1;
            }
        }

        /// <summary>
        /// Assigns and stores the player's original racial appearance.
        /// This value is primarily used in the space system for switching between space and character modes.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The database entity</param>
        private void AssignRacialAppearance(uint player, Player dbPlayer)
        {
            DelayCommand(0.1f, () =>
            {
                _raceService.SetDefaultRaceAppearance(player);
            });
            dbPlayer.OriginalAppearanceType = GetAppearanceType(player);
        }

        /// <summary>
        /// Gives the starting items to the player.
        /// </summary>
        /// <param name="player">The player to receive the starting items.</param>
        private void GiveStartingItems(uint player)
        {
            var race = GetRacialType(player);
            var item = CreateItemOnObject("survival_knife", player);
            SetName(item, GetName(player) + "'s Survival Knife");
            SetItemCursedFlag(item, true);

            item = CreateItemOnObject("fresh_bread", player);
            SetItemCursedFlag(item, true);

            var clothes = race == RacialType.Droid ? "dlarproto" : "travelers_clothes";
            item = CreateItemOnObject(clothes, player);
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionEquipItem(item, InventorySlot.Chest);
            });

            GiveGoldToCreature(player, 200);
        }

        /// <summary>
        /// Sets the character type (either Standard or Force Sensitive) based on their character class.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="dbPlayer">The player entity</param>
        private void AssignCharacterType(uint player, Player dbPlayer)
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
        private void RegisterDefaultRespawnPoint(Player dbPlayer)
        {
            const string DefaultRespawnWaypointTag = "DTH_DEFAULT_RESPAWN_POINT";
            var waypoint = GetWaypointByTag(DefaultRespawnWaypointTag);

            if (!GetIsObjectValid(waypoint))
            {
                _logger.Write<ErrorLogGroup>($"Default respawn waypoint could not be located. Did you place a waypoint with the tag 'DTH_DEFAULT_RESPAWN_POINT'?");
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

        private void ApplyMovementRate(uint player)
        {
            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
        }

    }
}

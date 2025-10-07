using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
{
    public class _1_LegacyPlayerMigration: LegacyMigrationBase, IPlayerMigration
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICreaturePluginService _creaturePlugin;
        private readonly IPlayerPluginService _playerPlugin;
        
        // Lazy-loaded services to break circular dependencies
        private IPlayerInitializationService PlayerInitialization => _serviceProvider.GetRequiredService<IPlayerInitializationService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICharacterResourceService CharacterResourceService => _serviceProvider.GetRequiredService<ICharacterResourceService>();
        private IRacialAppearanceService RacialAppearanceService => _serviceProvider.GetRequiredService<IRacialAppearanceService>();
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();

        public _1_LegacyPlayerMigration(
            IDatabaseService db, 
            IServiceProvider serviceProvider,
            ICreaturePluginService creaturePlugin,
            IPlayerPluginService playerPlugin)
            : base(serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _creaturePlugin = creaturePlugin;
            _playerPlugin = playerPlugin;
        }

        public int Version => 1;
        public void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            AutoLevelUp(player);
            ResetNWNSkills(player);
            ResetFeats(player);
            ResetHotBar(player);
            ResetStats(player, dbPlayer);
            ResetAlignment(player);
            ResetSavingThrows(player);
            StoreRacialAppearance(player, dbPlayer);

            MigrateItems(player);
            MigrateCyborgsToHuman(player);
            AdjustCatharParts(player);

            _db.Set(dbPlayer);
        }

        private void AutoLevelUp(uint player)
        {
            // Most players are Force characters so we default to that class. This can be changed via the migration UI.
            _creaturePlugin.SetClassByPosition(player, 0, ClassType.ForceSensitive);

            GiveXPToCreature(player, 800000);
            var @class = GetClassByPosition(1, player);

            for (var level = 1; level <= 40; level++)
            {
                LevelUpHenchman(player, @class);
            }

            _creaturePlugin.SetRawAbilityScore(player, AbilityType.Might, 10);
            _creaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
            _creaturePlugin.SetRawAbilityScore(player, AbilityType.Perception, 10);
            _creaturePlugin.SetRawAbilityScore(player, AbilityType.Agility, 10);
            _creaturePlugin.SetRawAbilityScore(player, AbilityType.Willpower, 10);
            _creaturePlugin.SetRawAbilityScore(player, AbilityType.Social, 10);
        }

        private void ResetFeats(uint player)
        {
            PlayerInitialization.ClearFeats(player);
            PlayerInitialization.GrantBasicFeats(player);
        }

        private void ResetNWNSkills(uint player)
        {
            PlayerInitialization.InitializeSkills(player);
        }

        private void ResetSavingThrows(uint player)
        {
            PlayerInitialization.InitializeSavingThrows(player);
        }

        private void ResetStats(uint player, Player dbPlayer)
        {
            dbPlayer.BAB = 1;
            StatService.AdjustPlayerMaxHP(dbPlayer, player, 70);
            StatService.AdjustPlayerMaxFP(dbPlayer, 10, player);
            StatService.AdjustPlayerMaxSTM(dbPlayer, 10, player);
            _creaturePlugin.SetBaseAttackBonus(player, 1);
            dbPlayer.HP = CharacterResourceService.GetCurrentHP(player);
            dbPlayer.FP = StatService.GetMaxFP(player, dbPlayer);
            dbPlayer.Stamina = StatService.GetMaxStamina(player, dbPlayer);

            dbPlayer.BaseStats[AbilityType.Might] = _creaturePlugin.GetRawAbilityScore(player, AbilityType.Might);
            dbPlayer.BaseStats[AbilityType.Perception] = _creaturePlugin.GetRawAbilityScore(player, AbilityType.Perception);
            dbPlayer.BaseStats[AbilityType.Vitality] = _creaturePlugin.GetRawAbilityScore(player, AbilityType.Vitality);
            dbPlayer.BaseStats[AbilityType.Willpower] = _creaturePlugin.GetRawAbilityScore(player, AbilityType.Willpower);
            dbPlayer.BaseStats[AbilityType.Agility] = _creaturePlugin.GetRawAbilityScore(player, AbilityType.Agility);
            dbPlayer.BaseStats[AbilityType.Social] = _creaturePlugin.GetRawAbilityScore(player, AbilityType.Social);
        }

        private void ResetHotBar(uint player)
        {
            const int MaxSlots = 35;
            for (var slot = 0; slot <= MaxSlots; slot++)
            {
                _playerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
            }

            PlayerInitialization.InitializeHotBar(player);
        }

        private void ResetAlignment(uint player)
        {
            PlayerInitialization.AdjustAlignment(player);
        }

        private void StoreRacialAppearance(uint player, Player dbPlayer)
        {
            dbPlayer.OriginalAppearanceType = GetAppearanceType(player);
        }

        private void RemoveItems(uint item)
        {
            string[] resrefsToRemove =
            {
                "tk_omnidye",
                "fist",
                "player_guide",
                "xp_tome_1",
                "xp_tome_2",
                "xp_tome_3",
                "xp_tome_4",
                "refund_tome",
                "slug_shake"
            };

            var resref = GetResRef(item);
            if (resrefsToRemove.Contains(resref))
            {
                DestroyObject(item);
            }
        }

        private void MigrateItems(uint player)
        {
            // Inventory Items
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                WipeItemProperties(item);
                ItemService.MarkLegacyItem(item);
                WipeDescription(item);
                WipeVariables(item);
                CleanItemName(item);
                RemoveItems(item);
            }

            // Equipped Items
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var slot = (InventorySlotType)index;
                var item = GetItemInSlot(slot, player);

                // Skip invalid items (empty item slots)
                if (!GetIsObjectValid(item))
                    continue;

                // Skip creature items.
                if (slot == InventorySlotType.CreatureLeft ||
                    slot == InventorySlotType.CreatureRight ||
                    slot == InventorySlotType.CreatureBite ||
                    slot == InventorySlotType.CreatureArmor)
                {
                    continue;
                }

                WipeItemProperties(item);
                ItemService.MarkLegacyItem(item);
                WipeDescription(item);
                WipeVariables(item);
                RemoveItems(item);

                AssignCommand(player, () => ActionUnequipItem(item));
            }
        }

        private void MigrateCyborgsToHuman(uint player)
        {
            if (GetRacialType(player) == RacialType.Cyborg)
            {
                _creaturePlugin.SetRacialType(player, RacialType.Human);
            }
        }

        private void AdjustCatharParts(uint player)
        {
            if (GetRacialType(player) == RacialType.Cathar)
            {
                DelayCommand(10f, () =>
                {
                    var gender = GetGender(player);

                    SetCreatureBodyPart(CreaturePartType.Head, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.Head, gender), player);
                    SetCreatureBodyPart(CreaturePartType.Torso, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.Torso, gender), player);
                    SetCreatureBodyPart(CreaturePartType.Pelvis, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.Pelvis, gender), player);
                    SetCreatureBodyPart(CreaturePartType.RightBicep, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.RightBicep, gender), player);
                    SetCreatureBodyPart(CreaturePartType.RightForearm, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.RightForearm, gender), player);
                    SetCreatureBodyPart(CreaturePartType.RightHand, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.RightHand, gender), player);
                    SetCreatureBodyPart(CreaturePartType.RightThigh, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.RightThigh, gender), player);
                    SetCreatureBodyPart(CreaturePartType.RightShin, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.RightShin, gender), player);
                    SetCreatureBodyPart(CreaturePartType.RightFoot, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.RightFoot, gender), player);
                    SetCreatureBodyPart(CreaturePartType.LeftBicep, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.LeftBicep, gender), player);
                    SetCreatureBodyPart(CreaturePartType.LeftForearm, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.LeftForearm, gender), player);
                    SetCreatureBodyPart(CreaturePartType.LeftHand, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.LeftHand, gender), player);
                    SetCreatureBodyPart(CreaturePartType.LeftThigh, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.LeftThigh, gender), player);
                    SetCreatureBodyPart(CreaturePartType.LeftShin, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.LeftShin, gender), player);
                    SetCreatureBodyPart(CreaturePartType.LeftFoot, RacialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.LeftFoot, gender), player);
                });
            }
        }
    }
}

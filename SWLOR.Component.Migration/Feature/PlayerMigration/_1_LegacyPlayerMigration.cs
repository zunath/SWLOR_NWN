using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _1_LegacyPlayerMigration: LegacyMigrationBase, IPlayerMigration
    {
        private readonly IDatabaseService _db;
        private readonly IPlayerInitializationService _playerInitialization;
        private readonly IStatService _statService;
        private readonly IRacialAppearanceService _racialAppearanceService;
        private readonly IItemService _itemService;

        public _1_LegacyPlayerMigration(
            IDatabaseService db, 
            IStatService statService, 
            IItemService itemService,
            IRacialAppearanceService racialAppearanceService,
            IPlayerInitializationService playerInitialization)
        {
            _db = db;
            _statService = statService;
            _itemService = itemService;
            _racialAppearanceService = racialAppearanceService;
            _playerInitialization = playerInitialization;
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
            CreaturePlugin.SetClassByPosition(player, 0, ClassType.ForceSensitive);

            GiveXPToCreature(player, 800000);
            var @class = GetClassByPosition(1, player);

            for (var level = 1; level <= 40; level++)
            {
                LevelUpHenchman(player, @class);
            }

            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Might, 10);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Perception, 10);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Agility, 10);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Willpower, 10);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Social, 10);
        }

        private void ResetFeats(uint player)
        {
            _playerInitialization.ClearFeats(player);
            _playerInitialization.GrantBasicFeats(player);
        }

        private void ResetNWNSkills(uint player)
        {
            _playerInitialization.InitializeSkills(player);
        }

        private void ResetSavingThrows(uint player)
        {
            _playerInitialization.InitializeSavingThrows(player);
        }

        private void ResetStats(uint player, Player dbPlayer)
        {
            dbPlayer.BAB = 1;
            _statService.AdjustPlayerMaxHP(dbPlayer, player, 70);
            _statService.AdjustPlayerMaxFP(dbPlayer, 10, player);
            _statService.AdjustPlayerMaxSTM(dbPlayer, 10, player);
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
        }

        private void ResetHotBar(uint player)
        {
            const int MaxSlots = 35;
            for (var slot = 0; slot <= MaxSlots; slot++)
            {
                PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
            }

            _playerInitialization.InitializeHotBar(player);
        }

        private void ResetAlignment(uint player)
        {
            _playerInitialization.AdjustAlignment(player);
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
                _itemService.MarkLegacyItem(item);
                WipeDescription(item);
                WipeVariables(item);
                CleanItemName(item);
                RemoveItems(item);
            }

            // Equipped Items
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var slot = (InventorySlot)index;
                var item = GetItemInSlot(slot, player);

                // Skip invalid items (empty item slots)
                if (!GetIsObjectValid(item))
                    continue;

                // Skip creature items.
                if (slot == InventorySlot.CreatureLeft ||
                    slot == InventorySlot.CreatureRight ||
                    slot == InventorySlot.CreatureBite ||
                    slot == InventorySlot.CreatureArmor)
                {
                    continue;
                }

                WipeItemProperties(item);
                _itemService.MarkLegacyItem(item);
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
                CreaturePlugin.SetRacialType(player, RacialType.Human);
            }
        }

        private void AdjustCatharParts(uint player)
        {
            if (GetRacialType(player) == RacialType.Cathar)
            {
                DelayCommand(10f, () =>
                {
                    var gender = GetGender(player);

                    SetCreatureBodyPart(CreaturePart.Head, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.Head, gender), player);
                    SetCreatureBodyPart(CreaturePart.Torso, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.Torso, gender), player);
                    SetCreatureBodyPart(CreaturePart.Pelvis, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.Pelvis, gender), player);
                    SetCreatureBodyPart(CreaturePart.RightBicep, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.RightBicep, gender), player);
                    SetCreatureBodyPart(CreaturePart.RightForearm, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.RightForearm, gender), player);
                    SetCreatureBodyPart(CreaturePart.RightHand, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.RightHand, gender), player);
                    SetCreatureBodyPart(CreaturePart.RightThigh, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.RightThigh, gender), player);
                    SetCreatureBodyPart(CreaturePart.RightShin, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.RightShin, gender), player);
                    SetCreatureBodyPart(CreaturePart.RightFoot, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.RightFoot, gender), player);
                    SetCreatureBodyPart(CreaturePart.LeftBicep, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.LeftBicep, gender), player);
                    SetCreatureBodyPart(CreaturePart.LeftForearm, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.LeftForearm, gender), player);
                    SetCreatureBodyPart(CreaturePart.LeftHand, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.LeftHand, gender), player);
                    SetCreatureBodyPart(CreaturePart.LeftThigh, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.LeftThigh, gender), player);
                    SetCreatureBodyPart(CreaturePart.LeftShin, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.LeftShin, gender), player);
                    SetCreatureBodyPart(CreaturePart.LeftFoot, _racialAppearanceService.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.LeftFoot, gender), player);
                });
            }
        }
    }
}

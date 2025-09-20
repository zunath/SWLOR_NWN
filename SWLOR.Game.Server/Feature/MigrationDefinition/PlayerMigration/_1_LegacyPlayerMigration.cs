using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _1_LegacyPlayerMigration: LegacyMigrationBase, IPlayerMigration
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static readonly CatharRacialAppearanceDefinition _catharAppearance = new();

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
            Stat.AdjustPlayerMaxHP(dbPlayer, player, 70);
            Stat.AdjustPlayerMaxFP(dbPlayer, 10, player);
            Stat.AdjustPlayerMaxSTM(dbPlayer, 10, player);
            CreaturePlugin.SetBaseAttackBonus(player, 1);
            dbPlayer.HP = GetCurrentHitPoints(player);
            dbPlayer.FP = Stat.GetMaxFP(player, dbPlayer);
            dbPlayer.Stamina = Stat.GetMaxStamina(player, dbPlayer);

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
                Item.MarkLegacyItem(item);
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
                Item.MarkLegacyItem(item);
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
                    if (GetGender(player) == Gender.Female)
                    {

                        SetCreatureBodyPart(CreaturePart.Head, _catharAppearance.FemaleHeads.First(), player);
                    }
                    else
                    {
                        SetCreatureBodyPart(CreaturePart.Head, _catharAppearance.MaleHeads.First(), player);
                    }

                    SetCreatureBodyPart(CreaturePart.Torso, _catharAppearance.Torsos.First(), player);
                    SetCreatureBodyPart(CreaturePart.Pelvis, _catharAppearance.Pelvis.First(), player);
                    SetCreatureBodyPart(CreaturePart.RightBicep, _catharAppearance.RightBicep.First(), player);
                    SetCreatureBodyPart(CreaturePart.RightForearm, _catharAppearance.RightForearm.First(), player);
                    SetCreatureBodyPart(CreaturePart.RightHand, _catharAppearance.RightHand.First(), player);
                    SetCreatureBodyPart(CreaturePart.RightThigh, _catharAppearance.RightThigh.First(), player);
                    SetCreatureBodyPart(CreaturePart.RightShin, _catharAppearance.RightShin.First(), player);
                    SetCreatureBodyPart(CreaturePart.RightFoot, _catharAppearance.RightFoot.First(), player);
                    SetCreatureBodyPart(CreaturePart.LeftBicep, _catharAppearance.LeftBicep.First(), player);
                    SetCreatureBodyPart(CreaturePart.LeftForearm, _catharAppearance.LeftForearm.First(), player);
                    SetCreatureBodyPart(CreaturePart.LeftHand, _catharAppearance.LeftHand.First(), player);
                    SetCreatureBodyPart(CreaturePart.LeftThigh, _catharAppearance.LeftThigh.First(), player);
                    SetCreatureBodyPart(CreaturePart.LeftShin, _catharAppearance.LeftShin.First(), player);
                    SetCreatureBodyPart(CreaturePart.LeftFoot, _catharAppearance.LeftFoot.First(), player);
                });
            }
        }
    }
}

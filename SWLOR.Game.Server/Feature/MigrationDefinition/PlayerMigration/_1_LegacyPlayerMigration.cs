using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _1_LegacyPlayerMigration: IPlayerMigration
    {
        private static readonly CatharRacialAppearanceDefinition _catharAppearance = new();

        public int Version => 1;
        public void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            AutoLevelUp(player);
            ResetNWNSkills(player);
            ResetSavingThrows(player);
            ResetFeats(player);
            ResetHotBar(player);
            ResetStats(player, dbPlayer);
            ResetAlignment(player);
            StoreRacialAppearance(player, dbPlayer);

            MigrateItems(player);
            MigrateCyborgsToHuman(player);
            AdjustCatharParts(player);

            DB.Set(dbPlayer);
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

        private void MigrateItems(uint player)
        {
            void WipeItemProperties(uint item)
            {
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    RemoveItemProperty(item, ip);
                }
            }

            void WipeDescription(uint item)
            {
                SetDescription(item, string.Empty);
                SetDescription(item, string.Empty, false);
            }

            void WipeVariables(uint item)
            {
                var variableCount = ObjectPlugin.GetLocalVariableCount(item);
                for (var variableIndex = 0; variableIndex < variableCount - 1; variableIndex++)
                {
                    var stCurVar = ObjectPlugin.GetLocalVariable(item, variableIndex);

                    switch (stCurVar.Type)
                    {
                        case LocalVariableType.Int:
                            DeleteLocalInt(item, stCurVar.Key);
                            break;
                        case LocalVariableType.Float:
                            DeleteLocalFloat(item, stCurVar.Key);
                            break;
                        case LocalVariableType.String:
                            DeleteLocalString(item, stCurVar.Key);
                            break;
                        case LocalVariableType.Object:
                            DeleteLocalObject(item, stCurVar.Key);
                            break;
                        case LocalVariableType.Location:
                            DeleteLocalLocation(item, stCurVar.Key);
                            break;
                    }
                }
            }

            // Inventory Items
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                WipeItemProperties(item);
                Item.MarkLegacyItem(item);
                WipeDescription(item);
                WipeVariables(item);
            }

            // Equipped Items
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index);
                if (!GetIsObjectValid(item))
                    continue;

                WipeItemProperties(item);
                Item.MarkLegacyItem(item);
                WipeDescription(item);
                WipeVariables(item);

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
                if (GetGender(player) == Gender.Female)
                {

                    SetCreatureBodyPart(CreaturePart.Head, _catharAppearance.FemaleHeads.First());
                }
                else
                {
                    SetCreatureBodyPart(CreaturePart.Head, _catharAppearance.MaleHeads.First());
                }

                SetCreatureBodyPart(CreaturePart.Torso, _catharAppearance.Torsos.First());
                SetCreatureBodyPart(CreaturePart.Pelvis, _catharAppearance.Pelvis.First());
                SetCreatureBodyPart(CreaturePart.RightBicep, _catharAppearance.RightBicep.First());
                SetCreatureBodyPart(CreaturePart.RightForearm, _catharAppearance.RightForearm.First());
                SetCreatureBodyPart(CreaturePart.RightHand, _catharAppearance.RightHand.First());
                SetCreatureBodyPart(CreaturePart.RightThigh, _catharAppearance.RightThigh.First());
                SetCreatureBodyPart(CreaturePart.RightShin, _catharAppearance.RightShin.First());
                SetCreatureBodyPart(CreaturePart.RightFoot, _catharAppearance.RightFoot.First());
                SetCreatureBodyPart(CreaturePart.LeftBicep, _catharAppearance.LeftBicep.First());
                SetCreatureBodyPart(CreaturePart.LeftForearm, _catharAppearance.LeftForearm.First());
                SetCreatureBodyPart(CreaturePart.LeftHand, _catharAppearance.LeftHand.First());
                SetCreatureBodyPart(CreaturePart.LeftThigh, _catharAppearance.LeftThigh.First());
                SetCreatureBodyPart(CreaturePart.LeftShin, _catharAppearance.LeftShin.First());
                SetCreatureBodyPart(CreaturePart.LeftFoot, _catharAppearance.LeftFoot.First());
            }
        }
    }
}

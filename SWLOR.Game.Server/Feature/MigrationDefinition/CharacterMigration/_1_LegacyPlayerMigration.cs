using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Brings a character from the legacy server onto the current rules: 40 native Force Sensitive levels, reset
/// skills, feats, quickbar, stats, alignment and saving throws, legacy-marked items with their bonuses removed
/// and their equipment taken off, and Cathar body parts and Cyborg race replaced.
/// </summary>
internal sealed class _1_LegacyPlayerMigration : ICharacterMigration
{
    private const string LegacyItemTag = "LEGACY_ITEM";

    private static readonly HashSet<string> RemovedResrefs = new()
    {
        "tk_omnidye", "fist", "player_guide", "xp_tome_1", "xp_tome_2", "xp_tome_3", "xp_tome_4", "refund_tome", "slug_shake",
    };

    private static readonly InventorySlot[] CreatureSlots =
    {
        InventorySlot.CreatureLeft, InventorySlot.CreatureRight, InventorySlot.CreatureBite, InventorySlot.CreatureArmor,
    };

    private static readonly AbilityType[] Abilities =
    {
        AbilityType.Might, AbilityType.Vitality, AbilityType.Perception, AbilityType.Agility, AbilityType.Willpower, AbilityType.Social,
    };

    public int Version => 1;

    public void Migrate(SavedCharacter character)
    {
        // The retired Cyborg race is not eligible for current player classes.
        if (character.ReadInteger("Race") == (int)RacialType.Cyborg)
            character.WriteInteger("Race", (int)RacialType.Human, 0);

        // Most players are Force characters, so the legacy class becomes Force Sensitive.
        character.SetClassByPosition(0, ClassType.ForceSensitive);
        CharacterRecalculation.EnsureNativeLevels(character);
        foreach (var ability in Abilities)
            character.SetRawAbilityScore(ability, 10);

        CharacterRecalculation.InitializeSkills(character);
        CharacterRecalculation.ResetFeatsToBaseline(character);
        character.SetQuickBar(new Dictionary<int, FeatType> { [0] = FeatType.PropertyMenu });
        ResetStats(character);
        character.WriteInteger("LawfulChaotic", 50, 0);
        character.WriteInteger("GoodEvil", 50, 0);
        character.SetBaseSavingThrows(0);
        if (character.AppliesRecordEffects)
            character.Record.OriginalAppearanceType = (AppearanceType)character.ReadInteger("Appearance_Type");
        MigrateItems(character);
        AdjustCatharParts(character);
    }

    private static void ResetStats(SavedCharacter character)
    {
        var record = character.Record;
        // A record that already holds this change still has its maximum HP written to the file's levels.
        character.AdjustPlayerMaxHP(character.AppliesRecordEffects ? Stat.BaseHP - record.MaxHP : 0);
        character.WriteInteger("BaseAttackBonus", 1, 0);
        if (!character.AppliesRecordEffects)
            return;
        record.BAB = 1;
        record.MaxFP = Stat.BaseFP;
        record.MaxStamina = Stat.BaseSTM;
        record.HP = character.GetCurrentHitPoints();
        record.FP = SavedEquipmentStats.GetMaxFP(character);
        record.Stamina = SavedEquipmentStats.GetMaxStamina(character);
        foreach (var ability in Abilities)
            record.BaseStats[ability] = character.GetRawAbilityScore(ability);
    }

    /// <summary>
    /// Removes retired items and turns the rest into legacy items: no properties, descriptions or locals, and
    /// carried items lose custom names. Equipped items are taken off, into the inventory or overflow storage when
    /// it is full.
    /// </summary>
    private static void MigrateItems(SavedCharacter character)
    {
        foreach (var item in character.CarriedRecursive.ToArray())
        {
            if (Remove(character, item))
                continue;
            Wipe(item);
            item.SetName(string.Empty);
        }
        for (var index = 0; index < NumberOfInventorySlots; index++)
        {
            var slot = (InventorySlot)index;
            var item = character.GetEquipped(slot);
            if (item == null || CreatureSlots.Contains(slot) || Remove(character, item))
                continue;
            Wipe(item);
            character.Unequip(item);
        }
    }

    private static bool Remove(SavedCharacter character, SavedItem item)
    {
        if (!RemovedResrefs.Contains(item.ResRef))
            return false;
        character.Destroy(item);
        if (item.Container != null)
            StoredObjectData.NumberListElements(item.List);
        return true;
    }

    private static void Wipe(SavedItem item)
    {
        item.RemoveProperties(_ => true);
        item.Tag = LegacyItemTag;
        // SetDescription with an empty description clears the identified and unidentified overrides.
        item.WriteString("DescriptionOverr", string.Empty);
        item.WriteString("DescIdentifiedOv", string.Empty);
        item.DeleteAllLocals();
    }

    private static void AdjustCatharParts(SavedCharacter character)
    {
        if (character.ReadInteger("Race") != (int)RacialType.Cathar)
            return;
        var cathar = new CatharRacialAppearanceDefinition();
        var female = character.ReadInteger("Gender") == (int)Gender.Female;
        character.SetCreatureBodyPart("Appearance_Head", female ? cathar.FemaleHeads.First() : cathar.MaleHeads.First());
        character.SetCreatureBodyPart("BodyPart_Torso", cathar.Torsos.First());
        character.SetCreatureBodyPart("BodyPart_Pelvis", cathar.Pelvis.First());
        character.SetCreatureBodyPart("BodyPart_RBicep", cathar.RightBicep.First());
        character.SetCreatureBodyPart("BodyPart_RFArm", cathar.RightForearm.First());
        character.SetCreatureBodyPart("BodyPart_RHand", cathar.RightHand.First());
        character.SetCreatureBodyPart("BodyPart_RThigh", cathar.RightThigh.First());
        character.SetCreatureBodyPart("BodyPart_RShin", cathar.RightShin.First());
        // The engine saves the right foot under this label, unlike every other body part.
        character.SetCreatureBodyPart("ArmorPart_RFoot", cathar.RightFoot.First());
        character.SetCreatureBodyPart("BodyPart_LBicep", cathar.LeftBicep.First());
        character.SetCreatureBodyPart("BodyPart_LFArm", cathar.LeftForearm.First());
        character.SetCreatureBodyPart("BodyPart_LHand", cathar.LeftHand.First());
        character.SetCreatureBodyPart("BodyPart_LThigh", cathar.LeftThigh.First());
        character.SetCreatureBodyPart("BodyPart_LShin", cathar.LeftShin.First());
        character.SetCreatureBodyPart("BodyPart_LFoot", cathar.LeftFoot.First());
    }
}

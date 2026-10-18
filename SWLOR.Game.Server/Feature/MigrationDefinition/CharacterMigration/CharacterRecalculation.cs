using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Saved-data counterparts of the character-wide steps older player migrations share.</summary>
internal static class CharacterRecalculation
{
    private static readonly InventorySlot[] CreatureSlots =
    {
        InventorySlot.CreatureArmor, InventorySlot.CreatureBite, InventorySlot.CreatureLeft, InventorySlot.CreatureRight,
    };

    /// <summary>
    /// Saved-data <c>PlayerMigrationBase.RecalculateStats</c>: unequips everything but creature items, then
    /// resets the record's equipment-derived stats to their base values and restores full HP, FP and STM.
    /// </summary>
    public static void RecalculateStats(SavedCharacter character)
    {
        for (var index = 0; index < NumberOfInventorySlots; index++)
        {
            var slot = (InventorySlot)index;
            var item = character.GetEquipped(slot);
            if (item == null || CreatureSlots.Contains(slot))
                continue;
            character.ApplyItemStats(item, false);
            character.Unequip(item);
        }

        if (character.AppliesRecordEffects)
        {
            var record = character.Record;
            record.MaxHP = Stat.BaseHP;
            record.HPRegen = 0;
            record.MaxFP = Stat.BaseFP;
            record.FP = SavedEquipmentStats.GetMaxFP(character);
            record.FPRegen = 0;
            record.MaxStamina = Stat.BaseSTM;
            record.Stamina = SavedEquipmentStats.GetMaxStamina(character);
            record.STMRegen = 0;
            foreach (var (type, _) in Skill.GetActiveCraftingSkills())
            {
                record.Craftsmanship[type] = 0;
                record.Control[type] = 0;
                record.CPBonus[type] = 0;
            }
            record.Attack = 0;
            record.Accuracy = 0;
            record.ForceAttack = 0;
            record.CombatReadiness = 0;
            foreach (var defense in Combat.GetDefenseDamageTypes())
                record.Defenses[defense] = 0;
            foreach (var resistance in Resistance.GetAllResistanceTypes())
                record.Resistances[resistance] = 0;
            record.Evasion = 0;
        }
        character.AdjustPlayerMaxHP(0);
        character.RestoreFullHitPoints();
        if (character.AppliesRecordEffects)
            character.Record.HP = character.GetCurrentHitPoints();
    }

    /// <summary>Saved-data <c>PlayerMigrationBase.RefundPerk</c>: returns a perk's purchase price as skill points.</summary>
    public static void RefundPerk(SavedCharacter character, PerkType perkType)
    {
        var record = character.Record;
        if (!character.AppliesRecordEffects || !record.Perks.TryGetValue(perkType, out var perkLevel))
            return;
        var perkDetail = Perk.GetPerkDetails(perkType);
        var refundAmount = perkDetail.PerkLevels.Where(x => x.Key <= perkLevel).Sum(x => x.Value.Price);
        record.UnallocatedSP += refundAmount;
        record.Perks.Remove(perkType);
        Log.Write(LogGroup.Migration, $"{record.Name} ({record.Id}) refunded {refundAmount} SP for perk '{perkType}'.");
        character.Notices.Add($"Perk '{perkDetail.Name}' was automatically refunded. You reclaimed {refundAmount} SP.");
    }

    /// <summary>
    /// Saved-data <see cref="Perk.GetPlayerEffectivePerkLevel(uint, PerkType)"/>: the highest owned perk level whose
    /// requirements the record (and its active beast) meet.
    /// </summary>
    public static int GetEffectivePerkLevel(SavedCharacter character, PerkType perkType)
    {
        var record = character.Record;
        var playerPerkLevel = record.Perks.TryGetValue(perkType, out var owned) ? owned : 0;
        if (playerPerkLevel <= 0)
            return 0;
        var perk = Perk.GetPerkDetails(perkType);
        foreach (var (level, detail) in perk.PerkLevels.Where(x => x.Key <= playerPerkLevel).OrderByDescending(x => x.Key))
        {
            if (detail.Requirements.Count <= 0 ||
                detail.Requirements.All(requirement => string.IsNullOrWhiteSpace(requirement.CheckRequirements(record, character.ActiveBeast))))
                return level;
        }
        return 0;
    }

    /// <summary>Saved-data <see cref="Perk.RestorePlayerFeats"/>: native feats granted by every owned player perk.</summary>
    public static void RestorePlayerFeats(SavedCharacter character)
    {
        foreach (var (perkType, level) in character.Record.Perks.ToList())
        {
            if (level <= 0 || !Perk.TryGetPerkDetails(perkType, out var detail) ||
                !detail.IsActive || detail.GroupType != PerkGroupType.Player)
                continue;

            var granted = Perk.GetGrantedFeatsForPerkLevel(perkType, level);
            var grantedSet = granted.ToHashSet();
            foreach (var feat in Perk.GetAllActiveAbilityFeats(perkType))
            {
                if (grantedSet.Contains(feat))
                    continue;
                RemoveQuickCastFeat(character, feat);
            }
            foreach (var feat in granted)
            {
                if (!KnowsQuickCastFeat(character, feat))
                    character.AddFeatByLevel(feat, 1);
            }
            ApplyQuickCastPreferences(character);
        }
    }

    private static bool KnowsQuickCastFeat(SavedCharacter character, FeatType feat)
    {
        var original = QuickCast.GetOriginalFeat(feat);
        return character.HasFeat(original) ||
               Service.AbilityService.QuickCastFeatMap.TryGetAlias(original, out var alias) && character.HasFeat(alias);
    }

    private static void RemoveQuickCastFeat(SavedCharacter character, FeatType feat)
    {
        var original = QuickCast.GetOriginalFeat(feat);
        character.RemoveFeat(original);
        if (Service.AbilityService.QuickCastFeatMap.TryGetAlias(original, out var alias))
            character.RemoveFeat(alias);
    }

    /// <summary>
    /// Saved-data <see cref="QuickCast.ApplyPreferences"/>: each ability feat the character knows is held in the form
    /// its automatic-targeting setting asks for, on the feat list and the quickbar.
    /// </summary>
    public static void ApplyQuickCastPreferences(SavedCharacter character)
    {
        var settings = character.Record.Settings;
        if (settings == null)
            return;
        foreach (var (alias, original) in Service.AbilityService.QuickCastFeatMap.Originals)
        {
            var hasOriginal = character.HasFeat(original);
            var hasAlias = character.HasFeat(alias);
            if (!hasOriginal && !hasAlias)
                continue;
            var automatic = QuickCast.UsesAutomaticTarget(original, settings);
            var desired = automatic ? alias : original;
            var other = automatic ? original : alias;
            if (!(automatic ? hasAlias : hasOriginal))
                character.AddFeat(desired);
            if (automatic ? hasOriginal : hasAlias)
                character.RemoveFeat(other);
        }

        var slots = character.QuickBar;
        for (var index = 0; index < 36 && index < slots.Count; index++)
        {
            if (slots[index].Type != SWLOR.Game.Server.Core.NWNX.Enum.QuickBarSlotType.Feat)
                continue;
            var current = (FeatType)slots[index].IntParameter;
            var original = QuickCast.GetOriginalFeat(current);
            if (!Service.AbilityService.QuickCastFeatMap.TryGetAlias(original, out var alias))
                continue;
            var desired = KnowsQuickCastFeat(character, original) && QuickCast.UsesAutomaticTarget(original, settings) ? alias : original;
            if (current != desired)
                character.SetQuickBarFeat(index, desired);
        }
    }

    /// <summary>Saved-data <c>PlayerInitialization.ResetFeatsToBaseline</c>: only the basic feats every character has.</summary>
    public static void ResetFeatsToBaseline(SavedCharacter character)
    {
        character.ClearFeats();
        character.AddFeatByLevel(FeatType.ShieldProficiency, 1);
        character.AddFeatByLevel(FeatType.WeaponProficiencyExotic, 1);
        character.AddFeatByLevel(FeatType.WeaponProficiencyMartial, 1);
        character.AddFeatByLevel(FeatType.WeaponProficiencySimple, 1);
        character.AddFeatByLevel(FeatType.UncannyDodge1, 1);
        character.AddFeatByLevel(FeatType.PropertyMenu, 1);
    }

    /// <summary>Saved-data <c>PlayerInitialization.InitializeSkills</c>: native skills 0 through 26 at rank 0.</summary>
    public static void InitializeSkills(SavedCharacter character)
    {
        for (var skill = 0; skill < 27; skill++)
            character.SetSkillRank(skill, 0);
    }

    /// <summary>
    /// Saved-data <c>PlayerInitialization.EnsureNativeLevels</c>: levels the character to 40 in its first class
    /// and leaves its ability scores as they were. Returns false when it already has 40 levels.
    /// </summary>
    public static bool EnsureNativeLevels(SavedCharacter character)
    {
        if (character.NativeLevelCount >= 40)
            return false;
        var @class = character.GetClassByPosition(1);
        if (!SavedCharacterData.TryGetClassPackage(@class, out _))
            throw new System.InvalidOperationException($"No leveling package is available for class {@class}.");
        character.WriteInteger("Experience", System.Math.Max(character.ReadInteger("Experience"), 800000), 4);
        character.AddNativeLevels(40);
        return true;
    }
}

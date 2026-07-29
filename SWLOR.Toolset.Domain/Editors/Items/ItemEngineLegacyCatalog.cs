namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Base-game NWN:EE itemproperty rows the Module\uti corpus still carries that no SWLOR item
    /// editor stat group models: Ability, Armor, Enhancement, AttackPenalty, WeightReduction,
    /// BonusFeats, Damage, DamageRacialGroup, DamagePenalty, DamageReduced,
    /// DamageResist, Damage_Vulnerability, Darkvision, EnhancedContainer_Weight, Haste,
    /// ImprovedSavingThrows, Keen, Light, Mighty, DamageNone, OnHit, AttackBonus,
    /// AttackBonusRacialGroup, VampiricRegeneration, OnMonsterHit,
    /// Massive_Criticals, Freedom_of_Movement, Monster_damage, Special_Walk, Weight_Increase,
    /// OnHitCastSpell, VisualEffect, and Additional_Property. Property ids, labels, subtype
    /// tables, and CostTableResRef ids below are read straight off
    /// SWLOR_Haks/sw_2da/itempropdef.2da, verified against that corpus.
    /// </summary>
    /// <remarks>
    /// DamageImmunity (20), Immunity (37), and Regeneration (51) were removed from this catalog by
    /// owner decision, so the Engine card never shows them - the corpus's entries for all three
    /// (222/78/31 uses respectively) are preserved rather than migrated
    /// (<c>ItemCombinationAuditTests</c>' allowlist).
    /// </remarks>
    public static class ItemEngineLegacyCatalog
    {
        public static IReadOnlyList<ItemEngineLegacyDefinition> All { get; } = Build();

        public static bool Contains(int propertyId) =>
            All.Any(definition => definition.PropertyId == propertyId);

        private static List<ItemEngineLegacyDefinition> Build() => new()
        {
            new("Ability", 0, "IPRP_ABILITIES", 1),
            new("Armor", 1, null, 2),
            new("Enhancement", 6, null, 2),
            new("AttackPenalty", 10, null, 20),
            new("WeightReduction", 11, null, 10),
            new("BonusFeats", 12, "IPRP_FEATS", 0),
            new("Damage", 16, "IPRP_DAMAGETYPE", 4),
            new("DamageRacialGroup", 18, "racialtypes", 4),
            new("DamagePenalty", 21, null, 20),
            new("DamageReduced", 22, "IPRP_PROTECTION", 6),
            new("DamageResist", 23, "IPRP_DAMAGETYPE", 7),
            new("Damage_Vulnerability", 24, "IPRP_DAMAGETYPE", 22),
            new("Darkvision", 26, null, 0),
            new("EnhancedContainer_Weight", 32, null, 15),
            new("Haste", 35, null, 0),
            new("ImprovedSavingThrows", 40, "IPRP_SAVEELEMENT", 2),
            new("Keen", 43, null, 0),
            new("Light", 44, null, 18),
            new("Mighty", 45, null, 2),
            new("DamageNone", 47, null, 0),
            new("OnHit", 48, "IPRP_ONHIT", 24),
            new("AttackBonus", 56, null, 2),
            new("AttackBonusRacialGroup", 58, "racialtypes", 2),
            // UnlimitedAmmo (61) is deliberately absent: every ranged weapon in this game has
            // unlimited ammunition, so the property decides nothing and a per-item field for it is
            // a question with one answer. Existing values are preserved, just not offered.
            new("VampiricRegeneration", 67, null, 2),
            new("OnMonsterHit", 72, "IPRP_MONSTERHIT", 0),
            new("Massive_Criticals", 74, null, 4),
            new("Freedom_of_Movement", 75, null, 0),
            new("Monster_damage", 77, null, 19),
            new("Special_Walk", 79, "IPRP_WALK", 0),
            new("Weight_Increase", 81, null, 0),
            new("OnHitCastSpell", 82, "IPRP_ONHITSPELL", 26),
            new("VisualEffect", 83, "IPRP_VISUALFX", -1),
            new("Additional_Property", 87, null, 32)
        };
    }
}

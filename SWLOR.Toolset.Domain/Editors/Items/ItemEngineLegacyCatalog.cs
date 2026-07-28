namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Base-game NWN:EE itemproperty rows the Module\uti corpus still carries that no SWLOR item
    /// editor stat group models: Ability, Armor, Enhancement, AttackPenalty, WeightReduction,
    /// BonusFeats, Damage, DamageRacialGroup, DamageImmunity, DamagePenalty, DamageReduced,
    /// DamageResist, Damage_Vulnerability, Darkvision, EnhancedContainer_Weight, Haste, Immunity,
    /// ImprovedSavingThrows, Keen, Light, Mighty, DamageNone, OnHit, Regeneration, AttackBonus,
    /// AttackBonusRacialGroup, UnlimitedAmmo, VampiricRegeneration, OnMonsterHit,
    /// Massive_Criticals, Freedom_of_Movement, Monster_damage, Special_Walk, Weight_Increase,
    /// OnHitCastSpell, VisualEffect, and Additional_Property. Property ids, labels, subtype
    /// tables, and CostTableResRef ids below are read straight off
    /// SWLOR_Haks/sw_2da/itempropdef.2da, verified against that corpus.
    /// </summary>
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
            new("DamageImmunity", 20, "IPRP_DAMAGETYPE", 5),
            new("DamagePenalty", 21, null, 20),
            new("DamageReduced", 22, "IPRP_PROTECTION", 6),
            new("DamageResist", 23, "IPRP_DAMAGETYPE", 7),
            new("Damage_Vulnerability", 24, "IPRP_DAMAGETYPE", 22),
            new("Darkvision", 26, null, 0),
            new("EnhancedContainer_Weight", 32, null, 15),
            new("Haste", 35, null, 0),
            new("Immunity", 37, "IPRP_IMMUNITY", 0),
            new("ImprovedSavingThrows", 40, "IPRP_SAVEELEMENT", 2),
            new("Keen", 43, null, 0),
            new("Light", 44, null, 18),
            new("Mighty", 45, null, 2),
            new("DamageNone", 47, null, 0),
            new("OnHit", 48, "IPRP_ONHIT", 24),
            new("Regeneration", 51, null, 2),
            new("AttackBonus", 56, null, 2),
            new("AttackBonusRacialGroup", 58, "racialtypes", 2),
            new("UnlimitedAmmo", 61, "IPRP_AMMOTYPE", 14),
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

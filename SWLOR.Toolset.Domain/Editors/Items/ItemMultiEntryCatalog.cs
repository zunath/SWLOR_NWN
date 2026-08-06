namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Every itempropdef.2da property whose Subtype fans out across a real per-item lookup table
    /// rather than the small fixed enumerations <see cref="ItemStatCatalog"/> expands inline.
    /// Verified against the Module\uti corpus's per-property subtype census: FoodBonus (106, 33
    /// subs), ArmorEnhancement/WeaponEnhancement/StructureEnhancement/FoodEnhancement/
    /// StarshipEnhancement/ModuleEnhancement/DroidEnhancement (101/102/107/108/109/110/116, each
    /// subtyped), DroidPartType (122, 1-5), DroidInstruction (123, 40 subs), DNAType (128, 30 subs),
    /// EnzymeColor (129, 1-8), NPCSkill (125), WeaponDamageType (134, exclusive - at most one entry
    /// per item), UseLimitationPerk (100, perk id sub + level CostValue), and UseLimitationRacial (64).
    /// </summary>
    /// <remarks>
    /// The *Enhancement properties (101/102/107/108/109/110/116) mark an item AS an enhancement
    /// module (Craft.IsItemEnhancement, SWLOR.Game.Server/Service/Craft.cs) rather than counting
    /// gear slots - gear slots come from recipes instead - so they surface under the dedicated
    /// <see cref="ItemStatGroup.Enhancements"/> context rather than any real stat group.
    /// UseLimitationPerk (100) and UseLimitationRacial (64) are already catalogued as equip
    /// requirements in <see cref="ItemRequirementCatalog"/>; they are declared here too, with
    /// <see cref="ItemMultiEntryDefinition.IsRequirement"/> set, purely so a corpus-coverage sweep
    /// over every multi-subtype property in one place also accounts for them.
    /// </remarks>
    public static class ItemMultiEntryCatalog
    {
        public static IReadOnlyList<ItemMultiEntryDefinition> All { get; } = Build();

        public static ItemMultiEntryDefinition? ByPropertyId(int propertyId) =>
            All.FirstOrDefault(definition => definition.PropertyId == propertyId);

        public static bool Contains(int propertyId) =>
            All.Any(definition => definition.PropertyId == propertyId);

        private static List<ItemMultiEntryDefinition> Build() => new()
        {
            new ItemMultiEntryDefinition("Food Bonus", 106, "iprp_foodtype", 45, ItemStatGroup.Bonuses),

            new ItemMultiEntryDefinition("Armor Enhancement", 101, "iprp_enhancearm", 45, ItemStatGroup.Enhancements),
            new ItemMultiEntryDefinition("Weapon Enhancement", 102, "iprp_enhancewpn", 45, ItemStatGroup.Enhancements),
            new ItemMultiEntryDefinition("Structure Enhancement", 107, "iprp_enhancestr", 45, ItemStatGroup.Enhancements),
            new ItemMultiEntryDefinition("Food Enhancement", 108, "iprp_enhancefd", 45, ItemStatGroup.Enhancements),
            new ItemMultiEntryDefinition("Starship Enhancement", 109, "iprp_enhancesta", 45, ItemStatGroup.Enhancements),
            new ItemMultiEntryDefinition("Module Enhancement", 110, "iprp_enhancemod", 45, ItemStatGroup.Enhancements),
            new ItemMultiEntryDefinition("Droid Enhancement", 116, "iprp_enhancedrd", 45, ItemStatGroup.Enhancements),

            new ItemMultiEntryDefinition("Droid Part Type", 122, "iprp_droidpart", -1, ItemStatGroup.Droid),
            new ItemMultiEntryDefinition("Droid Instruction", 123, "iprp_droidperk", 33, ItemStatGroup.Droid),

            new ItemMultiEntryDefinition("DNA Type", 128, "iprp_dnatype", -1, ItemStatGroup.Incubation),
            new ItemMultiEntryDefinition("Enzyme Color", 129, "iprp_enzcolor", -1, ItemStatGroup.Incubation),

            new ItemMultiEntryDefinition("NPC Skill", 125, "iprp_skill", 48, ItemStatGroup.Npc),

            // WeaponDamageType (134): the corpus never carries more than one entry of this property
            // per item, CostValue always 0 - a single exclusive choice, not an add/remove list.
            // itempropdef.2da declares no CostTableResRef for this property ("****"); the corpus's
            // own entries store CostTable 0, matching every other no-cost-table property.
            new ItemMultiEntryDefinition(
                "Weapon Damage Type", 134, "iprp_c_dmgtype", 0, ItemStatGroup.Combat, IsExclusive: true),

            new ItemMultiEntryDefinition(
                "Required Perk", 100, "iprp_resperk", 33, Context: null, IsRequirement: true, SearchNoun: "Perks"),
            new ItemMultiEntryDefinition(
                "Required Race", 64, "racialtypes", -1, Context: null, IsRequirement: true, SearchNoun: "Races")
        };
    }
}

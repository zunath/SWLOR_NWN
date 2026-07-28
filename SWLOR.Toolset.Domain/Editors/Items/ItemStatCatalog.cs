namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Every stat an item editor can offer, grouped as itempropdef.2da groups them. Property ids,
    /// subtype rows, and CostTableResRef ids below are read straight off
    /// SWLOR_Haks/sw_2da/itempropdef.2da and its named SubTypeResRef tables (iprp_defensetype,
    /// iprp_resistance, iprp_crafttype, iprp_droidstat), verified against that corpus.
    /// </summary>
    public static class ItemStatCatalog
    {
        public static IReadOnlyList<ItemStatDefinition> All { get; } = Build();

        public static IReadOnlyList<ItemStatDefinition> ByGroup(ItemStatGroup group) =>
            All.Where(stat => stat.Group == group).OrderBy(stat => stat.DisplayOrder).ToList();

        private static List<ItemStatDefinition> Build()
        {
            var order = 0;
            var stats = new List<ItemStatDefinition>();

            void Add(ItemStatGroup group, string label, int propertyId, int subtypeId = -1, int costTableId = -1) =>
                stats.Add(new ItemStatDefinition(group, label, propertyId, subtypeId, costTableId, order++));

            // Defense - iprp_defensetype (property 94).
            Add(ItemStatGroup.Defense, "Physical Defense", 94, 1, 35);
            Add(ItemStatGroup.Defense, "Force Defense", 94, 2, 35);

            // Resistance - iprp_resistance (property 133). All 8 labeled rows: 1-4 plus the
            // non-contiguous 100-103 block (Mind/Mobility/Trauma/Disruption).
            Add(ItemStatGroup.Resistance, "Fire Resistance", 133, 1, 54);
            Add(ItemStatGroup.Resistance, "Poison Resistance", 133, 2, 54);
            Add(ItemStatGroup.Resistance, "Electrical Resistance", 133, 3, 54);
            Add(ItemStatGroup.Resistance, "Ice Resistance", 133, 4, 54);
            Add(ItemStatGroup.Resistance, "Mind Resistance", 133, 100, 54);
            Add(ItemStatGroup.Resistance, "Mobility Resistance", 133, 101, 54);
            Add(ItemStatGroup.Resistance, "Trauma Resistance", 133, 102, 54);
            Add(ItemStatGroup.Resistance, "Disruption Resistance", 133, 103, 54);

            // Vitals.
            Add(ItemStatGroup.Vitals, "HP", 90, costTableId: 37);
            Add(ItemStatGroup.Vitals, "FP", 91, costTableId: 38);
            Add(ItemStatGroup.Vitals, "STM", 92, costTableId: 36);
            Add(ItemStatGroup.Vitals, "FP Regen", 119, costTableId: 45);
            Add(ItemStatGroup.Vitals, "STM Regen", 120, costTableId: 45);

            // Combat.
            Add(ItemStatGroup.Combat, "Attack", 111, costTableId: 45);
            Add(ItemStatGroup.Combat, "Force Attack", 112, costTableId: 45);
            Add(ItemStatGroup.Combat, "DMG", 93, costTableId: 34);
            Add(ItemStatGroup.Combat, "Delay", 98, costTableId: 52);
            Add(ItemStatGroup.Combat, "Evasion", 117, costTableId: 41);
            Add(ItemStatGroup.Combat, "Combat Readiness", 118, costTableId: 42);
            Add(ItemStatGroup.Combat, "Enhancement Level", 104, costTableId: 45);

            // DamageStat (103) was removed from the editor by owner decision - the corpus's 9
            // entries are preserved-only (ItemCombinationAuditTests' allowlist), never offered here.

            // WeaponDamageType (134) is a single exclusive choice, not six numeric rows - the
            // corpus carries at most one entry per item, CostValue always 0. Modeled as an
            // ItemMultiEntryDefinition (IsExclusive) in ItemMultiEntryCatalog instead.

            // Crafting - iprp_crafttype (properties 88 Control, 89 Craftsmanship, 115 CPBonus),
            // plus the two flat crafting stats that have no subtype of their own.
            var craftTypes = new (int Subtype, string Label)[]
            {
                (1, "Smithery"), (2, "Engineering"), (3, "Fabrication"), (4, "Agriculture")
            };
            foreach (var (subtype, label) in craftTypes)
                Add(ItemStatGroup.Crafting, $"Control ({label})", 88, subtype, 46);
            foreach (var (subtype, label) in craftTypes)
                Add(ItemStatGroup.Crafting, $"Craftsmanship ({label})", 89, subtype, 47);
            foreach (var (subtype, label) in craftTypes)
                Add(ItemStatGroup.Crafting, $"CP Bonus ({label})", 115, subtype, 45);
            Add(ItemStatGroup.Crafting, "Progress Penalty", 95, costTableId: 44);
            Add(ItemStatGroup.Crafting, "Blueprint Level", 130, costTableId: 51);

            // Bonuses. FoodBonus (106) is multi-subtype (33 subs in the corpus, iprp_foodtype) and
            // is catalogued only in ItemMultiEntryCatalog under this same group's context, not here.
            Add(ItemStatGroup.Bonuses, "Structure Bonus", 105, costTableId: 45);
            Add(ItemStatGroup.Bonuses, "Starship Bonus", 114, costTableId: 45);
            Add(ItemStatGroup.Bonuses, "Module Bonus", 113, costTableId: 45);
            Add(ItemStatGroup.Bonuses, "Increased Price", 126, costTableId: 49);

            // Droid. DroidStat (121) carries every one of iprp_droidstat's 31 labeled rows - the
            // ability/resistance/tier block (2-20) and the weapon-skill block (115-126). DroidPartType
            // (122, iprp_droidpart, 1-5 subs) and DroidInstruction (123, iprp_droidperk, 40 subs) are
            // multi-subtype and are catalogued only in ItemMultiEntryCatalog; DroidPersonality (124)
            // has no per-subtype expansion asked of this pass and stays flat.
            Add(ItemStatGroup.Droid, "Droid Personality", 124);
            var droidStats = new (int Subtype, string Label)[]
            {
                (2, "Tier"), (3, "AI Slots"), (4, "HP"), (5, "STM"), (6, "MGT"), (7, "PER"),
                (8, "VIT"), (9, "WIL"), (10, "AGI"), (11, "SOC"),
                (12, "Fire Resistance"), (13, "Poison Resistance"), (14, "Electrical Resistance"),
                (15, "Ice Resistance"), (16, "Mind Resistance"), (17, "Mobility Resistance"),
                (18, "Trauma Resistance"), (19, "Disruption Resistance"), (20, "Armor"),
                (115, "Vibroblade"), (116, "Vibroknife"), (117, "Lightsaber"), (118, "Heavy Vibro"),
                (119, "Spear"), (120, "Twin Blade"), (121, "Saberstaff"), (122, "Katar"),
                (123, "Staff"), (124, "Pistol"), (125, "Rifle"), (126, "Throwing")
            };
            foreach (var (subtype, label) in droidStats)
                Add(ItemStatGroup.Droid, $"Droid {label}", 121, subtype, 45);

            // Incubation. DNAType (128, 30 subs) and EnzymeColor (129, 8 subs) are multi-subtype and
            // are catalogued only in ItemMultiEntryCatalog under this same group's context.
            Add(ItemStatGroup.Incubation, "Incubation", 127, costTableId: 50);

            // NPC-facing. NPCSkill (125, iprp_skill) is multi-subtype and is catalogued only in
            // ItemMultiEntryCatalog under this same group's context.
            Add(ItemStatGroup.Npc, "NPC HP", 96, costTableId: 39);
            Add(ItemStatGroup.Npc, "NPC Level", 99, costTableId: 43);
            Add(ItemStatGroup.Npc, "Monster Damage", 77, costTableId: 19);

            // Utility.
            Add(ItemStatGroup.Utility, "Stealth", 136, costTableId: 41);
            Add(ItemStatGroup.Utility, "Detection", 137, costTableId: 41);
            Add(ItemStatGroup.Utility, "Trap Bonus", 138, costTableId: 41);
            Add(ItemStatGroup.Utility, "Disarm", 139, costTableId: 41);
            Add(ItemStatGroup.Utility, "Poison Bonus", 140, costTableId: 41);
            Add(ItemStatGroup.Utility, "Lockpicking", 141, costTableId: 41);
            Add(ItemStatGroup.Utility, "Shield Deflection", 135, costTableId: 45);

            return stats;
        }
    }
}

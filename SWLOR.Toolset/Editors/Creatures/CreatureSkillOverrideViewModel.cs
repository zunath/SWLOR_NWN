using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One NPC skill whose roll level replaces the creature's general NPC Level.</summary>
    public sealed class CreatureSkillOverrideViewModel
    {
        public int SkillId { get; }
        public string Name { get; }
        public CreatureStatCellViewModel Level { get; }

        public CreatureSkillOverrideViewModel(
            int skillId,
            string name,
            CreatureEquipmentSet equipment,
            Func<CreatureEquipmentDocument> ensureSkin,
            Func<string, Action, bool> runEdit)
        {
            SkillId = skillId;
            Name = name;
            Level = new CreatureStatCellViewModel(
                name,
                () => equipment.ForSlot(CreaturePropertyCatalog.StatSkinSlot)?.Store
                    .GetPropertyValue(CreaturePropertyCatalog.NpcSkill, skillId) ?? 0,
                value => runEdit($"Change {name} skill override", () =>
                    ensureSkin().Store.SetPropertyValue(
                        CreaturePropertyCatalog.NpcSkill,
                        skillId,
                        48,
                        value == 0 ? null : value)),
                0,
                100);
        }
    }
}

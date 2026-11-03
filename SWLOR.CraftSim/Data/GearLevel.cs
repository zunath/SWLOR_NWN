using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Data
{
    /// <summary>
    /// The crafting stats a simulated crafter wears: a full set of one skill's gear tier, or nothing.
    /// </summary>
    public sealed class GearLevel
    {
        public GearLevel(string name, SkillType skill, int tier, IReadOnlyList<GearItem> items)
        {
            Name = name;
            Skill = skill;
            Tier = tier;
            Items = items;
            Control = items.Sum(i => i.Control);
            Craftsmanship = items.Sum(i => i.Craftsmanship);
            CpBonus = items.Sum(i => i.CpBonus);
        }

        /// <summary>A crafter with no crafting gear.</summary>
        public static GearLevel None(SkillType skill) => new("no gear", skill, 0, Array.Empty<GearItem>());

        /// <summary>A crafter whose stats are given directly, such as a test's.</summary>
        public static GearLevel Fixed(string name, SkillType skill, int control, int craftsmanship, int cpBonus) =>
            new(name, skill, 0, new[] { new GearItem(name, name, default, skill, control, craftsmanship, cpBonus) });

        public string Name { get; }
        public SkillType Skill { get; }
        public int Tier { get; }
        public IReadOnlyList<GearItem> Items { get; }
        public int Control { get; }
        public int Craftsmanship { get; }
        public int CpBonus { get; }

        public override string ToString() => $"{Name} (Control {Control}, Craftsmanship {Craftsmanship}, CP {CpBonus})";
    }
}

using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.CraftSim.Data
{
    /// <summary>One item blueprint's Control (88), Craftsmanship (89) and CPBonus (115) for one crafting skill, as cost-table values.</summary>
    public sealed class GearItem
    {
        public GearItem(string resref, string name, BaseItem baseItem, SkillType skill, int control, int craftsmanship, int cpBonus)
        {
            Resref = resref;
            Name = name;
            BaseItem = baseItem;
            Skill = skill;
            Control = control;
            Craftsmanship = craftsmanship;
            CpBonus = cpBonus;
        }

        public string Resref { get; }
        public string Name { get; }
        public BaseItem BaseItem { get; }
        public SkillType Skill { get; }
        public int Control { get; }
        public int Craftsmanship { get; }
        public int CpBonus { get; }

        public bool HasStats => Control > 0 || Craftsmanship > 0 || CpBonus > 0;
    }
}

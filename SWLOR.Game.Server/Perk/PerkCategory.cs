namespace SWLOR.Game.Server.Perk
{
    public class PerkCategory
    {
        public PerkCategoryType CategoryType { get; }
        public string Name { get; }
        public bool IsActive { get; }
        public int Sequence { get; }

        public PerkCategory(PerkCategoryType categoryType, string name, bool isActive, int sequence)
        {
            CategoryType = categoryType;
            Name = name;
            IsActive = isActive;
            Sequence = sequence;
        }
    }
}

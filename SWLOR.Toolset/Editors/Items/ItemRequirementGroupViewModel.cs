namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One Requirements-tab card: a fixed title (a skill category, "Required Stat", or
    /// "Other Requirements") and the cells under it. Kept separate from
    /// <see cref="ItemStatGroupViewModel"/> because a requirement group's title has no
    /// <see cref="Domain.Editors.Items.ItemStatGroup"/> to derive it from - the Requirements tab
    /// groups by <c>SkillCategoryType</c> and by requirement category instead.
    /// </summary>
    public sealed class ItemRequirementGroupViewModel
    {
        public string Title { get; }

        public IReadOnlyList<ItemStatCellViewModel> Cells { get; }

        public ItemRequirementGroupViewModel(string title, IReadOnlyList<ItemStatCellViewModel> cells)
        {
            Title = title;
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }
    }
}

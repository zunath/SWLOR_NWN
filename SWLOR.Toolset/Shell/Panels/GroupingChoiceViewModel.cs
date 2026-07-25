using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// A grouping option as a builder reads it.
    /// </summary>
    /// <remarks>
    /// The enum names describe the mechanism - Automatic, Folders, Flat - and the labels describe the
    /// result. "Planet" is what <see cref="CategoryGrouping.Automatic"/> produces for areas, which is
    /// the tree's headline use; the control's tooltip states the underlying rule for the types where
    /// the leading name segment is something else.
    /// </remarks>
    public sealed record GroupingChoiceViewModel(CategoryGrouping Value, string Label)
    {
        public override string ToString() => Label;
    }
}

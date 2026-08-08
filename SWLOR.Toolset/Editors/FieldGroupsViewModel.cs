using System.Collections.ObjectModel;

namespace SWLOR.Toolset.Editors
{
    /// <summary>The contents of a schema-driven tab: its groups of fields, in declared order.</summary>
    public sealed class FieldGroupsViewModel
    {
        public FieldGroupsViewModel(IEnumerable<EditorGroup> groups)
        {
            Groups = new ObservableCollection<EditorGroup>(groups);
        }

        public ObservableCollection<EditorGroup> Groups { get; }
    }
}

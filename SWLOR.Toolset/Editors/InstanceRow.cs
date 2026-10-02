using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Areas.Placement;

namespace SWLOR.Toolset.Editors
{
    /// <summary>One row of an instance-list grid: enough to display and re-locate the backing
    /// struct (by list index) for the detail form below the grid.</summary>
    public sealed class InstanceRow : ObservableObject
    {
        private string _tag;
        private float _x;
        private float _y;
        private float _z;
        private string _templateResRef;
        private string _displayName;

        public int Index { get; }

        /// <summary>
        /// The name this placement carries itself, or empty when it inherits its blueprint's.
        /// </summary>
        /// <remarks>
        /// Read here rather than resolved from the blueprint, because the two disagree constantly:
        /// see <see cref="InstanceFieldMap.GetDisplayName"/>. Callers that want a name that is never
        /// blank should go through <c>AreaEditorViewModel.ResolveInstanceName</c>, which falls back
        /// through the blueprint, the tag, and the resref.
        /// </remarks>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string TemplateResRef
        {
            get => _templateResRef;
            set => SetProperty(ref _templateResRef, value);
        }

        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        public float X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public float Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public float Z
        {
            get => _z;
            set => SetProperty(ref _z, value);
        }

        public InstanceRow(
            int index, string tag, string templateResRef, float x, float y, float z,
            string displayName = "")
        {
            Index = index;
            _tag = tag;
            _templateResRef = templateResRef;
            _x = x;
            _y = y;
            _z = z;
            _displayName = displayName;
        }
    }

}

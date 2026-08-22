using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>A progressively published ability row whose icon is resolved only when realized.</summary>
    public sealed partial class CreatureAbilityChoiceViewModel : ObservableObject
    {
        private int _iconRequested;

        public CreatureAbilityInfo Info { get; }
        public int FeatId => Info.FeatId;
        public string Name => Info.Name;
        public string Description => Info.Description;
        public string Classification => Info.Classification;

        [ObservableProperty]
        private Bitmap? _icon;

        public CreatureAbilityChoiceViewModel(CreatureAbilityInfo info)
        {
            Info = info;
        }

        internal bool TryBeginIconRequest() => Interlocked.Exchange(ref _iconRequested, 1) == 0;
    }
}

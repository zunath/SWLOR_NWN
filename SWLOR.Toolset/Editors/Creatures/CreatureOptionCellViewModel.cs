using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Exclusive subtype choice for a natural weapon property.</summary>
    public sealed partial class CreatureOptionCellViewModel : ObservableObject
    {
        private readonly Func<int?> _read;
        private readonly Func<int?, bool> _write;
        private bool _loading;

        public string Label { get; }
        public IReadOnlyList<CreatureOption> Options { get; }

        [ObservableProperty]
        private CreatureOption? _selected;

        public CreatureOptionCellViewModel(
            string label,
            IReadOnlyList<CreatureOption> options,
            Func<int?> read,
            Func<int?, bool> write)
        {
            Label = label;
            Options = options;
            _read = read;
            _write = write;
            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                var value = _read();
                Selected = value.HasValue
                    ? Options.FirstOrDefault(option => option.Value == value.Value)
                    : null;
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnSelectedChanged(CreatureOption? value)
        {
            if (_loading)
                return;
            if (!_write(value?.Value))
                Reload();
        }
    }
}

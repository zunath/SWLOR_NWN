using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Validated numeric cell backed by a creature field or linked item property.</summary>
    public sealed partial class CreatureStatCellViewModel : ObservableObject
    {
        private readonly Func<int> _read;
        private readonly Func<int, bool> _write;
        private bool _loading;

        public string Label { get; }
        public int Minimum { get; }
        public int Maximum { get; }

        [ObservableProperty]
        private decimal _number;

        public CreatureStatCellViewModel(
            string label,
            Func<int> read,
            Func<int, bool> write,
            int minimum = 0,
            int maximum = ushort.MaxValue)
        {
            Label = label;
            _read = read;
            _write = write;
            Minimum = minimum;
            Maximum = maximum;
            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                Number = _read();
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading)
                return;

            if (decimal.Truncate(value) != value || value < Minimum || value > Maximum ||
                !_write((int)value))
            {
                Reload();
            }
        }
    }
}

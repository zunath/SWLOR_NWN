namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One independently editable left/right body-part row with optional right-from-left mirroring.
    /// Shared by item armor and creature body editors so both surfaces apply the same edit rules.
    /// </summary>
    public sealed class BodyPartPairViewModel
    {
        private readonly Func<bool> _isMirrored;
        private readonly IReadOnlyList<int> _leftOptions;
        private readonly IReadOnlyList<int> _rightOptions;
        private readonly IReadOnlyList<int> _mirroredOptions;

        public string Label { get; }
        public ItemFieldCellViewModel Left { get; }
        public ItemFieldCellViewModel Right { get; }

        public BodyPartPairViewModel(
            string label,
            Func<int?> readLeft,
            Func<int?> readRight,
            Func<int, bool> writeLeft,
            Func<int, bool> writeRight,
            Func<int, bool> writeBoth,
            Func<bool> isMirrored,
            int minimum,
            int maximum,
            IReadOnlyList<int>? leftOptions = null,
            IReadOnlyList<int>? rightOptions = null)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _isMirrored = isMirrored ?? throw new ArgumentNullException(nameof(isMirrored));
            _leftOptions = leftOptions ?? Array.Empty<int>();
            _rightOptions = rightOptions ?? Array.Empty<int>();
            _mirroredOptions = _leftOptions.Count == 0 || _rightOptions.Count == 0
                ? Array.Empty<int>()
                : _leftOptions.Intersect(_rightOptions).OrderBy(number => number).ToList();

            ItemFieldCellViewModel? right = null;
            Left = new ItemFieldCellViewModel(
                $"Left {label}",
                readLeft,
                value =>
                {
                    var applied = _isMirrored() ? writeBoth(value) : writeLeft(value);
                    if (applied && _isMirrored())
                        right?.Reload();
                    return applied;
                },
                minimum,
                maximum,
                options: _leftOptions);

            right = new ItemFieldCellViewModel(
                $"Right {label}",
                readRight,
                writeRight,
                minimum,
                maximum,
                options: _rightOptions);
            Right = right;

            SetMirrored(_isMirrored());
        }

        /// <summary>
        /// Changes which model values are safe for both sides and makes the right editor read-only
        /// only while mirroring is active. The owner performs the one-time right-from-left write.
        /// </summary>
        public void SetMirrored(bool mirrored)
        {
            Left.SetOptions(mirrored ? _mirroredOptions : _leftOptions);
            Right.SetOptions(mirrored ? _mirroredOptions : _rightOptions);
            Right.IsReadOnly = mirrored;
        }

        public void Reload()
        {
            Left.Reload();
            Right.Reload();
        }
    }
}

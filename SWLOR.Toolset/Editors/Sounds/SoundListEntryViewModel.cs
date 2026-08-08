namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>One ordered Sound ResRef entry.</summary>
    public sealed class SoundListEntryViewModel
    {
        public int Index { get; }

        public string ResRef { get; }

        public SoundListEntryViewModel(int index, string resRef)
        {
            Index = index;
            ResRef = resRef;
        }
    }
}

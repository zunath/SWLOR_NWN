namespace SWLOR.Tools.AreaBuilder
{
    /// <summary>Lightweight ComboBox item: stable key plus the display text shown to the user.</summary>
    internal sealed class KeyedItem
    {
        public string Key { get; }
        public string DisplayName { get; }

        public KeyedItem(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }
    }
}

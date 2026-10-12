namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// One song row in the music picker window.
    /// </summary>
    public class MusicPickerEntry
    {
        public int TrackId { get; }
        public string Name { get; }
        public string Category { get; }
        public bool IsAvailable { get; }

        public MusicPickerEntry(int trackId, string name, string category, bool isAvailable)
        {
            TrackId = trackId;
            Name = name;
            Category = category;
            IsAvailable = isAvailable;
        }
    }
}

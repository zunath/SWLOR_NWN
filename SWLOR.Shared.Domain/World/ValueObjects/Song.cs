namespace SWLOR.Shared.Domain.World.ValueObjects
{
    /// <summary>
    /// Represents a song loaded from the ambientmusic.2da file
    /// </summary>
    public class Song
    {
        /// <summary>
        /// The ID of the song (row number in 2DA file)
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// The display name of the song
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Whether this song is available as a player battle song
        /// </summary>
        public bool IsAvailableAsBattleSong { get; }

        public Song(int id, string displayName, bool isAvailableAsBattleSong = false)
        {
            ID = id;
            DisplayName = displayName;
            IsAvailableAsBattleSong = isAvailableAsBattleSong;
        }
    }
}

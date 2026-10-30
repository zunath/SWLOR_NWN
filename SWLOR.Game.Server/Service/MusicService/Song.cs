namespace SWLOR.Game.Server.Service.MusicService
{
    /// <summary>
    /// A track of the ambientmusic.2da file: its row and the name it is shown with.
    /// </summary>
    public class Song
    {
        public int ID { get; }
        public string DisplayName { get; }

        public Song(int id, string displayName)
        {
            ID = id;
            DisplayName = displayName;
        }
    }
}

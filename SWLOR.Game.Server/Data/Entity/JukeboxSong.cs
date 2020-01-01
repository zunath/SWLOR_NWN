using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class JukeboxSong: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int AmbientMusicID { get; set; }
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[JukeboxSong]")]
    public class JukeboxSong: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public int AmbientMusicID { get; set; }
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }

        public IEntity Clone()
        {
            return new JukeboxSong
            {
                ID = ID,
                AmbientMusicID = AmbientMusicID,
                FileName = FileName,
                DisplayName = DisplayName,
                IsActive = IsActive
            };
        }
    }
}

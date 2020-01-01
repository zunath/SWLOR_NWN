using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Download: IEntity
    {
        public Download()
        {
            Name = string.Empty;
            Description = string.Empty;
            LocalPath = string.Empty;
            FileName = string.Empty;
            ContentType = string.Empty;
            Instructions = string.Empty;
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string LocalPath { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string Instructions { get; set; }
    }
}

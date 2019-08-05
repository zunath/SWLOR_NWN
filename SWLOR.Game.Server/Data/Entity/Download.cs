using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Download]")]
    public class Download: IEntity
    {
        public Download()
        {
            Name = "";
            Description = "";
            Url = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }

        public IEntity Clone()
        {
            return new Download
            {
                ID = ID,
                Name = Name,
                Description = Description,
                Url = Url,
                IsActive = IsActive
            };
        }
    }
}

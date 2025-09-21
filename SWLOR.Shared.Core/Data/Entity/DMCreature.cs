using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class DMCreature : EntityBase
    {
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string Tag { get; set; }
        public string Data { get; set; }

        public DMCreature(string name, string tag, string data)
        {
            Name = name;
            Tag = tag;
            Data = data;
        }
        public DMCreature()
        {
            Name = string.Empty;
            Tag = string.Empty;
            Data = string.Empty;
        }
    }
}

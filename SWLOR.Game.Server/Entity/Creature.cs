namespace SWLOR.Game.Server.Entity
{
    public class Creature : EntityBase
    {
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string Tag { get; set; }
        public string Data { get; set; }

        public Creature(string name, string tag, string data)
        {
            Name = name;
            Tag = tag;
            Data = data;
        }
        public Creature()
        {
            Name = string.Empty;
            Tag = string.Empty;
            Data = string.Empty;
        }
    }
}

using System.Security.AccessControl;

namespace SWLOR.Game.Server.Entity
{
    internal class TemplateArea : EntityBase
    {
        public string Data { get; set; }
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string Tag { get; set; }
        [Indexed]
        public string ResRef { get; set; }

        public TemplateArea()
        {
            Data = string.Empty;
            Name = string.Empty;
            Tag = string.Empty;
            ResRef = string.Empty;
        }
    }
}

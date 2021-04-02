using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum GuildType
    {
        [Guild("Unknown", false)]
        Invalid = 0,
        [Guild("Hunter's Guild", true)]
        HuntersGuild = 1,
        [Guild("Engineering Guild", true)]
        EngineeringGuild = 2,
        [Guild("Smithery Guild", true)]
        SmitheryGuild = 3
    }

    public class GuildAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public GuildAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}

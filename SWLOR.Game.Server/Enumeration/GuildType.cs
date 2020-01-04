using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum GuildType
    {
        [GuildType("Unknown", "Unknown")]
        Unknown = 0,
        [GuildType("Hunter's Guild", "Specializes in the detection and removal of threats across the galaxy.")]
        HuntersGuild = 1,
        [GuildType("Engineering Guild", "Specializes in the construction of engineering and electronic items.")]
        EngineeringGuild = 2,
        [GuildType("Weaponsmith Guild", "Specializes in the construction of weaponry.")]
        WeaponsmithGuild = 3,
        [GuildType("Armorsmith Guild", "Specializes in the construction of armor.")]
        ArmorsmithGuild = 4
    }

    public class GuildTypeAttribute: Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public GuildTypeAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}

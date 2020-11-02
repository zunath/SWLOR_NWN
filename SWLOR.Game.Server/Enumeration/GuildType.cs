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
        [Guild("Weaponsmith Guild", true)]
        WeaponsmithGuild = 3,
        [Guild("Armorsmith Guild", true)]
        ArmorsmithGuild = 4
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

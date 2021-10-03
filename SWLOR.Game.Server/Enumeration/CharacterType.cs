using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum CharacterType
    {
        [CharacterType("Invalid")]
        Invalid = 0,
        [CharacterType("Standard")]
        Standard = 1,
        [CharacterType("Force Sensitive")]
        ForceSensitive = 2
    }

    public class CharacterTypeAttribute : Attribute
    {
        public string Name { get; set; }

        public CharacterTypeAttribute(string name)
        {
            Name = name;
        }
    }
}

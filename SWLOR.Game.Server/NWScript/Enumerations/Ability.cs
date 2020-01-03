using System.ComponentModel;

namespace SWLOR.Game.Server.NWScript.Enumerations
{
    public enum Ability
    {
        [Description("N/A")]
        Invalid = -1,
        [Description("STR")]
        Strength = 0,
        [Description("DEX")]
        Dexterity = 1,
        [Description("CON")]
        Constitution = 2,
        [Description("INT")]
        Intelligence = 3,
        [Description("WIS")]
        Wisdom = 4,
        [Description("CHA")]
        Charisma = 5
    }
}

using System;

namespace SWLOR.Game.Server.Perk
{
    public class PerkCooldownAttribute: Attribute
    {
        public string Name { get; set; }
        public float Delay { get; set; }

        public PerkCooldownAttribute(string name, float delay)
        {
            Name = name;
            Delay = delay;
        }
    }
}

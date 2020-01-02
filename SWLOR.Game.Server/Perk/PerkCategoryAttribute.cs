using System;
using System.Collections.Generic;
using System.Text;

namespace SWLOR.Game.Server.Perk
{
    public class PerkCategoryAttribute: Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }

        public PerkCategoryAttribute(string name, bool isActive, int sequence)
        {
            Name = name;
            IsActive = isActive;
            Sequence = sequence;
        }
    }
}

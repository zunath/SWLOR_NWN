using System;

namespace SWLOR.Game.Server.Domain.World
{
    public class MusicCategoryAttribute : Attribute
    {
        public string Name { get; }

        public MusicCategoryAttribute(string name)
        {
            Name = name;
        }
    }
}

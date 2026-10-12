using System;

namespace SWLOR.Game.Server.Service.MusicService
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

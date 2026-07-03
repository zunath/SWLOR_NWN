using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum EmoteCategoryType
    {
        [EmoteCategory("Invalid", false)]
        Invalid = 0,
        [EmoteCategory("All", true)]
        All = 1,
        [EmoteCategory("Combat", true)]
        Combat = 2,
        [EmoteCategory("Exploration", true)]
        Exploration = 3,
        [EmoteCategory("Tasks", true)]
        Tasks = 4,
        [EmoteCategory("Social", true)]
        Social = 5,
        [EmoteCategory("Feelings", true)]
        Feelings = 6
    }

    public class EmoteCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsVisible { get; set; }

        public EmoteCategoryAttribute(string name, bool isVisible)
        {
            Name = name;
            IsVisible = isVisible;
        }
    }
}

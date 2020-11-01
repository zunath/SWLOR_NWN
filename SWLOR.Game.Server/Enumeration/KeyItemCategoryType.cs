using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum KeyItemCategoryType
    {
        [KeyItemCategory("Invalid", false)]
        Invalid = 0,
        [KeyItemCategory("Maps", true)]
        Maps = 1,
        [KeyItemCategory("Quest Items", true)]
        QuestItems = 2,
        [KeyItemCategory("Documents", true)]
        Documents = 3,
        [KeyItemCategory("Blueprints", true)]
        Blueprints = 4,
        [KeyItemCategory("Keys", true)]
        Keys = 5
    }

    public class KeyItemCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public KeyItemCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}

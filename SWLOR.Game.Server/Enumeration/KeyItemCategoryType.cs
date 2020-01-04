using System.ComponentModel;

namespace SWLOR.Game.Server.Enumeration
{
    public enum KeyItemCategoryType
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Maps")]
        Maps = 1,
        [Description("Quest Items")]
        QuestItems = 2,
        [Description("Documents")]
        Documents = 3,
        [Description("Blueprints")]
        Blueprints = 4,
        [Description("Keys")]
        Keys = 5
    }
}

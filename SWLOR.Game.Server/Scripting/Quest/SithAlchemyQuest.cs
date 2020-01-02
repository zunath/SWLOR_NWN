using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class SithAlchemyQuest: AbstractQuest
    {
        public SithAlchemyQuest()
        {
            // This is a placeholder to prevent players from accessing the Sith Alchemy ability before it's available.
            CreateQuest(99, "Sith Alchemy", string.Empty);
        }
    }
}

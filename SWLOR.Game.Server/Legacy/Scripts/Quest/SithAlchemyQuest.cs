using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
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

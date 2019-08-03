using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest
{
    public abstract class AbstractQuest
    {
        public IQuest Quest { get; private set; }
        protected IQuest CreateQuest(int id, string name, string journalTag)
        {
            Quest = new Quest(id, name, journalTag);
            return Quest;
        }
    }
}

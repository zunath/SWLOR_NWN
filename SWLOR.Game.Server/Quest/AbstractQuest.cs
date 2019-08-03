using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest
{
    public abstract class AbstractQuest
    {
        private IQuest _quest;

        protected IQuest CreateQuest(string name, string journalTag)
        {
            _quest = new Quest(name, journalTag);
            return _quest;
        }
    }
}

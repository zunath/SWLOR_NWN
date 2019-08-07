using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(NWPlayer player);
    }
}

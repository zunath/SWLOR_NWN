using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Quest.Contracts
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(NWPlayer player);
    }
}

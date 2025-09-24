namespace SWLOR.Component.Quest.Contracts
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(uint player);
    }
}

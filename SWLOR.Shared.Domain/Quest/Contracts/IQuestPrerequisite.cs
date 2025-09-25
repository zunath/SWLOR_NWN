namespace SWLOR.Shared.Domain.Quest.Contracts
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(uint player);
    }
}

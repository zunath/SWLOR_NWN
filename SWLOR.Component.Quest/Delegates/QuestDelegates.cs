namespace SWLOR.Shared.Core.Delegates
{
    public delegate void AcceptQuestDelegate(uint player, uint questSourceObject);
    public delegate void AbandonQuestDelegate(uint player);
    public delegate void AdvanceQuestDelegate(uint player, uint questSourceObject, int questState);
    public delegate void CompleteQuestDelegate(uint player, uint questSourceObject);
}

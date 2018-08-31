namespace SWLOR.Game.Server.GameObject.Contracts
{
    public interface INWItem
    {
        int AC { get; }
        int BaseItemType { get; }
        int Charges { get; set; }
        bool IsCursed { get; set; }
        bool IsDroppable { get; set; }
        bool IsStolen { get; set; }
        NWCreature Possessor { get; }
        int StackSize { get; set; }
        float Weight { get; }
    }
}
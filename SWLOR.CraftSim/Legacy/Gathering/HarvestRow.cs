namespace SWLOR.CraftSim.Legacy.Gathering
{
    public sealed record HarvestRow(
        int MightModifier, double BonusItemChance, double ExpectedHarvests, double ExpectedItems, double SimulatedHarvests, double SimulatedItems);
}

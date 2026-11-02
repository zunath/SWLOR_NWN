namespace SWLOR.CraftSim.Legacy.Crafting
{
    public enum LegacyActionResult
    {
        /// <summary>Locked, unaffordable or after the craft ended: nothing changed (a refused Rapid or Careful Synthesis still spends a Veneration step).</summary>
        Refused,
        RollSucceeded,
        RollFailed
    }
}

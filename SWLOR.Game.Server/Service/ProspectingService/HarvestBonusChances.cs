namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// The chances, in percent, that a finished harvest gains 1 more item from the harvester's city (Player City Revamp): each is its own
    /// roll, and 0 means it does not apply.
    /// </summary>
    /// <param name="ProspectorsCharterPercent">Prospectors' Charter's chance.</param>
    /// <param name="HomeGroundPercent">Home Ground's chance.</param>
    public readonly record struct HarvestBonusChances(int ProspectorsCharterPercent, int HomeGroundPercent)
    {
        public static readonly HarvestBonusChances None = new(0, 0);
    }
}

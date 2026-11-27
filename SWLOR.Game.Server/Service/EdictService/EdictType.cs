namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// The city-wide policies a mayor can enact (Player City Revamp, "Edict catalog"). Names and values are saved with
    /// cities and never change.
    /// </summary>
    public enum EdictType
    {
        Invalid = 0,
        FestivalSeason = 1,
        Alliance = 2,
        Embargo = 3,
        OpenGates = 4,
        GuildCharter = 5,
        ProspectorsCharter = 6,
        DrydockSubsidy = 7,
        HuntingGrounds = 8,
        ArtisansCharter = 9,
        BreedersCharter = 10,
        IndustrialCharter = 11,
        Shadowport = 12
    }
}

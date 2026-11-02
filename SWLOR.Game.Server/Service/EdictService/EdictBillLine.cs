namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>One Edict's charge on a city's weekly upkeep bill.</summary>
    public readonly record struct EdictBillLine(EdictType Type, string Name, int Credits);
}

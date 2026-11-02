namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>Where the piece's Thermal sits against its working range.</summary>
public enum ForgeThermalZone
{
    /// <summary>Under the range: Hydro-Press does little and cracks Integrity.</summary>
    Below = 1,

    /// <summary>Inside the range.</summary>
    InRange = 2,

    /// <summary>Over the range: Hydro-Press shapes more and cracks Integrity harder.</summary>
    Above = 3
}

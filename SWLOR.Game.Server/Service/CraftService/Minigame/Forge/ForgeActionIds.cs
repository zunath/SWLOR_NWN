namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>The ids of the Plasma Forge's actions. The last five are added by Smithery perks.</summary>
public static class ForgeActionIds
{
    public const string RampPlasma = "ramp_plasma";
    public const string HydroPress = "hydro_press";
    public const string VibroHone = "vibro_hone";
    public const string Rebond = "rebond";
    public const string CryoQuench = "cryo_quench";

    /// <summary>Forgemaster: a large Shape gain.</summary>
    public const string OverdrivePress = "overdrive_press";

    /// <summary>Forgemaster: Thermal in one action.</summary>
    public const string SurgePlasma = "surge_plasma";

    /// <summary>Forgemaster capstone: a once-per-craft Hydro-Press above the working range.</summary>
    public const string RedLinePress = "red_line_press";

    /// <summary>Artisan: Finish at any Thermal.</summary>
    public const string MicroHone = "micro_hone";

    /// <summary>Artisan: the piece stops cooling for a few actions.</summary>
    public const string TemperHold = "temper_hold";
}

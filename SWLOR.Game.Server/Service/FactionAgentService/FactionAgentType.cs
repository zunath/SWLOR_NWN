namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// A faction agent (plan section 29). Held missions store it by number, so a value never changes meaning.
    /// </summary>
    public enum FactionAgentType
    {
        Invalid = 0,
        RepublicVeles = 1,
        RepublicDantooine = 2,
        CzerkaCZ220 = 3,
        CzerkaHutlar = 4,
        SmugglerMonCala = 5,
        SmugglerStation = 6,
        HuttTatooine = 7,
        HuttSmugglersMoon = 8,
        MandalorianHutlar = 9,
        MandalorianDathomir = 10,
        SithDathomir = 11,
        SithKorriban = 12,
        SmugglerEshan = 13,
    }
}

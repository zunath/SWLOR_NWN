namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// One weapon of a space NPC ship preset: the module it is fitted with and the numbers it fires with
    /// in place of the module step's own.
    /// </summary>
    public class ShipNpcWeapon
    {
        public string ModuleTag { get; set; }
        public int Dmg { get; set; }
        public int Attack { get; set; }
        public int Accuracy { get; set; }
        public int Rank { get; set; }
    }
}

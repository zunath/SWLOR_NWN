namespace SWLOR.Game.Server.Service.ImplantService
{
    public delegate void ImplantInstalledDelegate(uint creature);

    public delegate void ImplantUninstalledDelegate(uint creature);

    public delegate string ImplantValidationDelegate(uint creature);

    public class ImplantDetail
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ImplantSlotType Slot { get; set; }
        public int RequiredLevel { get; set; }
        public ImplantInstalledDelegate InstalledAction { get; set; }
        public ImplantUninstalledDelegate UninstalledAction { get; set; }
        public ImplantValidationDelegate ValidationAction { get; set; }
    }
}

namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakAiDecision
    {
        public bool ShouldPlaySideCard { get; set; }
        public int SideHandIndex { get; set; }
        public int SelectedValue { get; set; }
        public bool ShouldStand { get; set; }

        public static PazaakAiDecision EndTurn()
        {
            return new PazaakAiDecision
            {
                SideHandIndex = -1,
                SelectedValue = 0,
                ShouldPlaySideCard = false,
                ShouldStand = false,
            };
        }
    }
}

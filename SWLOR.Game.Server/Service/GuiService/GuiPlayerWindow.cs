namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiPlayerWindow
    {
        public int WindowToken { get; set; }
        public IGuiDataModel DataModel { get; set; }

        public GuiPlayerWindow(IGuiDataModel dataModel)
        {
            DataModel = dataModel;
        }

    }
}

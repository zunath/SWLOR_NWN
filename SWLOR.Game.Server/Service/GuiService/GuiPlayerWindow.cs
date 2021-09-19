namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiPlayerWindow { }
    public class GuiPlayerWindow<T> : GuiPlayerWindow
        where T: IGuiDataModel
    {
        public int WindowToken { get; set; }
        public T DataModel { get; set; }

        public GuiPlayerWindow(T dataModel)
        {
            DataModel = dataModel;
        }
    }
}

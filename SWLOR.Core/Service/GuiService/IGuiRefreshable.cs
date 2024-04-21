namespace SWLOR.Core.Service.GuiService
{
    public interface IGuiRefreshable<in T>
        where T: IGuiRefreshEvent
    {
        void Refresh(T payload);
    }
}

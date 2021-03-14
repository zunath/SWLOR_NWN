namespace SWLOR.Game.Server.Service.ItemModService
{
    public delegate void ApplyItemModEffectDelegate(uint user, uint mod, uint item);

    public class ItemModDetail
    {
        public string Name { get; set; }
        public ApplyItemModEffectDelegate ApplyItemModAction { get; set; }

        public ItemModDetail()
        {
            Name = string.Empty;
        }
    }
}

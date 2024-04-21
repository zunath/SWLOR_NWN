namespace SWLOR.Core.Service.ItemService
{
    public interface IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems();
    }
}

namespace SWLOR.Core.Service.SpaceService
{
    public interface ISpaceObjectListDefinition
    {
        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects();
    }
}

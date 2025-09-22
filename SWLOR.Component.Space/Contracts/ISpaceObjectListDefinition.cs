using SWLOR.Component.Space.Model;

namespace SWLOR.Component.Space.Contracts
{
    public interface ISpaceObjectListDefinition
    {
        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects();
    }
}

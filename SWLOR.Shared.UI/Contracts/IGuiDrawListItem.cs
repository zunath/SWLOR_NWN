using SWLOR.NWN.API.Engine;

namespace SWLOR.Shared.UI.Contracts
{
    public interface IGuiDrawListItem
    {
        /// <summary>
        /// Builds the draw list item element.
        /// </summary>
        /// <returns>Json representing the draw list item element.</returns>
        Json ToJson();
    }
}

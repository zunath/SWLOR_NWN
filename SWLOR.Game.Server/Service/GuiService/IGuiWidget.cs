using System.Collections.Generic;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiWidget
    {
        /// <summary>
        /// Retrieves the Id of the Widget.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Retrieves the list of elements this widget contains.
        /// </summary>
        public List<IGuiWidget> Elements { get; }

        /// <summary>
        /// The explicit height declared on this widget, or 0 if none was set.
        /// Used by layout validation; only values &gt; 0 are emitted to NUI.
        /// </summary>
        float DeclaredHeight { get; }

        /// <summary>
        /// The explicit margin declared on this widget. A negative value means the
        /// engine default; zero means the author deliberately removed the margin.
        /// </summary>
        float DeclaredMargin { get; }

        /// <summary>
        /// Retrieves the set of events registered for this widget.
        /// </summary>
        public Dictionary<string, GuiMethodDetail> Events { get; }

        /// <summary>
        /// Builds the widget element.
        /// </summary>
        /// <returns>Json representing the widget element.</returns>
        Json ToJson();

        /// <summary>
        /// This property is used to work around a Vector error found in version 8193.34 of NWN.
        /// If Beamdog fixes this issue this property and all related code can be removed.
        /// Details on the Vector issue logged here: https://github.com/Beamdog/nwn-issues/issues/427
        /// </summary>
        public string VisibilityOverrideBindName { get; set; }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.UI
{
    public class GuiConstructedWindow
    {
        public GuiWindowType Type { get; set; }
        public string WindowId { get; set; }
        public Json Window { get; set; }
        public CreatePlayerWindowDelegate CreatePlayerWindowAction { get; set; }

        /// <summary>
        /// The view model type of a window <see cref="GuiWindowBuilder{T}"/> built. <see cref="GuiService"/> builds each player's
        /// view model of it with the content factory, through <see cref="CreatePlayerWindowAction"/>, which it sets when it caches the window.
        /// </summary>
        public Type ViewModelType { get; set; }
        public GuiRectangle InitialGeometry { get; set; }
        public Dictionary<string, Json> PartialViews { get; set; }
        public IReadOnlyList<string> LayoutFindings { get; set; }

        /// <summary>
        /// When true, the window stays open when its player changes areas.
        /// </summary>
        public bool KeepsOpenOnAreaChange { get; set; }

        /// <summary>
        /// The JSON text of each partial view, keyed by partial name.
        /// </summary>
        public IReadOnlyDictionary<string, string> PartialViewLayouts { get; set; }

        public GuiConstructedWindow(
            GuiWindowType type,
            string windowId,
            Json window,
            GuiRectangle initialGeometry,
            Dictionary<string, Json> partialViews,
            IReadOnlyList<string> layoutFindings,
            IReadOnlyDictionary<string, string> partialViewLayouts,
            CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            InitialGeometry = initialGeometry;
            PartialViews = partialViews;
            LayoutFindings = layoutFindings;
            PartialViewLayouts = partialViewLayouts;
            CreatePlayerWindowAction = createPlayerWindowAction;
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiWindow
    {
        public string Title { get; set; }
        public string TitleBindName { get; set; }
        public bool IsTitleBound => !string.IsNullOrWhiteSpace(TitleBindName);

        public GuiRectangle Geometry { get; set; }
        public string GeometryBindName { get; set; }
        public bool IsGeometryBound => !string.IsNullOrWhiteSpace(GeometryBindName);

        public bool IsResizable { get; set; }
        public string IsResizableBindName { get; set; }
        public bool IsResizableBound => !string.IsNullOrWhiteSpace(IsResizableBindName);

        public bool IsCollapsed { get; set; }
        public string IsCollapsedBindName { get; set; }
        public bool IsCollapsedBound => !string.IsNullOrWhiteSpace(IsCollapsedBindName);

        public bool IsClosable { get; set; }
        public string IsClosableBindName { get; set; }
        public bool IsClosableBound => !string.IsNullOrWhiteSpace(IsClosableBindName);

        public bool IsTransparent { get; set; }
        public string IsTransparentBindName { get; set; }
        public bool IsTransparentBound => !string.IsNullOrWhiteSpace(IsTransparentBindName);

        public bool ShowBorder { get; set; }
        public string ShowBorderBindName { get; set; }
        public bool IsShowBorderBound => !string.IsNullOrWhiteSpace(ShowBorderBindName);

        public List<GuiColumn> Columns { get; set; }

        public GuiWindow()
        {
            Title = "New Window";
            Geometry = new GuiRectangle(0f, 0f, 400f, 400f);
            IsResizable = true;
            IsCollapsed = false;
            IsClosable = true;
            IsTransparent = false;
            ShowBorder = true;

            Columns = new List<GuiColumn>();
        }

        public Json Build()
        {
            var root = JsonArray();

            foreach (var column in Columns)
            {
                root = JsonArrayInsert(root, column.ToJson());
            }

            root = Nui.Column(root);

            var title = IsTitleBound ? Nui.Bind(TitleBindName) : JsonString(Title);
            var geometry = IsGeometryBound ? Nui.Bind(GeometryBindName) : Geometry.ToJson();
            var isResizable = IsResizableBound ? Nui.Bind(IsResizableBindName) : JsonBool(IsResizable);
            var isCollapsed = IsCollapsedBound ? Nui.Bind(IsCollapsedBindName) : JsonBool(IsCollapsed);
            var isClosable = IsClosableBound ? Nui.Bind(IsClosableBindName) : JsonBool(IsClosable);
            var isTransparent = IsTransparentBound ? Nui.Bind(IsTransparentBindName) : JsonBool(IsTransparent);
            var showBorder = IsShowBorderBound ? Nui.Bind(ShowBorderBindName) : JsonBool(ShowBorder);

            return Nui.Window(root, title, geometry, isResizable, isCollapsed, isClosable, isTransparent, showBorder);
        }
    }
}

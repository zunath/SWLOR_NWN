using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiWindow
    {
        private string Title { get; set; }
        private string TitleBindName { get; set; }
        private bool IsTitleBound => !string.IsNullOrWhiteSpace(TitleBindName);
        
        private GuiRectangle Geometry { get; set; }
        private string GeometryBindName { get; set; }
        private bool IsGeometryBound => !string.IsNullOrWhiteSpace(GeometryBindName);
        
        private bool IsResizable { get; set; }
        private string IsResizableBindName { get; set; }
        private bool IsResizableBound => !string.IsNullOrWhiteSpace(IsResizableBindName);
        
        private bool IsCollapsed { get; set; }
        private string IsCollapsedBindName { get; set; }
        private bool IsCollapsedBound => !string.IsNullOrWhiteSpace(IsCollapsedBindName);
        
        private bool IsClosable { get; set; }
        private string IsClosableBindName { get; set; }
        private bool IsClosableBound => !string.IsNullOrWhiteSpace(IsClosableBindName);
        
        private bool IsTransparent { get; set; }
        private string IsTransparentBindName { get; set; }
        private bool IsTransparentBound => !string.IsNullOrWhiteSpace(IsTransparentBindName);
        
        private bool ShowBorder { get; set; }
        private string ShowBorderBindName { get; set; }
        private bool IsShowBorderBound => !string.IsNullOrWhiteSpace(ShowBorderBindName);

        private List<GuiColumn> Columns { get; set; }

        public GuiWindow SetTitle(string title)
        {
            Title = title;
            return this;
        }

        public GuiWindow BindTitle(string bindName)
        {
            TitleBindName = bindName;
            return this;
        }

        public GuiWindow SetGeometry(float x, float y, float width, float height)
        {
            Geometry = new GuiRectangle(x, y, width, height);
            return this;
        }

        public GuiWindow SetGeometry(GuiRectangle bounds)
        {
            Geometry = bounds;
            return this;
        }

        public GuiWindow BindGeometry(string bindName)
        {
            GeometryBindName = bindName;
            return this;
        }

        public GuiWindow SetIsResizable(bool isResizable)
        {
            IsResizable = isResizable;
            return this;
        }

        public GuiWindow BindIsResizable(string bindName)
        {
            IsResizableBindName = bindName;
            return this;
        }

        public GuiWindow SetIsCollapsed(bool isCollapsed)
        {
            IsCollapsed = isCollapsed;
            return this;
        }

        public GuiWindow BindIsCollapsed(string bindName)
        {
            IsCollapsedBindName = bindName;
            return this;
        }

        public GuiWindow SetIsClosable(bool isClosable)
        {
            IsClosable = isClosable;
            return this;
        }

        public GuiWindow BindIsClosable(string bindName)
        {
            IsClosableBindName = bindName;
            return this;
        }

        public GuiWindow SetIsTransparent(bool isTransparent)
        {
            IsTransparent = isTransparent;
            return this;
        }

        public GuiWindow BindIsTransparent(string bindName)
        {
            IsTransparentBindName = bindName;
            return this;
        }

        public GuiWindow SetShowBorder(bool showBorder)
        {
            ShowBorder = showBorder;
            return this;
        }

        public GuiWindow BindShowBorder(string bindName)
        {
            ShowBorderBindName = bindName;
            return this;
        }

        public GuiWindow AddColumn(Action<GuiColumn> col)
        {
            var column = new GuiColumn();
            Columns.Add(column);
            col(column);

            return this;
        }

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

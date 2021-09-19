using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiWindow { }

    public class GuiWindow<T> : GuiWindow
        where T: IGuiViewModel
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

        public List<GuiColumn<T>> Columns { get; }


        public GuiWindow<T> SetTitle(string title)
        {
            Title = title;
            return this;
        }

        public GuiWindow<T> BindTitle<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TitleBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> SetGeometry(float x, float y, float width, float height)
        {
            Geometry = new GuiRectangle(x, y, width, height);
            return this;
        }

        public GuiWindow<T> SetGeometry(GuiRectangle bounds)
        {
            Geometry = bounds;
            return this;
        }

        public GuiWindow<T> BindGeometry<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            GeometryBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> SetIsResizable(bool isResizable)
        {
            IsResizable = isResizable;
            return this;
        }

        public GuiWindow<T> BindIsResizable<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsResizableBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> SetIsCollapsed(bool isCollapsed)
        {
            IsCollapsed = isCollapsed;
            return this;
        }

        public GuiWindow<T> BindIsCollapsed<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsCollapsedBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> SetIsClosable(bool isClosable)
        {
            IsClosable = isClosable;
            return this;
        }

        public GuiWindow<T> BindIsClosable<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsClosableBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> SetIsTransparent(bool isTransparent)
        {
            IsTransparent = isTransparent;
            return this;
        }

        public GuiWindow<T> BindIsTransparent<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsTransparentBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> SetShowBorder(bool showBorder)
        {
            ShowBorder = showBorder;
            return this;
        }

        public GuiWindow<T> BindShowBorder<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ShowBorderBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow<T> AddColumn(Action<GuiColumn<T>> col)
        {
            var column = new GuiColumn<T>();
            Columns.Add(column);
            col(column);

            return this;
        }

        public MethodInfo OpenedEventMethodInfo { get; private set; }
        public MethodInfo ClosedEventMethodInfo { get; private set; }

        public GuiWindow<T> BindOnOpened<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            OpenedEventMethodInfo = GuiHelper<T>.GetMethodInfo(expression);
            return this;
        }

        public GuiWindow<T> BindOnClosed<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            ClosedEventMethodInfo = GuiHelper<T>.GetMethodInfo(expression);
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

            Columns = new List<GuiColumn<T>>();
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

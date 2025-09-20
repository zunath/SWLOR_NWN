using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiWindow { }

    public class GuiWindow<T> : GuiWindow
        where T: IGuiViewModel
    {
        private string Title { get; set; }
        private string TitleBindName { get; set; }
        private bool IsTitleBound => !string.IsNullOrWhiteSpace(TitleBindName);
        
        public GuiRectangle Geometry { get; private set; }
        private string GeometryBindName { get; set; } = "Geometry";

        private bool IsResizable { get; set; }
        private string IsResizableBindName { get; set; }
        private bool IsResizableBound => !string.IsNullOrWhiteSpace(IsResizableBindName);
        
        private bool IsCollapsible { get; set; }
        private string IsCollapsibleBindName { get; set; }
        private bool IsCollapsibleBound => !string.IsNullOrWhiteSpace(IsCollapsibleBindName);
        
        private bool IsClosable { get; set; }
        private string IsClosableBindName { get; set; }
        private bool IsClosableBound => !string.IsNullOrWhiteSpace(IsClosableBindName);
        
        private bool IsTransparent { get; set; }
        private string IsTransparentBindName { get; set; }
        private bool IsTransparentBound => !string.IsNullOrWhiteSpace(IsTransparentBindName);
        
        private bool ShowBorder { get; set; }
        private string ShowBorderBindName { get; set; }
        private bool IsShowBorderBound => !string.IsNullOrWhiteSpace(ShowBorderBindName);

        private bool AcceptsInput { get; set; } = true;
        private string AcceptsInputBindName { get; set; }
        private bool IsAcceptsInputBound => !string.IsNullOrWhiteSpace(AcceptsInputBindName);

        public Dictionary<string, IGuiWidget> PartialViews { get; }
        public List<IGuiWidget> Elements { get; }


        /// <summary>
        /// Sets a static value for the title of the window.
        /// </summary>
        /// <param name="title">The title to set.</param>
        public GuiWindow<T> SetTitle(string title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the title of the window.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindTitle<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TitleBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets the initial geometry of the window.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public GuiWindow<T> SetInitialGeometry(float x, float y, float width, float height)
        {
            Geometry = new GuiRectangle(x, y, width, height);
            return this;
        }

        /// <summary>
        /// Sets the initial geometry of the window.
        /// </summary>
        /// <param name="bounds">The X, Y, Width, and Height of the window.</param>
        public GuiWindow<T> SetInitialGeometry(GuiRectangle bounds)
        {
            Geometry = bounds;
            return this;
        }
        
        /// <summary>
        /// Sets a static value for whether the window can be resized by the user.
        /// </summary>
        /// <param name="isResizable">true if resizable, false otherwise</param>
        public GuiWindow<T> SetIsResizable(bool isResizable)
        {
            IsResizable = isResizable;
            return this;
        }

        /// <summary>
        /// Binds a static value for whether the window can be resized by the user.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindIsResizable<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsResizableBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the window may be collapsed by the user.
        /// </summary>
        /// <param name="isCollapsible">true if the window can be collapsed, false otherwise</param>
        public GuiWindow<T> SetIsCollapsible(bool isCollapsible)
        {
            IsCollapsible = isCollapsible;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for whether the window may be collapsed by the user.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindIsCollapsed<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsCollapsibleBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the window may be closed by the user.
        /// </summary>
        /// <param name="isClosable">true if the window can be closed, false otherwise</param>
        public GuiWindow<T> SetIsClosable(bool isClosable)
        {
            IsClosable = isClosable;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for whether the window can be closed by the user.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindIsClosable<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsClosableBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the window is transparent.
        /// </summary>
        /// <param name="isTransparent">true if transparent, false otherwise</param>
        public GuiWindow<T> SetIsTransparent(bool isTransparent)
        {
            IsTransparent = isTransparent;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for whether the window is transparent.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindIsTransparent<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsTransparentBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the window's border will be shown.
        /// </summary>
        /// <param name="showBorder">true if the border is shown, false otherwise</param>
        public GuiWindow<T> SetShowBorder(bool showBorder)
        {
            ShowBorder = showBorder;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for whether the window's border will be shown.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindShowBorder<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ShowBorderBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Adds a partial view to the window. This will only define the partial view.
        /// You need to call ChangePartialView from within the view model for it to actually change.
        /// </summary>
        /// <param name="name">The name to associate to the partial view. This must be unique within the window.</param>
        /// <param name="view">The structure of the view.</param>
        public GuiWindow<T> DefinePartialView(string name, Action<GuiGroup<T>> view)
        {
            var group = new GuiGroup<T>();
            group.SetScrollbars(NuiScrollbars.None);
            group.SetShowBorder(false);
            PartialViews[name] = group;
            view(group);

            return this;
        }

        /// <summary>
        /// Adds a column to the layout.
        /// </summary>
        /// <param name="col">The new column's definition.</param>
        public GuiWindow<T> AddColumn(Action<GuiColumn<T>> col)
        {
            var column = new GuiColumn<T>();
            Elements.Add(column);
            col(column);

            return this;
        }

        /// <summary>
        /// Adds a row to the layout.
        /// </summary>
        /// <param name="row">The new row's definition.</param>
        public GuiWindow<T> AddRow(Action<GuiRow<T>> row)
        {
            var newRow = new GuiRow<T>();
            Elements.Add(newRow);
            row(newRow);

            return this;
        }
        
        public GuiMethodDetail OpenedEventMethodInfo { get; private set; }
        public GuiMethodDetail ClosedEventMethodInfo { get; private set; }

        /// <summary>
        /// Binds an action to the Open event of the window.
        /// Fires when the window is opened.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public GuiWindow<T> BindOnOpened<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            OpenedEventMethodInfo = GuiHelper<T>.GetMethodInfo(expression);
            return this;
        }

        /// <summary>
        /// Binds an action to the Closed event of the window.
        /// Fires when the window is closed.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public GuiWindow<T> BindOnClosed<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            ClosedEventMethodInfo = GuiHelper<T>.GetMethodInfo(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the window accepts input from the user.
        /// </summary>
        /// <param name="acceptsInput">true if the window accepts input, false otherwise</param>
        public GuiWindow<T> SetAcceptsInput(bool acceptsInput)
        {
            AcceptsInput = acceptsInput;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for whether the window accepts input from the user.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiWindow<T> BindAcceptsInput<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AcceptsInputBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiWindow()
        {
            Title = "New Window";
            Geometry = new GuiRectangle(0f, 0f, 400f, 400f);
            IsResizable = true;
            IsCollapsible = false;
            IsClosable = true;
            IsTransparent = false;
            ShowBorder = true;

            PartialViews = new Dictionary<string, IGuiWidget>();
            Elements = new List<IGuiWidget>();
        }

        /// <summary>
        /// Builds the Window element.
        /// </summary>
        /// <returns>Json representing the window element.</returns>
        public Json Build()
        {
            var root = JsonArray();

            foreach (var column in Elements)
            {
                root = JsonArrayInsert(root, column.ToJson());
            }

            root = Nui.Row(root);

            var title = IsTitleBound ? Nui.Bind(TitleBindName) : Title == null ? JsonBool(false) : JsonString(Title);
            var geometry = Nui.Bind(GeometryBindName);
            var isResizable = IsResizableBound ? Nui.Bind(IsResizableBindName) : JsonBool(IsResizable);
            var isCollapsible = IsCollapsibleBound ? Nui.Bind(IsCollapsibleBindName) : (IsCollapsible ? JsonNull() : JsonBool(false));
            var isClosable = IsClosableBound ? Nui.Bind(IsClosableBindName) : JsonBool(IsClosable);
            var isTransparent = IsTransparentBound ? Nui.Bind(IsTransparentBindName) : JsonBool(IsTransparent);
            var showBorder = IsShowBorderBound ? Nui.Bind(ShowBorderBindName) : JsonBool(ShowBorder);
            var acceptsInput = IsAcceptsInputBound ? Nui.Bind(AcceptsInputBindName) : JsonBool(AcceptsInput);

            return Nui.Window(root, title, geometry, isResizable, isCollapsible, isClosable, isTransparent, showBorder, acceptsInput);
        }
    }
}

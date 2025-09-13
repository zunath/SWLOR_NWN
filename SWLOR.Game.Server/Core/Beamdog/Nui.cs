using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core.Beamdog
{
    public static class Nui
    {
        /// <summary>
        /// Window
        /// Special cases:
        /// * Set the window title to JsonBool(FALSE), Collapse to JsonBool(FALSE) and bClosable to FALSE
        ///   to hide the title bar.
        ///   Note: You MUST provide a way to close the window some other way, or the user will be stuck with it.
        /// </summary>
        /// <param name="jRoot">Layout-ish (NuiRow, NuiCol, NuiGroup)</param>
        /// <param name="jTitle">Bind:String</param>
        /// <param name="jGeometry">
        /// Bind:Rect
        /// Set x and/or y to -1.0 to center the window on that axis
        /// Set x and/or y to -2.0 to position the window's top left at the mouse cursor's position of that axis
        /// Set x and/or y to -3.0 to center the window on the mouse cursor's position of that axis
        /// </param>
        /// <param name="jResizable">
        /// Bind:Bool
        /// Set to JsonBool(TRUE) or JsonNull() to let user resize without binding.</param>
        /// <param name="jCollapsed">
        /// Bind:Bool
        /// Set to a static value JsonBool(FALSE) to disable collapsing.Set to JsonNull() to let user collapse without binding.
        /// For better UX, leave collapsing on.</param>
        /// <param name="jClosable">
        /// Bind:Bool
        /// You must provide a way to close the window if you set this to FALSE.
        /// For better UX, handle the window "closed" event.</param>
        /// <param name="jTransparent">
        /// Bind:Bool
        /// Do not render background</param>
        /// <param name="jBorder">
        /// Bind:Bool
        /// Do not render border</param>
        /// <param name="jAcceptsInput">
        /// Bind:Bool        Set JsonBool(FALSE) to disable all input.
        /// All hover, clicks and keypresses will fall through.
        /// </param>
        public static Json Window(
            Json jRoot,
            Json jTitle,
            Json jGeometry,
            Json jResizable,
            Json jCollapsed,
            Json jClosable,
            Json jTransparent,
            Json jBorder,
            Json jAcceptsInput
        )
        {
            var ret = JsonObject();
            // Currently hardcoded and here to catch backwards-incompatible data in the future.
            ret = JsonObjectSet(ret, "version", JsonInt(1));
            ret = JsonObjectSet(ret, "title", jTitle);
            ret = JsonObjectSet(ret, "root", jRoot);
            ret = JsonObjectSet(ret, "geometry", jGeometry);
            ret = JsonObjectSet(ret, "resizable", jResizable);
            ret = JsonObjectSet(ret, "collapsed", jCollapsed);
            ret = JsonObjectSet(ret, "closable", jClosable);
            ret = JsonObjectSet(ret, "transparent", jTransparent);
            ret = JsonObjectSet(ret, "border", jBorder);
            ret = JsonObjectSet(ret, "accepts_input", jAcceptsInput);
            return ret;
        }
        
        private static Json NuiElement(
          string sType,
          Json jLabel,
          Json jValue
        )
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "type", JsonString(sType));
            ret = JsonObjectSet(ret, "label", jLabel);
            ret = JsonObjectSet(ret, "value", jValue);
            return ret;
        }

        /// <summary>
        /// Create a dynamic bind. Unlike static values, these can change at runtime:
        ///    NuiBind("mybindlabel");
        ///    NuiSetBind(.., "mybindlabel", JsonString("hi"));
        /// To create static values, just use the Json types directly:
        ///    JsonString("hi");
        /// </summary>
        public static Json Bind(string sId)
        {
            return JsonObjectSet(JsonObject(), "bind", JsonString(sId));
        }

        /// <summary>
        /// Tag the given element with a id.
        /// Only tagged elements will send events to the server.
        /// </summary>
        public static Json Id(Json jElem, string sId)
        {
            return JsonObjectSet(jElem, "id", JsonString(sId));
        }

        /// <summary>
        /// A shim/helper that can be used to render or bind a strref where otherwise
        /// a string value would go.
        /// </summary>
        public static Json StrRef(int nStrRef)
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "strref", JsonInt(nStrRef));
            return ret;
        }

        /// <summary>
        /// A column will auto-space all elements inside of it and advise the parent about it's desired size.
        /// </summary>
        public static Json Column(Json jList)
        {
            return JsonObjectSet(NuiElement("col", JsonNull(), JsonNull()), "children", jList);
        }

        /// <summary>
        /// A row will auto-space all elements inside of it and advise the parent
        /// about it's desired size.
        /// </summary>
        public static Json Row(Json jList)
        {
            return JsonObjectSet(NuiElement("row", JsonNull(), JsonNull()), "children", jList);
        }

        /// <summary>
        /// A group, usually with a border and some padding, holding a single element. Can scroll.
        /// Will not advise parent of size, so you need to let it fill a span (col/row) as if it was a element.
        /// </summary>
        public static Json Group(Json jChild, bool bBorder = true, NuiScrollbars nScroll = NuiScrollbars.Auto)
        {
            var ret = NuiElement("group", JsonNull(), JsonNull());
            ret = JsonObjectSet(ret, "children", JsonArrayInsert(JsonArray(), jChild));
            ret = JsonObjectSet(ret, "border", JsonBool(bBorder));
            ret = JsonObjectSet(ret, "scrollbars", JsonInt((int)nScroll));
            return ret;
        }

        // Modifiers/Attributes: These are all static and cannot be bound, since the UI system
        // cannot easily reflow once the layout is set up. You need to swap the layout if you
        // want to change element geometry.

        /// <summary>
        /// Defines a specific width for a Nui element.
        /// </summary>
        public static Json Width(Json jElem, float fWidth)
        {
            return JsonObjectSet(jElem, "width", JsonFloat(fWidth));
        }

        /// <summary>
        /// Defines a specific height for a Nui element.
        /// </summary>
        public static Json Height(Json jElem, float fHeight)
        {
            return JsonObjectSet(jElem, "height", JsonFloat(fHeight));
        }

        /// <summary>
        /// Defines an aspect ratio for a Nui element.
        /// </summary>
        public static Json Aspect(Json jElem, float fAspect)
        {
            return JsonObjectSet(jElem, "aspect", JsonFloat(fAspect));
        }

        /// <summary>
        /// Set a margin on the widget. The margin is the spacing outside of the widget.
        /// </summary>
        public static Json Margin(Json jElem, float fMargin)
        {
            return JsonObjectSet(jElem, "margin", JsonFloat(fMargin));
        }

        /// <summary>
        /// Set padding on the widget. The margin is the spacing inside of the widget.
        /// </summary>
        public static Json Padding(Json jElem, float fPadding)
        {
            return JsonObjectSet(jElem, "padding", JsonFloat(fPadding));
        }

        /// <summary>
        /// Disabled elements are non-interactive and greyed out.
        /// </summary>
        public static Json Enabled(Json jElem, Json jEnabler)
        {
            return JsonObjectSet(jElem, "enabled", jEnabler);
        }

        /// <summary>
        /// Invisible elements do not render at all, but still take up layout space.
        /// </summary>
        public static Json Visible(Json jElem, Json jVisible)
        {
            return JsonObjectSet(jElem, "visible", jVisible);
        }

        /// <summary>
        /// Tooltips show on mouse hover.
        /// </summary>
        public static Json Tooltip(Json jElem, Json jTooltip)
        {
            return JsonObjectSet(jElem, "tooltip", jTooltip);
        }

        /// <summary>
        /// Tooltips for disabled elements show on mouse hover.
        /// </summary>
        public static Json DisabledTooltip(Json jElem, Json jTooltip)
        {
            return JsonObjectSet(jElem, "disabled_tooltip", jTooltip);
        }
        
        /// <summary>
        /// Encouraged elements have a breathing animated glow inside of it. 
        /// </summary>
        public static Json Encouraged(Json jElem, Json jEncouraged)
        {
            return JsonObjectSet(jElem, "encouraged", jEncouraged);
        }

        public static Json Vec(float x, float y)
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "x", JsonFloat(x));
            ret = JsonObjectSet(ret, "y", JsonFloat(y));
            return ret;
        }

        public static Json Rect(float x, float y, float w, float h)
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "x", JsonFloat(x));
            ret = JsonObjectSet(ret, "y", JsonFloat(y));
            ret = JsonObjectSet(ret, "w", JsonFloat(w));
            ret = JsonObjectSet(ret, "h", JsonFloat(h));
            return ret;
        }

        public static Json Color(int r, int g, int b, int a = 255)
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "r", JsonInt(r));
            ret = JsonObjectSet(ret, "g", JsonInt(g));
            ret = JsonObjectSet(ret, "b", JsonInt(b));
            ret = JsonObjectSet(ret, "a", JsonInt(a));
            return ret;
        }

        /// <summary>
        /// Style the foreground color of the widget. This is dependent on the widget
        /// in question and only supports solid/full colors right now (no texture skinning).
        /// For example, labels would style their text color; progress bars would style the bar.
        /// </summary>
        /// <param name="jElem">Element</param>
        /// <param name="jColor">Bind:Color</param>
        public static Json StyleForegroundColor(Json jElem, Json jColor)
        {
            return JsonObjectSet(jElem, "foreground_color", jColor);
        }

        /// <summary>
        /// A special widget that just takes up layout space.
        /// If you add multiple spacers to a span, they will try to size equally.
        ///  e.g.: [ <spacer> <button|w=50> <spacer> ] will try to center the button.
        /// </summary>
        public static Json Spacer()
        {
            return NuiElement("spacer", JsonNull(), JsonNull());
        }

        /// <summary>
        /// Create a label field. Labels are single-line stylable non-editable text fields.
        /// </summary>
        /// <param name="jValue">Bind:String</param>
        /// <param name="jHAlign">Bind:Int:NUI_HALIGN_*</param>
        /// <param name="jVAlign">BIND:Int:NUI_VALIGN_*</param>
        public static Json Label(Json jValue, Json jHAlign, Json jVAlign)
        {
            var ret = NuiElement("label", JsonNull(), jValue);
            ret = JsonObjectSet(ret, "text_halign", jHAlign);
            ret = JsonObjectSet(ret, "text_valign", jVAlign);
            return ret;
        }

        /// <summary>
        /// Create a non-editable text field: This element can do multiple lines, has a skinned
        /// border and a scrollbar if needed.
        /// </summary>
        /// <param name="jValue">Bind:String</param>
        /// <param name="showBorder">bool</param>
        /// <param name="scrollbars">int</param>
        public static Json Text(Json jValue, bool showBorder = true, NuiScrollbars scrollbars = NuiScrollbars.Auto)
        {
            var ret = NuiElement("text", JsonNull(), jValue);
            ret = JsonObjectSet(ret, "border", JsonBool(showBorder));
            ret = JsonObjectSet(ret, "scrollbars", JsonInt((int)scrollbars));

            return ret;
        }

        /// <summary>
        /// A clickable button with text as the label.
        /// Sends "click" events on click.
        /// </summary>
        /// <param name="jLabel">Bind:String</param>
        public static Json Button(Json jLabel)
        {
            return NuiElement("button", jLabel, JsonNull());
        }

        /// <summary>
        /// A clickable button with an image as the label.
        /// Sends "click" events on click.
        /// </summary>
        /// <param name="jResRef">Bind:ResRef</param>
        public static Json ButtonImage(Json jResRef)
        {
            return NuiElement("button_image", jResRef, JsonNull());
        }

        /// <summary>
        /// A clickable button with text as the label.
        /// Same as the normal button, but this one is a toggle.
        /// Sends "click" events on click.
        /// </summary>
        /// <param name="jLabel">Bind:String</param>
        /// <param name="jValue">Bind:Bool</param>
        public static Json ButtonSelect(Json jLabel, Json jValue)
        {
            return NuiElement("button_select", jLabel, jValue);
        }

        /// <summary>
        /// A checkbox with a label to the right of it.
        /// </summary>
        /// <param name="jLabel">Bind:String</param>
        /// <param name="jBool">Bind:Bool</param>
        public static Json Check(Json jLabel, Json jBool)
        {
            return NuiElement("check", jLabel, jBool);
        }

        /// <summary>
        /// An image, with no border or padding.
        /// </summary>
        /// <param name="jResRef">Bind:ResRef</param>
        /// <param name="jAspect">Bind:Int:NUI_ASPECT_*</param>
        /// <param name="jHAlign">Bind:Int:NUI_HALIGN_*</param>
        /// <param name="jVAlign">Bind:Int:NUI_VALIGN_*</param>
        public static Json Image(Json jResRef, Json jAspect, Json jHAlign, Json jVAlign)
        {
            var img = NuiElement("image", JsonNull(), jResRef);
            img = JsonObjectSet(img, "image_aspect", jAspect);
            img = JsonObjectSet(img, "image_halign", jHAlign);
            img = JsonObjectSet(img, "image_valign", jVAlign);
            return img;
        }

        /// <summary>
        /// Optionally render only subregion of jImage.
        /// jRegion is a NuiRect (x, y, w, h) to indicate the render region inside the image.
        /// </summary>
        /// <param name="jImage">NuiImage</param>
        /// <param name="jRegion">Bind:NuiRect</param>
        public static Json ImageRegion(Json jImage, Json jRegion)
        {
            return JsonObjectSet(jImage, "image_region", jRegion);
        }

        /// <summary>
        /// A combobox/dropdown.
        /// </summary>
        /// <param name="jElements">Bind:ComboEntry[]</param>
        /// <param name="jSelected">Bind:Int (index into jElements)</param>
        public static Json Combo(Json jElements, Json jSelected)
        {
            return JsonObjectSet(NuiElement("combo", JsonNull(), jSelected), "elements", jElements);
        }

        /// <summary>
        /// A combo box entry.
        /// </summary>
        public static Json ComboEntry(string sLabel, int nValue)
        {
            return JsonArrayInsert(JsonArrayInsert(JsonArray(), JsonString(sLabel)), JsonInt(nValue));
        }

        /// <summary>
        /// A floating-point slider. A good step size for normal-sized sliders is 0.01.
        /// </summary>
        /// <param name="jValue">Bind:Float</param>
        /// <param name="jMin">Bind:Float</param>
        /// <param name="jMax">Bind:Float</param>
        /// <param name="jStepSize">Bind:Float</param>
        public static Json SliderFloat(Json jValue, Json jMin, Json jMax, Json jStepSize)
        {
            var ret = NuiElement("sliderf", JsonNull(), jValue);
            ret = JsonObjectSet(ret, "min", jMin);
            ret = JsonObjectSet(ret, "max", jMax);
            ret = JsonObjectSet(ret, "step", jStepSize);
            return ret;
        }

        /// <summary>
        /// An integer/discrete slider.
        /// </summary>
        /// <param name="jValue">Bind:Int</param>
        /// <param name="jMin">Bind:Int</param>
        /// <param name="jMax">Bind:Int</param>
        /// <param name="jStepSize">Bind:Int</param>
        public static Json Slider(Json jValue, Json jMin, Json jMax, Json jStepSize)
        {
            var ret = NuiElement("slider", JsonNull(), jValue);
            ret = JsonObjectSet(ret, "min", jMin);
            ret = JsonObjectSet(ret, "max", jMax);
            ret = JsonObjectSet(ret, "step", jStepSize);
            return ret;
        }

        /// <summary>
        /// A progress bar. Progress is always from 0.0 to 1.0.
        /// </summary>
        /// <param name="jValue">Bind:Float (0.0->1.0)</param>
        /// <returns></returns>
        public static Json Progress(Json jValue)
        {
            return NuiElement("progress", JsonNull(), jValue);
        }

        /// <summary>
        /// An editable text field.
        /// </summary>
        /// <param name="jPlaceholder">Bind:String</param>
        /// <param name="jValue">Bind:String</param>
        /// <param name="nMaxLength">UInt >= 1, <= 65535</param>
        /// <param name="bMultiline">Bool</param>
        /// <param name="bWordWrap">Bool</param>
        public static Json TextEdit(Json jPlaceholder, Json jValue, int nMaxLength, bool bMultiline, bool bWordWrap = true)
        {
            var ret = NuiElement("textedit", jPlaceholder, jValue);
            ret = JsonObjectSet(ret, "max", JsonInt(nMaxLength));
            ret = JsonObjectSet(ret, "multiline", JsonBool(bMultiline));
            ret = JsonObjectSet(ret, "wordwrap", JsonBool(bWordWrap));
            return ret;
        }

        /// <summary>
        /// Creates a list view of elements.
        /// jTemplate needs to be an array of NuiListTemplateCell instances.
        /// All binds referenced in jTemplate should be arrays of rRowCount size;
        /// e.g. when rendering a NuiLabel(), the bound label String should be an array of strings.
        /// You can pass in one of the template jRowCount into jSize as a convenience. The array
        /// size will be uses as the Int bind.
        /// jRowHeight defines the height of the rendered rows.
        /// </summary>
        /// <param name="jTemplate">NuiListTemplateCell[]</param>
        /// <param name="jRowCount">Bind:Int</param>
        /// <param name="fRowHeight">Height of the row</param>
        /// <param name="showBorder">true to show the border, false otherwise</param>
        /// <param name="scrollbars">The type of scroll bars to use, if any.</param>
        public static Json List(
            Json jTemplate, 
            Json jRowCount, 
            float fRowHeight = NuiStyle.RowHeight,
            bool showBorder = true,
            NuiScrollbars scrollbars = NuiScrollbars.Y)
        {
            var ret = NuiElement("list", JsonNull(), JsonNull());
            ret = JsonObjectSet(ret, "row_template", jTemplate);
            ret = JsonObjectSet(ret, "row_count", jRowCount);
            ret = JsonObjectSet(ret, "row_height", JsonFloat(fRowHeight));
            ret = JsonObjectSet(ret, "border", JsonBool(showBorder));
            ret = JsonObjectSet(ret, "scrollbars", JsonInt((int)scrollbars));

            return ret;
        }

        /// <summary>
        /// Builds a template cell for use in a list.
        /// </summary>
        /// <param name="jElem">Element</param>
        /// <param name="fWidth">Float:0 = auto, >1 = pixel width</param>
        /// <param name="bVariable">Bool:Cell can grow if space is available; otherwise static</param>
        public static Json ListTemplateCell(Json jElem, float fWidth, bool bVariable)
        {
            var ret = JsonArray();
            ret = JsonArrayInsert(ret, jElem);
            ret = JsonArrayInsert(ret, JsonFloat(fWidth));
            ret = JsonArrayInsert(ret, JsonBool(bVariable));
            return ret;
        }

        /// <summary>
        /// A simple color picker, with no border or spacing.
        /// </summary>
        /// <param name="jColor">Bind:Color</param>
        public static Json ColorPicker(Json jColor)
        {
            var ret = NuiElement("color_picker", JsonNull(), jColor);
            return ret;
        }

        /// <summary>
        /// A list of options (radio buttons). Only one can be selected
        /// at a time. jValue is updated every time a different element is
        /// selected. The special value -1 means "nothing".
        /// </summary>
        /// <param name="nDirection">NUI_DIRECTION_*</param>
        /// <param name="jElements">JsonArray of string labels</param>
        /// <param name="jValue">Bind:UInt</param>
        public static Json Options(NuiDirection nDirection, Json jElements, Json jValue)
        {
            var ret = NuiElement("options", JsonNull(), jValue);
            ret = JsonObjectSet(ret, "direction", JsonInt((int)nDirection));
            ret = JsonObjectSet(ret, "elements", jElements);
            return ret;
        }

        /// <summary>
        /// A group of buttons.  Only one can be selected at a time.  jValue
        /// is updated every time a different button is selected.  The special
        /// value -1 means "nothing".
        /// </summary>
        public static Json Toggles(NuiDirection nDirection, Json jElements, Json jValue)
        {
            var ret = NuiElement("tabbar", JsonNull(), jValue);
            ret = JsonObjectSet(ret, "direction", JsonInt((int)nDirection));
            ret = JsonObjectSet(ret, "elements", jElements);
            return ret;
        }

        /// <summary>
        /// Creates a slot for a chart.
        /// </summary>
        /// <param name="nType">Int:NUI_CHART_TYPE_*</param>
        /// <param name="jLegend">Bind:String</param>
        /// <param name="jColor">Bind:NuiColor</param>
        /// <param name="jData">Bind:Float[]</param>
        public static Json ChartSlot(NuiChartType nType, Json jLegend, Json jColor, Json jData)
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "type", JsonInt((int)nType));
            ret = JsonObjectSet(ret, "legend", jLegend);
            ret = JsonObjectSet(ret, "color", jColor);
            ret = JsonObjectSet(ret, "data", jData);
            return ret;
        }

        /// <summary>
        /// Renders a chart.
        /// Currently, min and max values are determined automatically and
        /// cannot be influenced.
        /// </summary>
        /// <param name="jSlots">NuiChartSlot[]</param>
        public static Json Chart(Json jSlots)
        {
            var ret = NuiElement("chart", JsonNull(), jSlots);
            return ret;
        }
        
        private static Json NuiDrawListItem(
            NuiDrawListItemType nType, 
            Json jEnabled, 
            Json jColor, 
            Json jFill, 
            Json jLineThickness,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = JsonObject();
            ret = JsonObjectSet(ret, "type", JsonInt((int)nType));
            ret = JsonObjectSet(ret, "enabled", jEnabled);
            ret = JsonObjectSet(ret, "color", jColor);
            ret = JsonObjectSet(ret, "fill", jFill);
            ret = JsonObjectSet(ret, "line_thickness", jLineThickness);
            ret = JsonObjectSet(ret, "order", JsonInt((int)nOrder));
            ret = JsonObjectSet(ret, "render", JsonInt((int)nRender));
            return ret;
        }

        /// <summary>
        /// Draws a poly line on a chart.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jColor">Bind:Color</param>
        /// <param name="jFill">Bind:Bool</param>
        /// <param name="jLineThickness">Bind:Float</param>
        /// <param name="jPoints">Bind:Float[]    Always provide points in pairs</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json DrawListPolyLine(
            Json jEnabled, 
            Json jColor, 
            Json jFill, 
            Json jLineThickness, 
            Json jPoints,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = NuiDrawListItem(NuiDrawListItemType.PolyLine, jEnabled, jColor, jFill, jLineThickness, nOrder, nRender);
            ret = JsonObjectSet(ret, "points", jPoints);
            return ret;
        }

        /// <summary>
        /// Draws a curve line on a chart.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jColor">Bind:Color</param>
        /// <param name="jLineThickness">Bind:Float</param>
        /// <param name="jA">Bind:Vec2</param>
        /// <param name="jB">Bind:Vec2</param>
        /// <param name="jCtrl0">Bind:Vec2</param>
        /// <param name="jCtrl1">Bind:Vec2</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json DrawListCurve(
            Json jEnabled, 
            Json jColor, 
            Json jLineThickness, 
            Json jA, 
            Json jB, 
            Json jCtrl0, 
            Json jCtrl1,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = NuiDrawListItem(NuiDrawListItemType.Curve, jEnabled, jColor, JsonBool(false), jLineThickness, nOrder, nRender);
            ret = JsonObjectSet(ret, "a", jA);
            ret = JsonObjectSet(ret, "b", jB);
            ret = JsonObjectSet(ret, "ctrl0", jCtrl0);
            ret = JsonObjectSet(ret, "ctrl1", jCtrl1);
            return ret;
        }

        /// <summary>
        /// Draws a circle on a chart.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jColor">Bind:Color</param>
        /// <param name="jFill">Bind:Bool</param>
        /// <param name="jLineThickness">Bind:Float</param>
        /// <param name="jRect">Bind:Rect</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json DrawListCircle(
            Json jEnabled, 
            Json jColor, 
            Json jFill, 
            Json jLineThickness, 
            Json jRect,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = NuiDrawListItem(NuiDrawListItemType.Circle, jEnabled, jColor, jFill, jLineThickness, nOrder, nRender);
            ret = JsonObjectSet(ret, "rect", jRect);
            return ret;
        }

        /// <summary>
        /// Draws an arc on a chart.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jColor">Bind:Color</param>
        /// <param name="jFill">Bind:Bool</param>
        /// <param name="jLineThickness">Bind:Float</param>
        /// <param name="jCenter">Bind:Vec2</param>
        /// <param name="jRadius">Bind:Float</param>
        /// <param name="jAMin">Bind:Float</param>
        /// <param name="jAMax">Bind:Float</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json DrawListArc(
            Json jEnabled, 
            Json jColor, 
            Json jFill, 
            Json jLineThickness, 
            Json jCenter, 
            Json jRadius, 
            Json jAMin, 
            Json jAMax,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = NuiDrawListItem(NuiDrawListItemType.Arc, jEnabled, jColor, jFill, jLineThickness, nOrder, nRender);
            ret = JsonObjectSet(ret, "c", jCenter);
            ret = JsonObjectSet(ret, "radius", jRadius);
            ret = JsonObjectSet(ret, "amin", jAMin);
            ret = JsonObjectSet(ret, "amax", jAMax);
            return ret;
        }

        /// <summary>
        /// Draws text for a list.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jColor">Bind:Color</param>
        /// <param name="jRect">Bind:Rect</param>
        /// <param name="jText">Bind:String</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json DrawListText(
            Json jEnabled, 
            Json jColor, 
            Json jRect, 
            Json jText,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = NuiDrawListItem(NuiDrawListItemType.Text, jEnabled, jColor, JsonNull(), JsonNull(), nOrder, nRender);
            ret = JsonObjectSet(ret, "rect", jRect);
            ret = JsonObjectSet(ret, "text", jText);
            return ret;
        }

        /// <summary>
        /// Draws an image for a list.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jResRef">Bind:ResRef</param>
        /// <param name="jRect">Bind:Rect</param>
        /// <param name="jAspect">Bind:Int:NUI_ASPECT_*</param>
        /// <param name="jHAlign">Bind:Int:NUI_HALIGN_*</param>
        /// <param name="jVAlign">Bind:Int:NUI_VALIGN_*</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json DrawListImage(
            Json jEnabled, 
            Json jResRef, 
            Json jRect, 
            Json jAspect, 
            Json jHAlign, 
            Json jVAlign,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always)
        {
            var ret = NuiDrawListItem(NuiDrawListItemType.Image, jEnabled, JsonNull(), JsonNull(), JsonNull(), nOrder, nRender);
            ret = JsonObjectSet(ret, "image", jResRef);
            ret = JsonObjectSet(ret, "rect", jRect);
            ret = JsonObjectSet(ret, "image_aspect", jAspect);
            ret = JsonObjectSet(ret, "image_halign", jHAlign);
            ret = JsonObjectSet(ret, "image_valign", jVAlign);
            return ret;
        }

        public static Json DrawListImageRegion(Json jDrawListImage, Json jRegion)
        {
            return JsonObjectSet(jDrawListImage, "image_region", jRegion);
        }

        /// <summary>
        /// Draws a line for a list.
        /// </summary>
        /// <param name="jEnabled">Bind:Bool</param>
        /// <param name="jColor">Bind:Color</param>
        /// <param name="jLineThickness">Bind:Float</param>
        /// <param name="jA">Bind:Vec2</param>
        /// <param name="jB">Bind:Vec2</param>
        /// <param name="nOrder">Int:NUI_DRAW_LIST_ITEM_ORDER_*</param>
        /// <param name="nRender">Int:NUI_DRAW_LIST_ITEM_RENDER_*</param>
        public static Json NuiDrawListLine(
            Json jEnabled,
            Json jColor,
            Json jLineThickness,
            Json jA,
            Json jB,
            NuiDrawListItemOrderType nOrder = NuiDrawListItemOrderType.After,
            NuiDrawListItemRenderType nRender = NuiDrawListItemRenderType.Always
            )
        {
            Json ret = NuiDrawListItem(NuiDrawListItemType.Line, jEnabled, jColor, JsonNull(), jLineThickness, nOrder, nRender);
            ret = JsonObjectSet(ret, "a", jA);
            ret = JsonObjectSet(ret, "b", jB);
            return ret;
        }

        /// <summary>
        /// Draws a list.
        /// </summary>
        /// <param name="jElem">Element</param>
        /// <param name="jScissor">Bind:Bool       Constrain painted elements to widget bounds.</param>
        /// <param name="jList">DrawListItem[]</param>
        public static Json DrawList(Json jElem, Json jScissor, Json jList)
        {
            var ret = JsonObjectSet(jElem, "draw_list", jList);
            ret = JsonObjectSet(ret, "draw_list_scissor", jScissor);
            return ret;
        }
    }
}

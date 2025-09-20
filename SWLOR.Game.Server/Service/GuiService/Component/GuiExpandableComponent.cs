using System;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiExpandableComponent<T>: GuiWidget<T, GuiExpandableComponent<T>>
        where T: IGuiViewModel
    {
        /// <summary>
        /// Adds a button.
        /// </summary>
        public GuiButton<T> AddButton()
        {
            var newButton = new GuiButton<T>();
            Elements.Add(newButton);

            return newButton;
        }

        /// <summary>
        /// Adds a button with an image.
        /// </summary>
        public GuiButtonImage<T> AddButtonImage()
        {
            var newButton = new GuiButtonImage<T>();
            Elements.Add(newButton);

            return newButton;
        }

        /// <summary>
        /// Adds a chart.
        /// </summary>
        public GuiChart<T> AddChart()
        {
            var newChart = new GuiChart<T>();
            Elements.Add(newChart);

            return newChart;
        }

        /// <summary>
        /// Adds a checkbox.
        /// </summary>
        public GuiCheckBox<T> AddCheckBox()
        {
            var newCheckBox = new GuiCheckBox<T>();
            Elements.Add(newCheckBox);

            return newCheckBox;
        }

        /// <summary>
        /// Adds a color picker.
        /// </summary>
        public GuiColorPicker<T> AddColorPicker()
        {
            var newColorPicker = new GuiColorPicker<T>();
            Elements.Add(newColorPicker);

            return newColorPicker;
        }

        /// <summary>
        /// Adds a combo box.
        /// </summary>
        public GuiComboBox<T> AddComboBox()
        {
            var newComboBox = new GuiComboBox<T>();
            Elements.Add(newComboBox);

            return newComboBox;
        }

        /// <summary>
        /// Adds an image.
        /// </summary>
        public GuiImage<T> AddImage()
        {
            var newImage = new GuiImage<T>();
            Elements.Add(newImage);

            return newImage;
        }

        /// <summary>
        /// Adds a label.
        /// </summary>
        /// <returns></returns>
        public GuiLabel<T> AddLabel()
        {
            var newLabel = new GuiLabel<T>();
            Elements.Add(newLabel);

            return newLabel;
        }

        /// <summary>
        /// Adds a set of options from which to choose.
        /// </summary>
        public GuiOptions<T> AddOptions()
        {
            var newOptions = new GuiOptions<T>();
            Elements.Add(newOptions);

            return newOptions;
        }

        /// <summary>
        /// Adds a progress bar.
        /// </summary>
        public GuiProgressBar<T> AddProgressBar()
        {
            var newProgressBar = new GuiProgressBar<T>();
            Elements.Add(newProgressBar);

            return newProgressBar;
        }

        /// <summary>
        /// Adds a slider with float values.
        /// </summary>
        public GuiSliderFloat<T> AddSliderFloat()
        {
            var newSliderFloat = new GuiSliderFloat<T>();
            Elements.Add(newSliderFloat);

            return newSliderFloat;
        }

        /// <summary>
        /// Adds a slider with integer values.
        /// </summary>
        public GuiSliderInt<T> AddSliderInt()
        {
            var newSliderInt = new GuiSliderInt<T>();
            Elements.Add(newSliderInt);

            return newSliderInt;
        }

        /// <summary>
        /// Adds empty space.
        /// </summary>
        public GuiSpacer<T> AddSpacer()
        {
            var newSpacer = new GuiSpacer<T>();
            Elements.Add(newSpacer);

            return newSpacer;
        }

        /// <summary>
        /// Adds text.
        /// </summary>
        public GuiText<T> AddText()
        {
            var newText = new GuiText<T>();
            Elements.Add(newText);

            return newText;
        }

        /// <summary>
        /// Adds an editable text box.
        /// </summary>
        public GuiTextEdit<T> AddTextEdit()
        {
            var newTextEdit = new GuiTextEdit<T>();
            Elements.Add(newTextEdit);

            return newTextEdit;
        }

        /// <summary>
        /// Adds a button which can be toggled on and off.
        /// </summary>
        public GuiToggleButton<T> AddToggleButton()
        {
            var newToggleButton = new GuiToggleButton<T>();
            Elements.Add(newToggleButton);

            return newToggleButton;
        }

        /// <summary>
        /// Adds a list of elements.
        /// </summary>
        /// <param name="template">The template to use.</param>
        public GuiList<T> AddList(Action<GuiListTemplate<T>> template)
        {
            var newTemplate = new GuiListTemplate<T>();
            template(newTemplate);
            var newList = new GuiList<T>(newTemplate);
            Elements.Add(newList);

            return newList;
        }

        public GuiGroup<T> AddGroup(Action<GuiGroup<T>> group)
        {
            var newGroup = new GuiGroup<T>();
            group(newGroup);
            Elements.Add(newGroup);

            return newGroup;
        }

        public GuiGroup<T> AddPartialView(string partialName)
        {
            var newGroup = new GuiGroup<T>();
            newGroup.SetId(partialName);
            newGroup.SetScrollbars(NuiScrollbars.None);
            newGroup.SetShowBorder(false);
            Elements.Add(newGroup);

            return newGroup;
        }

        /// <summary>
        /// Adds a column.
        /// </summary>
        /// <param name="col">The column to build.</param>
        public GuiColumn<T> AddColumn(Action<GuiColumn<T>> col)
        {
            var newColumn = new GuiColumn<T>();
            Elements.Add(newColumn);
            col(newColumn);

            return newColumn;
        }
    }
}

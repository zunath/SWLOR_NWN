using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiExpandableComponent<T>: GuiWidget<T, GuiExpandableComponent<T>>
        where T: IGuiViewModel
    {
        public List<IGuiWidget> Elements { get; protected set; }
        
        public GuiButton<T> AddButton()
        {
            var newButton = new GuiButton<T>();
            Elements.Add(newButton);

            return newButton;
        }

        public GuiButtonImage<T> AddButtonImage()
        {
            var newButton = new GuiButtonImage<T>();
            Elements.Add(newButton);

            return newButton;
        }

        public GuiChart<T> AddChart()
        {
            var newChart = new GuiChart<T>();
            Elements.Add(newChart);

            return newChart;
        }

        public GuiCheckBox<T> AddCheckBox()
        {
            var newCheckBox = new GuiCheckBox<T>();
            Elements.Add(newCheckBox);

            return newCheckBox;
        }

        public GuiColorPicker<T> AddColorPicker()
        {
            var newColorPicker = new GuiColorPicker<T>();
            Elements.Add(newColorPicker);

            return newColorPicker;
        }

        public GuiComboBox<T> AddComboBox()
        {
            var newComboBox = new GuiComboBox<T>();
            Elements.Add(newComboBox);

            return newComboBox;
        }

        public GuiImage<T> AddImage()
        {
            var newImage = new GuiImage<T>();
            Elements.Add(newImage);

            return newImage;
        }

        public GuiLabel<T> AddLabel()
        {
            var newLabel = new GuiLabel<T>();
            Elements.Add(newLabel);

            return newLabel;
        }

        public GuiOptions<T> AddOptions()
        {
            var newOptions = new GuiOptions<T>();
            Elements.Add(newOptions);

            return newOptions;
        }

        public GuiProgressBar<T> AddProgressBar()
        {
            var newProgressBar = new GuiProgressBar<T>();
            Elements.Add(newProgressBar);

            return newProgressBar;
        }

        public GuiSliderFloat<T> AddSliderFloat()
        {
            var newSliderFloat = new GuiSliderFloat<T>();
            Elements.Add(newSliderFloat);

            return newSliderFloat;
        }

        public GuiSliderInt<T> AddSliderInt()
        {
            var newSliderInt = new GuiSliderInt<T>();
            Elements.Add(newSliderInt);

            return newSliderInt;
        }

        public GuiSpacer<T> AddSpacer()
        {
            var newSpacer = new GuiSpacer<T>();
            Elements.Add(newSpacer);

            return newSpacer;
        }

        public GuiText<T> AddText()
        {
            var newText = new GuiText<T>();
            Elements.Add(newText);

            return newText;
        }

        public GuiTextEdit<T> AddTextEdit()
        {
            var newTextEdit = new GuiTextEdit<T>();
            Elements.Add(newTextEdit);

            return newTextEdit;
        }

        public GuiToggleButton<T> AddToggleButton()
        {
            var newToggleButton = new GuiToggleButton<T>();
            Elements.Add(newToggleButton);

            return newToggleButton;
        }

        public GuiList<T> AddList(Action<GuiListTemplate<T>> template)
        {
            var newTemplate = new GuiListTemplate<T>();
            template(newTemplate);
            var newList = new GuiList<T>(newTemplate);
            Elements.Add(newList);

            return newList;
        }

        public GuiColumn<T> AddColumn()
        {
            var newColumn = new GuiColumn<T>();
            Elements.Add(newColumn);

            return newColumn;
        }

        protected GuiExpandableComponent()
        {
            Elements = new List<IGuiWidget>();
        }

    }
}

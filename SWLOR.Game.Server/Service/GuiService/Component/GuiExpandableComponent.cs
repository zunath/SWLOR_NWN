using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public abstract class GuiExpandableComponent: GuiWidget
    {
        protected List<GuiWidget> Elements { get; set; }

        public GuiButton AddButton()
        {
            var newButton = new GuiButton();
            Elements.Add(newButton);

            return newButton;
        }

        public GuiButtonImage AddButtonImage()
        {
            var newButton = new GuiButtonImage();
            Elements.Add(newButton);

            return newButton;
        }

        public GuiChart AddChart()
        {
            var newChart = new GuiChart();
            Elements.Add(newChart);

            return newChart;
        }

        public GuiCheckBox AddCheckBox()
        {
            var newCheckBox = new GuiCheckBox();
            Elements.Add(newCheckBox);

            return newCheckBox;
        }

        public GuiColorPicker AddColorPicker()
        {
            var newColorPicker = new GuiColorPicker();
            Elements.Add(newColorPicker);

            return newColorPicker;
        }

        public GuiComboBox AddComboBox()
        {
            var newComboBox = new GuiComboBox();
            Elements.Add(newComboBox);

            return newComboBox;
        }

        public GuiImage AddImage()
        {
            var newImage = new GuiImage();
            Elements.Add(newImage);

            return newImage;
        }

        public GuiLabel AddLabel()
        {
            var newLabel = new GuiLabel();
            Elements.Add(newLabel);

            return newLabel;
        }

        public GuiOptions AddOptions()
        {
            var newOptions = new GuiOptions();
            Elements.Add(newOptions);

            return newOptions;
        }

        public GuiProgressBar AddProgressBar()
        {
            var newProgressBar = new GuiProgressBar();
            Elements.Add(newProgressBar);

            return newProgressBar;
        }

        public GuiSliderFloat AddSliderFloat()
        {
            var newSliderFloat = new GuiSliderFloat();
            Elements.Add(newSliderFloat);

            return newSliderFloat;
        }

        public GuiSliderInt AddSliderInt()
        {
            var newSliderInt = new GuiSliderInt();
            Elements.Add(newSliderInt);

            return newSliderInt;
        }

        public GuiSpacer AddSpacer()
        {
            var newSpacer = new GuiSpacer();
            Elements.Add(newSpacer);

            return newSpacer;
        }

        public GuiText AddText()
        {
            var newText = new GuiText();
            Elements.Add(newText);

            return newText;
        }

        public GuiTextEdit AddTextEdit()
        {
            var newTextEdit = new GuiTextEdit();
            Elements.Add(newTextEdit);

            return newTextEdit;
        }

        public GuiToggleButton AddToggleButton()
        {
            var newToggleButton = new GuiToggleButton();
            Elements.Add(newToggleButton);

            return newToggleButton;
        }

        public GuiList AddList(Action<GuiListTemplate> template)
        {
            var newTemplate = new GuiListTemplate();
            template(newTemplate);
            var newList = new GuiList(newTemplate);
            Elements.Add(newList);

            return newList;
        }

        protected GuiExpandableComponent()
        {
            Elements = new List<GuiWidget>();
        }

    }
}

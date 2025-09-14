using System;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiListTemplate<T>: GuiWidget<T, GuiListTemplate<T>>
        where T: IGuiViewModel
    {
        public override Json BuildElement()
        {
            throw new NotSupportedException();
        }

        public GuiListTemplate<T> AddCell(Action<GuiTemplateCell<T>> cell)
        {
            var newCell = new GuiTemplateCell<T>();
            cell(newCell);
            Elements.Add(newCell);

            return this;
        }

        /// <summary>
        /// Serializes the list template into Json.
        /// </summary>
        public override Json ToJson()
        {
            var template = JsonArray();

            foreach (var element in Elements)
            {
                var json = element.ToJson();
                template = JsonArrayInsert(template, json);
            }

            return template;
        }
    }
}

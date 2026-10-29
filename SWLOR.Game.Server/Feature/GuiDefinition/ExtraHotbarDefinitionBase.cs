using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A borderless bar of click-to-use slots. Each extra hotbar is its own window so its position is saved separately.
    /// </summary>
    public abstract class ExtraHotbarDefinitionBase : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ExtraHotbarViewModel> _builder = new();

        protected abstract GuiWindowType WindowType { get; }
        protected abstract float InitialY { get; }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(WindowType)
                .SetInitialGeometry(ExtraHotbarViewModel.InitialX, InitialY,
                    ExtraHotbarViewModel.GetWidth(ExtraHotbars.MaxSlots, ExtraHotbarLayout.Horizontal),
                    ExtraHotbarViewModel.GetHeight(ExtraHotbars.MaxSlots, ExtraHotbarLayout.Horizontal, false))
                .BindTitle(model => model.TitleBar)
                .SetIsClosable(false)
                .SetIsResizable(false)
                .SetIsCollapsible(false)
                .SetShowBorder(false)
                .BindIsTransparent(model => model.IsTransparent)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(ExtraHotbarViewModel.HorizontalPartial, group => group.AddColumn(col =>
                {
                    col.SetMargin(0f);
                    col.AddRow(row =>
                    {
                        row.SetMargin(0f);
                        for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
                            AddSlot(row, slot, "h");
                    });
                }))
                .DefinePartialView(ExtraHotbarViewModel.VerticalPartial, group => group.AddColumn(col =>
                {
                    col.SetMargin(0f);
                    for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
                    {
                        var index = slot;
                        col.AddRow(row =>
                        {
                            row.SetMargin(0f);
                            AddSlot(row, index, "v");
                        });
                    }
                }))
                .DefinePartialView(ExtraHotbarViewModel.StackedPartial, group => group.AddColumn(col =>
                {
                    col.SetMargin(0f);
                    for (var start = 0; start < ExtraHotbars.MaxSlots; start += ExtraHotbarViewModel.StackedRowLength)
                    {
                        var first = start;
                        col.AddRow(row =>
                        {
                            row.SetMargin(0f);
                            for (var slot = first; slot < first + ExtraHotbarViewModel.StackedRowLength; slot++)
                                AddSlot(row, slot, "s");
                        });
                    }
                }))
                .AddColumn(col =>
                {
                    col.SetMargin(0f);
                    col.AddRow(row =>
                    {
                        row.SetMargin(0f);
                        row.AddPartialView(ExtraHotbarViewModel.SlotsElement);
                    });
                });

            return _builder.Build();
        }

        private static void AddSlot(GuiRow<ExtraHotbarViewModel> row, int slot, string layout)
        {
            // A plain button with the icon drawn over it, so an empty slot shows only its frame.
            const float inset = 2f;
            row.AddButton()
                .SetId($"ehb_{layout}_{slot}")
                .SetText(string.Empty)
                .BindTooltip(Bind<string>(ExtraHotbarViewModel.TooltipProperty(slot)))
                .BindIsEncouraged(Bind<bool>(ExtraHotbarViewModel.IsActiveProperty(slot)))
                .BindIsVisible(Bind<bool>(ExtraHotbarViewModel.IsVisibleProperty(slot)))
                .SetWidth(ExtraHotbarViewModel.SlotSize)
                .SetHeight(ExtraHotbarViewModel.SlotSize)
                .SetMargin(ExtraHotbarViewModel.SlotMargin)
                .BindOnClicked(model => model.OnClickSlot(slot))
                .BindOnMouseDown(model => model.OnMouseDownSlot(slot))
                .AddDrawList(drawList => drawList
                    .AddImage(image => image
                        .BindResref(Bind<string>(ExtraHotbarViewModel.IconProperty(slot)))
                        .BindIsEnabled(Bind<bool>(ExtraHotbarViewModel.HasIconProperty(slot)))
                        .SetPosition(inset, inset, ExtraHotbarViewModel.SlotSize - inset * 2f, ExtraHotbarViewModel.SlotSize - inset * 2f)
                        .SetAspect(NuiAspect.Stretch))
                    .AddText(text => text
                        .BindText(Bind<string>(ExtraHotbarViewModel.CountProperty(slot)))
                        .SetBounds(inset, ExtraHotbarViewModel.SlotSize - 18f, ExtraHotbarViewModel.SlotSize - inset * 2f, 16f)
                        .SetColor(255, 255, 255)));
        }

        private static Expression<Func<ExtraHotbarViewModel, TValue>> Bind<TValue>(string propertyName)
        {
            var model = Expression.Parameter(typeof(ExtraHotbarViewModel), "model");
            return Expression.Lambda<Func<ExtraHotbarViewModel, TValue>>(
                Expression.Property(model, propertyName), model);
        }
    }
}

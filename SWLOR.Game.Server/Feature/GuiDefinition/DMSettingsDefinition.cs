using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A DM's chat and display settings, opened from the DM toolbar or /dmsettings.
    /// </summary>
    public class DMSettingsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMSettingsViewModel> _builder = new();

        private const float ContentPanelWidth = 320f;
        private const float LabelHeight = 20f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMSettings)
                .SetInitialGeometry(DMToolbarViewModel.ToolbarX, DMToolbarViewModel.ToolbarY, 360f, 360f)
                .SetTitle("DM Settings")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMSettingsViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMSettingsViewModel.MainContentElement);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMSettingsViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            AddSectionLabel(content, "Emote Style");
                            content.AddRow(r =>
                            {
                                r.AddOptions()
                                    .SetId("dms_emote_style")
                                    .SetDirection(NuiDirection.Horizontal)
                                    .AddOption("Regular")
                                    .AddOption("Novel")
                                    .SetTooltip("Regular: emotes go between *asterisks*. Novel: speech goes between \"quotes\" and everything else is an emote.")
                                    .BindSelectedValue(model => model.EmoteStyleIndex)
                                    .SetHeight(30f)
                                    .SetWidth(240f);
                                r.AddSpacer();
                            });

                            AddSectionLabel(content, "Comms");
                            content.AddRow(r =>
                            {
                                r.AddOptions()
                                    .SetId("dms_comms_filter")
                                    .SetDirection(NuiDirection.Vertical)
                                    .AddOption("Show All Party Comms")
                                    .AddOption("Show Only My Party's Comms")
                                    .SetTooltip("With 'Only My Party', you see no Comms while you are not in a party.")
                                    .BindSelectedValue(model => model.CommsFilterIndex)
                                    .SetHeight(72f)
                                    .SetWidth(300f);
                                r.AddSpacer();
                            });

                            content.AddRow(r => r.AddLabel()
                                .BindText(model => model.ArmorDisplayText)
                                .SetHeight(LabelHeight)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));
                            content.AddRow(r =>
                            {
                                r.AddCheckBox()
                                    .SetId("dms_show_helmet")
                                    .SetText("Show Helmet")
                                    .BindIsChecked(model => model.ShowHelmet);
                                r.AddSpacer();
                            });
                            content.AddRow(r =>
                            {
                                r.AddCheckBox()
                                    .SetId("dms_show_cloak")
                                    .SetText("Show Cloak")
                                    .BindIsChecked(model => model.ShowCloak);
                                r.AddSpacer();
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddSectionLabel(GuiColumn<DMSettingsViewModel> content, string text)
        {
            content.AddRow(r => r.AddLabel()
                .SetText(text)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }
    }
}

using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CharacterSheetDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterSheetViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CharacterSheet)
                .BindGeometry(model => model.Geometry)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(false)
                .SetGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Character Sheet")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Name)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.PortraitResref)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetAspectRatio(0.8f)
                            .SetHeight(200f)
                            .AddDrawList(list =>
                            {
                                // Hide the bottom bar on portraits with a black box.
                                // This leaves a gap but it looks better than the random colors found
                                // on the portraits.
                                list.AddImage(image =>
                                {
                                    image.SetResref("blackbox")
                                        .SetPosition(0, 312, 256, 112)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetVerticalAlign(NuiVerticalAlign.Bottom);
                                });
                            });
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Change Portrait")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickChangePortrait());
                        row.AddSpacer();
                    });
                })

                .AddColumn(col =>
                {

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.CharacterType)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("HP")
                            .SetColor(139, 0, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.HP)
                            .SetColor(139, 0, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("FP")
                            .SetColor(0, 138, 250)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.FP)
                            .SetColor(0, 138, 250)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("STM")
                            .SetColor(0, 139, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.STM)
                            .SetColor(0, 139, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Might")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Might)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Perception")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Perception)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Vitality")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Vitality)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Willpower")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Willpower)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Social")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Social)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });
                })

                .AddColumn(col =>
                {

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Race)
                            .SetHeight(20f);
                    });


                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Defense")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Defense)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });


                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Evasion")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.Evasion)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("SP")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.SP)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("AP")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddLabel()
                            .BindText(model => model.AP)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Skills")
                            .SetHeight(32f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickSkills());
                        row.AddButton()
                            .SetText("Perks")
                            .SetHeight(32f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickPerks());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Quests")
                            .SetHeight(32f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickQuests());
                        row.AddButton()
                            .SetText("Recipes")
                            .SetHeight(32f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickRecipes());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Key Items")
                            .SetHeight(32f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickKeyItems());
                        row.AddButton()
                            .SetText("Achievements")
                            .SetHeight(32f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickAchievements());
                    });
                });

            return _builder.Build();
        }
    }
}

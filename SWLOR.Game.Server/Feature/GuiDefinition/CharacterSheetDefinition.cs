using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CharacterSheetDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterSheetViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CharacterSheet)
                .BindGeometry(model => model.Geometry)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetTitle("Character Sheet")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("Stats")
                            .BindIsToggled(model => model.IsStatsSelected)
                            .BindOnClicked(model => model.OnClickStats())
                            .SetHeight(25f);

                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Name)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddImage()
                            .BindResref(model => model.PortraitResref)
                            .SetAspect(NuiAspect.Stretch);

                        row.SetHeight(200f);
                    });

                })
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("Skills")
                            .BindIsToggled(model => model.IsSkillsSelected)
                            .BindOnClicked(model => model.OnClickSkills())
                            .SetHeight(25f);
                    });

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
                            .SetColor(139, 0, 0);

                        row.AddLabel()
                            .BindText(model => model.HP)
                            .SetColor(139, 0, 0);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("FP")
                            .SetColor(0, 138, 250);

                        row.AddLabel()
                            .BindText(model => model.FP)
                            .SetColor(0, 138, 250);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("STM")
                            .SetColor(0, 139, 0);

                        row.AddLabel()
                            .BindText(model => model.STM)
                            .SetColor(0, 139, 0);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Might");

                        row.AddLabel()
                            .BindText(model => model.Might);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Perception");

                        row.AddLabel()
                            .BindText(model => model.Perception);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Vitality");

                        row.AddLabel()
                            .BindText(model => model.Vitality);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Willpower");

                        row.AddLabel()
                            .BindText(model => model.Willpower);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Social");

                        row.AddLabel()
                            .BindText(model => model.Social);
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("Perks")
                            .BindIsToggled(model => model.IsPerksSelected)
                            .BindOnClicked(model => model.OnClickPerks())
                            .SetHeight(25f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Race)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Defense");

                        row.AddLabel()
                            .BindText(model => model.Defense);
                    });


                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Evasion");

                        row.AddLabel()
                            .BindText(model => model.Evasion);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.SP);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.AP);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });


                })

                ;

            return _builder.Build();
        }
    }
}

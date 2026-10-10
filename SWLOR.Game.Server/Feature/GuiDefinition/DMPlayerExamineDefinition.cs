using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DMPlayerExamineDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMPlayerExamineViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMPlayerExamine)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 640f, 660f)
                .BindTitle(model => model.Name)
                .DefinePartialView(DMPlayerExamineViewModel.DetailView, AddDetails)
                .DefinePartialView(DMPlayerExamineViewModel.SkillsView, AddSkills)
                .DefinePartialView(DMPlayerExamineViewModel.PerksView, AddPerks)
                .DefinePartialView(DMPlayerExamineViewModel.EffectsView, AddEffects)
                .DefinePartialView(DMPlayerExamineViewModel.NotesView, group =>
                {
                    AddPanel(group, mainCol =>
                    {
                        mainCol.AddRow(mainRow =>
                        {
                            mainRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddList(template =>
                                        {
                                            template.AddCell(cell =>
                                            {
                                                cell.AddToggleButton()
                                                    .BindText(model => model.NoteNames)
                                                    .BindIsToggled(model => model.NoteToggles)
                                                    .BindOnClicked(model => model.OnClickNote());
                                            });
                                        })
                                        .BindRowCount(model => model.NoteNames);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddButton()
                                        .SetHeight(32f)
                                        .SetText("New Note")
                                        .BindOnClicked(model => model.OnClickNewNote());

                                    row.AddButton()
                                        .SetHeight(32f)
                                        .SetText("Delete")
                                        .BindOnClicked(model => model.OnClickDeleteNote());
                                });
                            });

                            mainRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddTextEdit()
                                        .BindValue(model => model.ActiveNoteName)
                                        .SetPlaceholder("Note Name")
                                        .SetMaxLength(50)
                                        .BindIsEnabled(model => model.IsNoteSelected);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ActiveNoteCreator);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddTextEdit()
                                        .SetIsMultiline(true)
                                        .BindValue(model => model.ActiveNoteDetail)
                                        .SetMaxLength(3000)
                                        .BindIsEnabled(model => model.IsNoteSelected);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddSpacer();
                                    row.AddButton()
                                        .SetText("Save Changes")
                                        .SetHeight(32f)
                                        .BindOnClicked(model => model.OnClickSaveChanges())
                                        .BindIsEnabled(model => model.IsNoteSelected);
                                    row.AddSpacer();
                                });
                            });
                        });
                    });
                });

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(76f);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(28f);
                    row.AddToggles()
                        .AddOption("Details")
                        .AddOption("Skills")
                        .AddOption("Perks")
                        .BindSelectedValue(model => model.TopTabId)
                        .SetWidth(560f)
                        .SetHeight(28f);
                });
                layout.AddTabRow(row =>
                {
                    row.SetHeight(28f);
                    row.AddToggles()
                        .AddOption("Effects")
                        .AddOption("Notes")
                        .BindSelectedValue(model => model.BottomTabId)
                        .SetWidth(560f)
                        .SetHeight(28f);
                });
                layout.SetContentPartialElement(DMPlayerExamineViewModel.PartialView);
            });

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<DMPlayerExamineViewModel> host,
            Action<GuiColumn<DMPlayerExamineViewModel>> content)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content);
            }).SetWidth(560f)));
        }

        private static void AddDetails(GuiGroup<DMPlayerExamineViewModel> host) => AddPanel(host, col =>
        {
            col.AddRow(row =>
            {
                row.SetHeight(216f);
                row.AddImage().BindResref(model => model.PortraitResref)
                    .SetWidth(112f).SetHeight(152f).SetAspect(NuiAspect.Fit);
                row.AddColumn(details =>
                {
                    AddDetailRow(details, "Type", model => model.CharacterType);
                    AddDetailRow(details, "HP", model => model.Health);
                    AddDetailRow(details, "FP", model => model.FP);
                    AddDetailRow(details, "STM", model => model.STM);
                    AddDetailRow(details, "Alignment", model => model.AlignmentText);
                    AddDetailRow(details, "Experience", model => model.Experience);
                    AddDetailRow(details, "Credits", model => model.Credits);
                });
            });
            AddDetailRow(col, "Descriptor", model => model.Descriptor);
            AddDetailRow(col, "True name", model => model.TrueName);
            AddDetailRow(col, "Account", model => model.AccountName);
            AddDetailRow(col, "Public CD key", model => model.PublicCDKey);
            col.AddRow(r => r.AddLabel().SetText("Description").SetHeight(20f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
            col.AddRow(r => r.AddText().BindText(model => model.Description).SetHeight(96f));
        });

        private static void AddDetailRow(GuiColumn<DMPlayerExamineViewModel> col, string label,
            Expression<Func<DMPlayerExamineViewModel, string>> value)
        {
            col.AddRow(row =>
            {
                row.AddLabel().SetText(label).SetWidth(112f).SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddLabel().BindText(value).BindTooltip(value).SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddSkills(GuiGroup<DMPlayerExamineViewModel> host) => AddPanel(host, col =>
        {
            col.AddRow(row =>
            {
                row.AddComboBox().BindOptions(model => model.SkillCategories)
                    .BindSelectedIndex(model => model.SelectedSkillCategoryId).SetHeight(32f);
                row.AddTextEdit().SetPlaceholder("Search skills")
                    .BindValue(model => model.SkillSearchText).SetHeight(32f).SetMaxLength(100);
            });
            col.AddRow(r => r.AddLabel().BindText(model => model.SkillSummary).SetHeight(20f));
            col.AddTable(table => table
                .AddColumn("Skill", 300f, model => model.SkillNames, model => model.SkillDescriptions)
                .AddColumn("Level", 0f, model => model.SkillLevels)
                .SetRowHeight(28f));
        });

        private static void AddPerks(GuiGroup<DMPlayerExamineViewModel> host) => AddPanel(host, col =>
        {
            col.AddRow(row =>
            {
                row.AddComboBox().BindOptions(model => model.PerkCategories)
                    .BindSelectedIndex(model => model.SelectedPerkCategoryId).SetHeight(32f);
                row.AddTextEdit().SetPlaceholder("Search perks")
                    .BindValue(model => model.PerkSearchText).SetHeight(32f).SetMaxLength(100);
            });
            col.AddRow(row =>
            {
                row.AddComboBox().BindSelectedIndex(model => model.SelectedPerkStatusId)
                    .AddOption("All", 0).AddOption("Owned", 1).AddOption("Can Buy", 2).AddOption("Maxed", 3)
                    .SetWidth(268f).SetHeight(32f);
                row.AddComboBox().BindSelectedIndex(model => model.SelectedPerkSortOrderId)
                    .AddOption("Alphabetical (A-Z)", 0)
                    .AddOption("Alphabetical (Z-A)", 1)
                    .AddOption("Skill Level (Asc)", 2)
                    .AddOption("Skill Level (Desc)", 3)
                    .SetWidth(268f)
                    .SetHeight(32f);
            });
            col.AddRow(r => r.AddLabel().BindText(model => model.PerkSummary).SetHeight(20f));
            col.AddTable(table => table
                .AddColumn("Perk", 300f, model => model.PerkNames, model => model.PerkDescriptions)
                .AddColumn("Rank", 0f, model => model.PerkLevels)
                .SetRowHeight(28f));
        });

        private static void AddEffects(GuiGroup<DMPlayerExamineViewModel> host) => AddPanel(host, col =>
        {
            col.AddRow(row =>
            {
                row.AddLabel().BindText(model => model.EffectSummary).SetHeight(32f);
                row.AddButton().SetText("Refresh").SetWidth(100f).SetHeight(32f)
                    .BindOnClicked(model => model.OnClickRefreshEffects());
            });
            col.AddTable(table => table
                .AddColumn("Effect", 220f, model => model.EffectNames, model => model.EffectDetails)
                .AddColumn("Remaining", 110f, model => model.EffectDurations)
                .AddColumn("Source", 0f, model => model.EffectSources, model => model.EffectDetails)
                .SetRowHeight(28f));
        });
    }
}

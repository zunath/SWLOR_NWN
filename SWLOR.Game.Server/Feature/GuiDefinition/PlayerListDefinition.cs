using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Replaces the native player list: names the viewer may see, public descriptors, opt-in flags,
    /// tells, hostility, and party controls.
    /// </summary>
    public class PlayerListDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PlayerListViewModel> _builder = new();

        private const float ContentPanelWidth = 560f;
        private const float ButtonHeight = 32f;
        private const float StatusColumnWidth = 64f;
        private const float NameColumnWidth = 140f;
        private const float FlagColumnWidth = 28f;
        private const float FlagIconSize = 20f;
        // The flag icons are 32x32; beside each toggle they show at full size, matching the button height.
        private const float FlagToggleIconSize = 32f;
        private const float FlagToggleWidth = 88f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.PlayerList)
                .SetInitialGeometry(0, 0, 610f, 620f)
                .SetTitle("Player List")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(PlayerListViewModel.MainContentPartial, host => AddMainContent(host, false))
                .DefinePartialView(PlayerListViewModel.DMContentPartial, host => AddMainContent(host, true));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(PlayerListViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<PlayerListViewModel> host, bool isDM)
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
                            AddFlagToggles(content);
                            AddSearch(content);
                            AddHeader(content);
                            AddPlayerRows(content);
                            AddSelectionSummary(content);
                            AddActions(content);
                            if (isDM)
                                AddDMActions(content);
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddFlagToggles(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("YOUR FLAGS")
                    .SetColor(221, 181, 93)
                    .SetTooltip("Show other players what you are open to. Each disguise keeps its own flags.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);
            });

            content.AddRow(row =>
            {
                AddFlagToggle(row, PlayerListFlagType.GeneralRP,
                    model => model.IsGeneralRPFlagged, model => model.OnClickToggleGeneralRP());
                AddFlagToggle(row, PlayerListFlagType.PvP,
                    model => model.IsPvPFlagged, model => model.OnClickTogglePvP());
                AddFlagToggle(row, PlayerListFlagType.DM,
                    model => model.IsDMFlagged, model => model.OnClickToggleDM());
                AddFlagToggle(row, PlayerListFlagType.Crafting,
                    model => model.IsCraftingFlagged, model => model.OnClickToggleCrafting());
                row.AddSpacer();
            });
        }

        private static void AddFlagToggle(
            GuiRow<PlayerListViewModel> row,
            PlayerListFlagType flag,
            Expression<Func<PlayerListViewModel, bool>> isToggled,
            Expression<Func<PlayerListViewModel, Action>> onClick)
        {
            var definition = flag.GetAttribute<PlayerListFlagType, PlayerListFlagAttribute>();

            row.AddImage()
                .SetResref(definition.IconResref)
                .SetTooltip(definition.Description)
                .SetAspect(NuiAspect.Fit)
                .SetWidth(FlagToggleIconSize)
                .SetHeight(FlagToggleIconSize);

            row.AddToggleButton()
                .SetText(definition.Name)
                .SetTooltip(definition.Description)
                .SetDisabledTooltip("DMs do not use player list flags.")
                .BindIsToggled(isToggled)
                .BindIsEnabled(model => model.CanSetFlags)
                .BindOnClicked(onClick)
                .SetWidth(FlagToggleWidth)
                .SetHeight(ButtonHeight);
        }

        private static void AddSearch(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddTextEdit()
                    .SetPlaceholder("Search by name or descriptor")
                    .SetTooltip("Each word matches the start of a word in the name you see or the descriptor.")
                    .BindValue(model => model.SearchText)
                    .SetMaxLength(64)
                    .SetHeight(ButtonHeight);

                row.AddButton()
                    .SetText("X")
                    .SetTooltip("Clear the search")
                    .SetWidth(35f)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickClearSearch());
            });
        }

        private static void AddHeader(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(header =>
            {
                header.AddLabel()
                    .SetText("Status")
                    .SetTooltip("Leader and Party mark your party. Grouped players are in another party.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(StatusColumnWidth)
                    .SetHeight(20f);

                header.AddLabel()
                    .SetText("Name")
                    .SetTooltip("The name you gave this player, or Unassigned.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(NameColumnWidth)
                    .SetHeight(20f);

                header.AddLabel()
                    .SetText("Public Descriptor")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);

                header.AddLabel()
                    .SetText("Flags")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(FlagColumnWidth * 4)
                    .SetHeight(20f);
            });
        }

        private static void AddPlayerRows(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(listRow =>
            {
                listRow.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(StatusColumnWidth);
                        cell.AddLabel()
                            .BindText(model => model.RowStatuses)
                            .BindColor(model => model.RowStatusColors)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NameColumnWidth);
                        cell.AddToggleButton()
                            .BindText(model => model.RowNames)
                            .BindTooltip(model => model.RowNameTooltips)
                            .BindIsToggled(model => model.RowSelections)
                            .BindOnClicked(model => model.OnSelectRow());
                    });

                    template.AddCell(cell =>
                    {
                        cell.AddLabel()
                            .BindText(model => model.RowDescriptors)
                            .BindColor(model => model.RowDescriptorColors)
                            .BindTooltip(model => model.RowDescriptorTooltips)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    AddFlagCell(template, PlayerListFlagType.GeneralRP, model => model.RowShowsGeneralRP);
                    AddFlagCell(template, PlayerListFlagType.PvP, model => model.RowShowsPvP);
                    AddFlagCell(template, PlayerListFlagType.DM, model => model.RowShowsDM);
                    AddFlagCell(template, PlayerListFlagType.Crafting, model => model.RowShowsCrafting);
                })
                    .SetRowHeight(30f)
                    .BindRowCount(model => model.RowNames);
            });
        }

        // Each flag keeps its own column; rows hide the icon instead of sending an empty resref,
        // which the client draws as a missing texture. A list image needs a group around it.
        private static void AddFlagCell(
            GuiListTemplate<PlayerListViewModel> template,
            PlayerListFlagType flag,
            Expression<Func<PlayerListViewModel, GuiBindingList<bool>>> isShown)
        {
            var definition = flag.GetAttribute<PlayerListFlagType, PlayerListFlagAttribute>();

            template.AddCell(cell =>
            {
                cell.SetIsVariable(false);
                cell.SetWidth(FlagColumnWidth);
                cell.AddGroup(group =>
                {
                    group.AddImage()
                        .SetResref(definition.IconResref)
                        .SetTooltip(definition.Description)
                        .BindIsVisible(isShown)
                        .SetAspect(NuiAspect.Fit)
                        .SetWidth(FlagIconSize)
                        .SetHeight(FlagIconSize)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                })
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None);
            });
        }

        private static void AddSelectionSummary(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.SelectedText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);

                row.AddLabel()
                    .BindText(model => model.OnlineSummary)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                    .SetWidth(140f)
                    .SetHeight(20f);
            });
        }

        private static void AddActions(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(row =>
            {
                AddActionButton(row, "Send Tell", 110f, "Send a private message to the selected player",
                    model => model.CanSendTell, model => model.SendTellReason, model => model.OnClickSendTell());

                row.AddButton()
                    .BindText(model => model.ToggleHostileText)
                    .SetTooltip("Mark or clear the selected player as hostile for PvP")
                    .BindDisabledTooltip(model => model.ToggleHostileReason)
                    .BindIsEnabled(model => model.CanToggleHostile)
                    .BindOnClicked(model => model.OnClickToggleHostile())
                    .SetWidth(120f)
                    .SetHeight(ButtonHeight);

                row.AddSpacer();
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Call")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(44f)
                    .SetHeight(ButtonHeight);

                AddActionButton(row, "Call", 90f, "Call the selected player on your HoloCom",
                    model => model.CanCall, model => model.CallReason, model => model.OnClickCall());
                AddActionButton(row, "Answer", 90f, "Answer your incoming HoloCom call",
                    model => model.CanAnswerCall, model => model.AnswerCallReason, model => model.OnClickAnswerCall());
                AddActionButton(row, "Decline", 90f, "Decline your incoming HoloCom call",
                    model => model.CanDeclineCall, model => model.DeclineCallReason, model => model.OnClickDeclineCall());
                row.AddButton()
                    .BindText(model => model.EndCallText)
                    .SetTooltip("End your HoloCom call or cancel the call you are placing")
                    .BindDisabledTooltip(model => model.EndCallReason)
                    .BindIsEnabled(model => model.CanEndCall)
                    .BindOnClicked(model => model.OnClickEndCall())
                    .SetWidth(110f)
                    .SetHeight(ButtonHeight);

                row.AddSpacer();
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Party")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(44f)
                    .SetHeight(ButtonHeight);

                AddActionButton(row, "Invite", 90f, "Invite the selected player to your party",
                    model => model.CanInvite, model => model.InviteReason, model => model.OnClickInvite());
                AddActionButton(row, "Remove", 90f, "Remove the selected player from your party",
                    model => model.CanRemove, model => model.RemoveReason, model => model.OnClickRemove());
                AddActionButton(row, "Make Leader", 110f, "Make the selected player your party's leader",
                    model => model.CanMakeLeader, model => model.MakeLeaderReason, model => model.OnClickMakeLeader());
                AddActionButton(row, "Leave Party", 110f, "Leave your party",
                    model => model.CanLeaveParty, model => model.LeavePartyReason, model => model.OnClickLeaveParty());

                row.AddSpacer();
            });
        }

        private static void AddDMActions(GuiColumn<PlayerListViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("DM")
                    .SetColor(100, 180, 255)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(44f)
                    .SetHeight(ButtonHeight);

                AddActionButton(row, "Jump To", 90f, "Teleport to the selected player",
                    model => model.CanJumpTo, model => model.JumpToReason, model => model.OnClickJumpTo());
                AddActionButton(row, "Jump Here", 90f, "Bring the selected player to you",
                    model => model.CanJumpHere, model => model.JumpHereReason, model => model.OnClickJumpHere());
                AddActionButton(row, "Examine", 90f, "Open the DM examine window for the selected player",
                    model => model.CanExamine, model => model.ExamineReason, model => model.OnClickExamine());
                AddActionButton(row, "Boot", 90f, "Disconnect the selected player from the server",
                    model => model.CanBoot, model => model.BootReason, model => model.OnClickBoot());

                row.AddSpacer();
            });
        }

        private static void AddActionButton(
            GuiRow<PlayerListViewModel> row,
            string text,
            float width,
            string tooltip,
            Expression<Func<PlayerListViewModel, bool>> isEnabled,
            Expression<Func<PlayerListViewModel, string>> disabledReason,
            Expression<Func<PlayerListViewModel, Action>> onClick)
        {
            row.AddButton()
                .SetText(text)
                .SetTooltip(tooltip)
                .BindDisabledTooltip(disabledReason)
                .BindIsEnabled(isEnabled)
                .BindOnClicked(onClick)
                .SetWidth(width)
                .SetHeight(ButtonHeight);
        }
    }
}

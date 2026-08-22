using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class HoloComDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HoloComViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.HoloCom)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 400f, 600f)
                .SetTitle("HoloCom")
                .DefinePartialView(HoloComViewModel.MessagesTabPartial, AddMessagesTab)
                .DefinePartialView(HoloComViewModel.ContactsTabPartial, AddContactsTab)
                .AddStandardLayout(layout =>
                {
                    layout.SetTabPanelHeight(40f);
                    layout.AddTabRow(tabRow =>
                    {
                        tabRow.SetHeight(28f);
                        tabRow.AddToggles()
                            .AddOption("Messages")
                            .AddOption("Contacts")
                            .BindSelectedValue(model => model.TabToggleValue)
                            .SetWidth(240f)
                            .SetHeight(28f);
                        tabRow.AddSpacer();
                    });
                    layout.SetContentPartialElement(HoloComViewModel.TabContentPartialElement);
                });

            return _builder.Build();
        }

        private static void AddMessagesTab(GuiGroup<HoloComViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.SetWidth(400f);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Refresh")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickRefreshMessages());

                    row.AddToggleButton()
                        .SetText("Unread Only")
                        .SetWidth(120f)
                        .SetHeight(32f)
                        .BindIsToggled(model => model.ShowUnreadOnly);

                    row.AddButton()
                        .SetText("Delete Read")
                        .SetWidth(100f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickDeleteRead());

                    row.AddSpacer();
                });

                // The table lives inside a scrollable group with no fixed height: the
                // group compresses to whatever viewport the tab-content host provides
                // (fixed-height content taller than a scrollbars-None host is unsolvable),
                // and overflowing rows scroll. See Readmes/NuiLayoutRules.md.
                col.AddRow(row =>
                {
                    row.AddGroup(group =>
                    {
                        group.SetShowBorder(false);
                        group.SetScrollbars(NuiScrollbars.Auto);
                        group.AddColumn(tableCol =>
                        {
                            tableCol.AddTable<HoloComViewModel>(t => t
                                .AddComponentColumn("FROM", 0f, cell =>
                                {
                                    cell.AddLabel()
                                        .BindText(model => model.MessageSenderNames)
                                        .BindColor(model => model.MessageRowColors)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                }, isVariable: true)
                                .AddColumn("RECEIVED", 140f, model => model.MessageTimestamps)
                                .AddComponentColumn("", 32f, cell =>
                                {
                                    cell.AddButtonImage()
                                        .SetImageResref("arrow_right")
                                        .SetWidth(28f)
                                        .SetHeight(28f)
                                        .BindOnClicked(model => model.OnClickPlayMessage());
                                }, isVariable: false)
                                .AddComponentColumn("", 70f, cell =>
                                {
                                    cell.AddButton()
                                        .BindText(model => model.MessageSaveLabels)
                                        .SetWidth(70f)
                                        .SetHeight(28f)
                                        .BindOnClicked(model => model.OnClickToggleSaveRow());
                                }, isVariable: false)
                                .AddComponentColumn("", 32f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("X")
                                        .SetWidth(28f)
                                        .SetHeight(28f)
                                        .BindOnClicked(model => model.OnClickDeleteRow())
                                        .BindIsEnabled(model => model.MessageCanDelete);
                                }, isVariable: false)
                                .SetRowHeight(28f)
                                .BindRowCount(model => model.MessageSenderNames));
                        });
                    });
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();

                    row.AddButton()
                        .SetText("<")
                        .SetWidth(32f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickPrevPage())
                        .BindIsEnabled(model => model.IsPrevPageEnabled);

                    row.AddLabel()
                        .BindText(model => model.InboxPageLabel)
                        .SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);

                    row.AddButton()
                        .SetText(">")
                        .SetWidth(32f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickNextPage())
                        .BindIsEnabled(model => model.IsNextPageEnabled);

                    row.AddSpacer();
                });
            });
        }

        private static void AddContactsTab(GuiGroup<HoloComViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.SetWidth(400f);
            group.AddColumn(col =>
            {
                // Always-visible call banner. Rows that are invisible when the partial's
                // layout is applied never render when their visibility bind later flips
                // to true (this wiped the tab after answering a call), so the banner
                // stays visible and its buttons gate on enabled-binds instead.
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.CallStatusLabel)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);

                    row.AddButton()
                        .SetText("Answer")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickAnswerCall())
                        .BindIsEnabled(model => model.IsAnswerEnabled);

                    row.AddButton()
                        .SetText("Decline/End")
                        .SetWidth(100f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickDeclineEndCall())
                        .BindIsEnabled(model => model.IsDeclineEndEnabled);
                });

                // The contact lists stay visible during calls (no visibility binds) for
                // the same reason the banner is always visible: rows hidden at layout
                // apply time never come back. Call actions are guarded in the click
                // handlers and the HoloCom service instead.
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Refresh")
                        .SetWidth(90f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickRefreshContacts());
                    row.AddSpacer();
                });

                col.AddRow(row => row.AddLabel()
                        .SetText("Favorites")
                        .SetHeight(22f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                // Fixed-height scrollable group (~15 visible rows at 30f) so the
                // Favorites table can never grow tall enough to blow out the tab.
                col.AddRow(row =>
                {
                    row.AddGroup(favGroup =>
                    {
                        favGroup.SetShowBorder(false);
                        favGroup.SetScrollbars(NuiScrollbars.Auto);
                        favGroup.SetHeight(200f);
                        favGroup.AddColumn(favCol =>
                        {
                            favCol.AddTable<HoloComViewModel>(t => t
                                .AddComponentColumn("", 20f, cell =>
                                {
                                    cell.AddLabel()
                                        .BindText(model => model.FavoriteNames)
                                        .BindColor(model => model.FavoriteStatusColors)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                }, isVariable: true)
                                .AddComponentColumn("", 60f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Call")
                                        .SetWidth(60f)
                                        .SetHeight(26f)
                                        .BindOnClicked(model => model.OnClickCallFavorite())
                                        .BindIsEnabled(model => model.FavoriteIsOnline);
                                })
                                .AddComponentColumn("", 70f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Message")
                                        .SetWidth(70f)
                                        .SetHeight(26f)
                                        .BindOnClicked(model => model.OnClickMessageFavorite());
                                })
                                .AddComponentColumn("", 70f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Remove")
                                        .SetWidth(70f)
                                        .SetHeight(26f)
                                        .BindOnClicked(model => model.OnClickRemoveFavorite());
                                }, isVariable: false)
                                .SetShowHeader(false)
                                .SetRowHeight(30f)
                                .BindRowCount(model => model.FavoriteNames));
                        });
                    });
                });

                col.AddRow(row => row.AddLabel()
                        .SetText("Online Players")
                        .SetHeight(22f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                // Unsized scrollable group as the terminal row of the column (R2) so it
                // fills whatever space remains below the Favorites section.
                col.AddRow(row =>
                {
                    row.AddGroup(onlineGroup =>
                    {
                        onlineGroup.SetShowBorder(false);
                        onlineGroup.SetScrollbars(NuiScrollbars.Auto);
                        onlineGroup.AddColumn(onlineCol =>
                        {
                            onlineCol.AddTable<HoloComViewModel>(t => t
                                .AddComponentColumn("", 20f, cell =>
                                {
                                    cell.AddLabel()
                                        .BindText(model => model.OnlinePlayerNames)
                                        .BindColor(model => model.OnlinePlayerColors)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                }, isVariable: true)
                                .AddComponentColumn("", 60f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Call")
                                        .SetWidth(60f)
                                        .SetHeight(26f)
                                        .BindOnClicked(model => model.OnClickCallOnline());
                                })
                                .AddComponentColumn("", 70f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Message")
                                        .SetWidth(70f)
                                        .SetHeight(26f)
                                        .BindOnClicked(model => model.OnClickMessageOnline());
                                })
                                .AddComponentColumn("", 70f, cell =>
                                {
                                    cell.AddButton()
                                        .SetText("Favorite")
                                        .SetWidth(70f)
                                        .SetHeight(26f)
                                        .BindOnClicked(model => model.OnClickFavoriteOnline());
                                }, isVariable: false)
                                .SetShowHeader(false)
                                .SetRowHeight(30f)
                                .BindRowCount(model => model.OnlinePlayerNames));
                        });
                    });
                });
            });
        }

    }
}
